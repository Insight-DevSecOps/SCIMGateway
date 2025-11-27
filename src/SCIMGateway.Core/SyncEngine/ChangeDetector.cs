// ==========================================================================
// T121-T123: ChangeDetector - Change Detection Implementation
// ==========================================================================
// Implements change detection, drift detection, and conflict detection
// ==========================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.SyncEngine;

/// <summary>
/// Implements change detection between Entra ID and provider states.
/// </summary>
public class ChangeDetector : IChangeDetector
{
    private readonly ILogger<ChangeDetector> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Attributes to ignore during comparison by default
    private static readonly HashSet<string> DefaultIgnoredAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "meta",
        "created",
        "lastModified",
        "version",
        "modifiedAt",
        "createdAt"
    };

    public ChangeDetector(ILogger<ChangeDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<ChangeDetectionResult> DetectChangesAsync(ChangeDetectionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(context.TenantId);
        ArgumentException.ThrowIfNullOrEmpty(context.ProviderId);

        _logger.LogInformation("Starting change detection for tenant {TenantId}, provider {ProviderId}", 
            context.TenantId, context.ProviderId);

        var result = new ChangeDetectionResult();

        // Detect changes in Entra (if both previous and current states provided)
        var entraChanges = new List<DriftReport>();
        if (context.PreviousEntraState != null && context.CurrentEntraState != null)
        {
            var userChanges = DetectUserChanges(
                context.PreviousEntraState.Users,
                context.CurrentEntraState.Users,
                context.TenantId,
                context.ProviderId).ToList();

            var groupChanges = DetectGroupChanges(
                context.PreviousEntraState.Groups,
                context.CurrentEntraState.Groups,
                context.TenantId,
                context.ProviderId).ToList();

            entraChanges.AddRange(userChanges);
            entraChanges.AddRange(groupChanges);
        }

        // Detect changes in Provider (if both previous and current states provided)
        var providerChanges = new List<DriftReport>();
        if (context.PreviousProviderState != null && context.CurrentProviderState != null)
        {
            var userChanges = DetectUserChanges(
                context.PreviousProviderState.Users,
                context.CurrentProviderState.Users,
                context.TenantId,
                context.ProviderId).ToList();

            var groupChanges = DetectGroupChanges(
                context.PreviousProviderState.Groups,
                context.CurrentProviderState.Groups,
                context.TenantId,
                context.ProviderId).ToList();

            providerChanges.AddRange(userChanges);
            providerChanges.AddRange(groupChanges);
        }

        // Detect drift between Entra and Provider current states
        if (context.CurrentEntraState != null && context.CurrentProviderState != null)
        {
            var driftReports = DetectDrift(
                context.CurrentEntraState,
                context.CurrentProviderState,
                context.TenantId,
                context.ProviderId).ToList();

            result.DriftReports.AddRange(driftReports);
        }

        // Detect conflicts (same resource modified in both systems)
        if (entraChanges.Any() && providerChanges.Any())
        {
            var conflicts = DetectConflicts(
                entraChanges,
                providerChanges,
                context.TenantId,
                context.ProviderId).ToList();

            result.ConflictReports.AddRange(conflicts);
        }

        // Calculate statistics
        CalculateStatistics(result);

        // Compute new state hash
        if (context.CurrentProviderState != null)
        {
            var allResources = new List<object>();
            allResources.AddRange(context.CurrentProviderState.Users);
            allResources.AddRange(context.CurrentProviderState.Groups);
            result.NewStateHash = ComputeStateHash(allResources);
        }

        _logger.LogInformation("Change detection completed: {Summary}", result.Summary);

        return await Task.FromResult(result);
    }

    /// <inheritdoc />
    public IEnumerable<DriftReport> DetectUserChanges(
        IEnumerable<ScimUser> previousUsers,
        IEnumerable<ScimUser> currentUsers,
        string tenantId,
        string providerId)
    {
        var driftReports = new List<DriftReport>();
        var previousDict = previousUsers.ToDictionary(u => u.Id, u => u);
        var currentDict = currentUsers.ToDictionary(u => u.Id, u => u);

        // Detect added users
        foreach (var current in currentUsers)
        {
            if (!previousDict.ContainsKey(current.Id))
            {
                driftReports.Add(DriftReportFactory.CreateAddedDrift(
                    tenantId,
                    providerId,
                    "User",
                    current.Id,
                    current,
                    current.DisplayName ?? current.UserName));
            }
        }

        // Detect deleted users
        foreach (var previous in previousUsers)
        {
            if (!currentDict.ContainsKey(previous.Id))
            {
                driftReports.Add(DriftReportFactory.CreateDeletedDrift(
                    tenantId,
                    providerId,
                    "User",
                    previous.Id,
                    previous,
                    previous.DisplayName ?? previous.UserName));
            }
        }

        // Detect modified users
        foreach (var current in currentUsers)
        {
            if (previousDict.TryGetValue(current.Id, out var previous))
            {
                var attributeDrifts = CompareUserAttributes(previous, current).ToList();
                if (attributeDrifts.Any())
                {
                    driftReports.Add(DriftReportFactory.CreateModifiedDrift(
                        tenantId,
                        providerId,
                        "User",
                        current.Id,
                        attributeDrifts,
                        previous,
                        current,
                        current.DisplayName ?? current.UserName));
                }
            }
        }

        _logger.LogDebug("Detected {Count} user changes for tenant {TenantId}, provider {ProviderId}", 
            driftReports.Count, tenantId, providerId);

        return driftReports;
    }

    /// <inheritdoc />
    public IEnumerable<DriftReport> DetectGroupChanges(
        IEnumerable<ScimGroup> previousGroups,
        IEnumerable<ScimGroup> currentGroups,
        string tenantId,
        string providerId)
    {
        var driftReports = new List<DriftReport>();
        var previousDict = previousGroups.ToDictionary(g => g.Id, g => g);
        var currentDict = currentGroups.ToDictionary(g => g.Id, g => g);

        // Detect added groups
        foreach (var current in currentGroups)
        {
            if (!previousDict.ContainsKey(current.Id))
            {
                driftReports.Add(DriftReportFactory.CreateAddedDrift(
                    tenantId,
                    providerId,
                    "Group",
                    current.Id,
                    current,
                    current.DisplayName));
            }
        }

        // Detect deleted groups
        foreach (var previous in previousGroups)
        {
            if (!currentDict.ContainsKey(previous.Id))
            {
                driftReports.Add(DriftReportFactory.CreateDeletedDrift(
                    tenantId,
                    providerId,
                    "Group",
                    previous.Id,
                    previous,
                    previous.DisplayName));
            }
        }

        // Detect modified groups
        foreach (var current in currentGroups)
        {
            if (previousDict.TryGetValue(current.Id, out var previous))
            {
                // Check for attribute changes
                var attributeDrifts = CompareGroupAttributes(previous, current).ToList();
                
                // Check for membership changes
                var membershipDrift = CompareMembership(previous, current);
                
                if (attributeDrifts.Any())
                {
                    driftReports.Add(DriftReportFactory.CreateModifiedDrift(
                        tenantId,
                        providerId,
                        "Group",
                        current.Id,
                        attributeDrifts,
                        previous,
                        current,
                        current.DisplayName));
                }

                if (membershipDrift != null)
                {
                    membershipDrift.TenantId = tenantId;
                    membershipDrift.ProviderId = providerId;
                    driftReports.Add(membershipDrift);
                }
            }
        }

        _logger.LogDebug("Detected {Count} group changes for tenant {TenantId}, provider {ProviderId}", 
            driftReports.Count, tenantId, providerId);

        return driftReports;
    }

    /// <inheritdoc />
    public IEnumerable<DriftReport> DetectDrift(
        ResourceState entraState,
        ResourceState providerState,
        string tenantId,
        string providerId)
    {
        var driftReports = new List<DriftReport>();

        // Compare users
        var entraUserDict = entraState.Users.ToDictionary(u => u.ExternalId ?? u.Id, u => u);
        var providerUserDict = providerState.Users.ToDictionary(u => u.ExternalId ?? u.Id, u => u);

        foreach (var (key, entraUser) in entraUserDict)
        {
            if (providerUserDict.TryGetValue(key, out var providerUser))
            {
                // Both exist - check for attribute mismatch
                var drifts = CompareUserAttributes(entraUser, providerUser).ToList();
                if (drifts.Any())
                {
                    var report = new DriftReport
                    {
                        TenantId = tenantId,
                        ProviderId = providerId,
                        DriftType = DriftType.AttributeMismatch,
                        ResourceType = "User",
                        ResourceId = entraUser.Id,
                        ExternalId = key,
                        ResourceDisplayName = entraUser.DisplayName ?? entraUser.UserName,
                        Summary = $"User '{key}' has {drifts.Count} attribute(s) with different values between Entra and provider",
                        Severity = drifts.Count > 3 ? DriftSeverity.High : DriftSeverity.Medium,
                        Details = new DriftDetails
                        {
                            DriftedAttributes = drifts,
                            EntraState = entraUser,
                            ProviderState = providerUser
                        }
                    };
                    driftReports.Add(report);
                }
            }
            else
            {
                // Exists in Entra but not in provider
                driftReports.Add(new DriftReport
                {
                    TenantId = tenantId,
                    ProviderId = providerId,
                    DriftType = DriftType.Deleted,
                    ResourceType = "User",
                    ResourceId = entraUser.Id,
                    ExternalId = key,
                    ResourceDisplayName = entraUser.DisplayName ?? entraUser.UserName,
                    Summary = $"User '{key}' exists in Entra but not in provider",
                    Severity = DriftSeverity.High,
                    Details = new DriftDetails { EntraState = entraUser }
                });
            }
        }

        // Check for users in provider but not in Entra
        foreach (var (key, providerUser) in providerUserDict)
        {
            if (!entraUserDict.ContainsKey(key))
            {
                driftReports.Add(new DriftReport
                {
                    TenantId = tenantId,
                    ProviderId = providerId,
                    DriftType = DriftType.Added,
                    ResourceType = "User",
                    ResourceId = providerUser.Id,
                    ExternalId = key,
                    ResourceDisplayName = providerUser.DisplayName ?? providerUser.UserName,
                    Summary = $"User '{key}' exists in provider but not in Entra",
                    Severity = DriftSeverity.Medium,
                    Details = new DriftDetails { ProviderState = providerUser }
                });
            }
        }

        // Compare groups (similar logic)
        var entraGroupDict = entraState.Groups.ToDictionary(g => g.ExternalId ?? g.Id, g => g);
        var providerGroupDict = providerState.Groups.ToDictionary(g => g.ExternalId ?? g.Id, g => g);

        foreach (var (key, entraGroup) in entraGroupDict)
        {
            if (providerGroupDict.TryGetValue(key, out var providerGroup))
            {
                var drifts = CompareGroupAttributes(entraGroup, providerGroup).ToList();
                var membershipDrift = CompareMembership(entraGroup, providerGroup);
                
                if (drifts.Any() || membershipDrift != null)
                {
                    var report = membershipDrift ?? new DriftReport
                    {
                        TenantId = tenantId,
                        ProviderId = providerId,
                        DriftType = DriftType.AttributeMismatch,
                        ResourceType = "Group",
                        ResourceId = entraGroup.Id,
                        ExternalId = key,
                        ResourceDisplayName = entraGroup.DisplayName,
                        Summary = $"Group '{key}' has differences between Entra and provider",
                        Details = new DriftDetails
                        {
                            DriftedAttributes = drifts,
                            EntraState = entraGroup,
                            ProviderState = providerGroup
                        }
                    };
                    
                    if (drifts.Any())
                    {
                        report.Details!.DriftedAttributes = drifts;
                    }
                    
                    driftReports.Add(report);
                }
            }
            else
            {
                driftReports.Add(new DriftReport
                {
                    TenantId = tenantId,
                    ProviderId = providerId,
                    DriftType = DriftType.Deleted,
                    ResourceType = "Group",
                    ResourceId = entraGroup.Id,
                    ExternalId = key,
                    ResourceDisplayName = entraGroup.DisplayName,
                    Summary = $"Group '{key}' exists in Entra but not in provider",
                    Severity = DriftSeverity.High,
                    Details = new DriftDetails { EntraState = entraGroup }
                });
            }
        }

        foreach (var (key, providerGroup) in providerGroupDict)
        {
            if (!entraGroupDict.ContainsKey(key))
            {
                driftReports.Add(new DriftReport
                {
                    TenantId = tenantId,
                    ProviderId = providerId,
                    DriftType = DriftType.Added,
                    ResourceType = "Group",
                    ResourceId = providerGroup.Id,
                    ExternalId = key,
                    ResourceDisplayName = providerGroup.DisplayName,
                    Summary = $"Group '{key}' exists in provider but not in Entra",
                    Severity = DriftSeverity.Medium,
                    Details = new DriftDetails { ProviderState = providerGroup }
                });
            }
        }

        _logger.LogDebug("Detected {Count} drift items between Entra and provider for tenant {TenantId}", 
            driftReports.Count, tenantId);

        return driftReports;
    }

    /// <inheritdoc />
    public IEnumerable<ConflictReport> DetectConflicts(
        IEnumerable<DriftReport> entraChanges,
        IEnumerable<DriftReport> providerChanges,
        string tenantId,
        string providerId)
    {
        var conflicts = new List<ConflictReport>();

        // Index changes by resource
        var entraChangesByResource = entraChanges
            .Where(c => c.DriftType == DriftType.Modified)
            .GroupBy(c => $"{c.ResourceType}:{c.ResourceId}")
            .ToDictionary(g => g.Key, g => g.First());

        var providerChangesByResource = providerChanges
            .Where(c => c.DriftType == DriftType.Modified)
            .GroupBy(c => $"{c.ResourceType}:{c.ResourceId}")
            .ToDictionary(g => g.Key, g => g.First());

        // Find resources modified in both systems (dual modification)
        var commonResources = entraChangesByResource.Keys.Intersect(providerChangesByResource.Keys);

        foreach (var resourceKey in commonResources)
        {
            var entraChange = entraChangesByResource[resourceKey];
            var providerChange = providerChangesByResource[resourceKey];

            // Check if they modified the same attributes
            var entraAttributes = entraChange.Details?.DriftedAttributes?.Select(a => a.AttributeName).ToHashSet() 
                                  ?? new HashSet<string>();
            var providerAttributes = providerChange.Details?.DriftedAttributes?.Select(a => a.AttributeName).ToHashSet() 
                                     ?? new HashSet<string>();
            
            var conflictingAttributes = entraAttributes.Intersect(providerAttributes).ToList();

            if (conflictingAttributes.Any())
            {
                var attributeConflicts = conflictingAttributes.Select(attr =>
                {
                    var entraAttr = entraChange.Details?.DriftedAttributes?.FirstOrDefault(a => a.AttributeName == attr);
                    var providerAttr = providerChange.Details?.DriftedAttributes?.FirstOrDefault(a => a.AttributeName == attr);
                    
                    return new AttributeConflict
                    {
                        AttributeName = attr,
                        EntraValue = entraAttr?.EntraValue,
                        ProviderValue = providerAttr?.ProviderValue
                    };
                }).ToList();

                conflicts.Add(ConflictReportFactory.CreateDualModificationConflict(
                    tenantId,
                    providerId,
                    entraChange.ResourceType,
                    entraChange.ResourceId,
                    new ChangeInfo
                    {
                        ChangeType = ChangeType.Update,
                        Timestamp = entraChange.Timestamp,
                        ChangedAttributes = entraAttributes.ToList()
                    },
                    new ChangeInfo
                    {
                        ChangeType = ChangeType.Update,
                        Timestamp = providerChange.Timestamp,
                        ChangedAttributes = providerAttributes.ToList()
                    },
                    attributeConflicts,
                    entraChange.ResourceDisplayName));
            }
        }

        // Check for delete-modify conflicts
        var entraDeletes = entraChanges
            .Where(c => c.DriftType == DriftType.Deleted)
            .ToDictionary(c => $"{c.ResourceType}:{c.ResourceId}", c => c);

        var providerDeletes = providerChanges
            .Where(c => c.DriftType == DriftType.Deleted)
            .ToDictionary(c => $"{c.ResourceType}:{c.ResourceId}", c => c);

        // Entra deleted but provider modified
        foreach (var (key, entraDelete) in entraDeletes)
        {
            if (providerChangesByResource.TryGetValue(key, out var providerModify))
            {
                conflicts.Add(ConflictReportFactory.CreateDeleteModifyConflict(
                    tenantId,
                    providerId,
                    entraDelete.ResourceType,
                    entraDelete.ResourceId,
                    deletedInEntra: true,
                    new ChangeInfo
                    {
                        ChangeType = ChangeType.Delete,
                        Timestamp = entraDelete.Timestamp
                    },
                    new ChangeInfo
                    {
                        ChangeType = ChangeType.Update,
                        Timestamp = providerModify.Timestamp,
                        ChangedAttributes = providerModify.Details?.DriftedAttributes?.Select(a => a.AttributeName).ToList()
                    },
                    entraDelete.ResourceDisplayName));
            }
        }

        // Provider deleted but Entra modified
        foreach (var (key, providerDelete) in providerDeletes)
        {
            if (entraChangesByResource.TryGetValue(key, out var entraModify))
            {
                conflicts.Add(ConflictReportFactory.CreateDeleteModifyConflict(
                    tenantId,
                    providerId,
                    providerDelete.ResourceType,
                    providerDelete.ResourceId,
                    deletedInEntra: false,
                    new ChangeInfo
                    {
                        ChangeType = ChangeType.Update,
                        Timestamp = entraModify.Timestamp,
                        ChangedAttributes = entraModify.Details?.DriftedAttributes?.Select(a => a.AttributeName).ToList()
                    },
                    new ChangeInfo
                    {
                        ChangeType = ChangeType.Delete,
                        Timestamp = providerDelete.Timestamp
                    },
                    providerDelete.ResourceDisplayName));
            }
        }

        _logger.LogDebug("Detected {Count} conflicts for tenant {TenantId}, provider {ProviderId}", 
            conflicts.Count, tenantId, providerId);

        return conflicts;
    }

    /// <inheritdoc />
    public IEnumerable<AttributeDrift> CompareAttributes(object previous, object current)
    {
        var drifts = new List<AttributeDrift>();
        
        if (previous == null || current == null)
            return drifts;

        var previousJson = JsonSerializer.Serialize(previous, _jsonOptions);
        var currentJson = JsonSerializer.Serialize(current, _jsonOptions);

        if (previousJson == currentJson)
            return drifts;

        // Parse as dictionaries and compare
        var previousDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(previousJson, _jsonOptions);
        var currentDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(currentJson, _jsonOptions);

        if (previousDict == null || currentDict == null)
            return drifts;

        foreach (var (key, currentValue) in currentDict)
        {
            if (DefaultIgnoredAttributes.Contains(key))
                continue;

            if (!previousDict.TryGetValue(key, out var previousValue))
            {
                // Attribute added
                drifts.Add(new AttributeDrift
                {
                    AttributeName = key,
                    Path = key,
                    EntraValue = null,
                    ProviderValue = GetJsonValue(currentValue)
                });
            }
            else if (previousValue.ToString() != currentValue.ToString())
            {
                // Attribute changed
                drifts.Add(new AttributeDrift
                {
                    AttributeName = key,
                    Path = key,
                    EntraValue = GetJsonValue(previousValue),
                    ProviderValue = GetJsonValue(currentValue),
                    IsMultiValued = currentValue.ValueKind == JsonValueKind.Array
                });
            }
        }

        // Check for removed attributes
        foreach (var (key, previousValue) in previousDict)
        {
            if (DefaultIgnoredAttributes.Contains(key))
                continue;

            if (!currentDict.ContainsKey(key))
            {
                drifts.Add(new AttributeDrift
                {
                    AttributeName = key,
                    Path = key,
                    EntraValue = GetJsonValue(previousValue),
                    ProviderValue = null
                });
            }
        }

        return drifts;
    }

    /// <inheritdoc />
    public string ComputeStateHash(IEnumerable<object> resources)
    {
        var json = JsonSerializer.Serialize(resources.OrderBy(r => 
        {
            var type = r.GetType();
            var idProp = type.GetProperty("Id");
            return idProp?.GetValue(r)?.ToString() ?? "";
        }), _jsonOptions);
        
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Compares user-specific attributes.
    /// </summary>
    private IEnumerable<AttributeDrift> CompareUserAttributes(ScimUser previous, ScimUser current)
    {
        var drifts = new List<AttributeDrift>();

        // Compare simple attributes
        if (previous.UserName != current.UserName)
        {
            drifts.Add(new AttributeDrift
            {
                AttributeName = "userName",
                Path = "userName",
                EntraValue = previous.UserName,
                ProviderValue = current.UserName
            });
        }

        if (previous.DisplayName != current.DisplayName)
        {
            drifts.Add(new AttributeDrift
            {
                AttributeName = "displayName",
                Path = "displayName",
                EntraValue = previous.DisplayName,
                ProviderValue = current.DisplayName
            });
        }

        if (previous.Active != current.Active)
        {
            drifts.Add(new AttributeDrift
            {
                AttributeName = "active",
                Path = "active",
                EntraValue = previous.Active,
                ProviderValue = current.Active
            });
        }

        // Compare name complex type
        if (previous.Name != null || current.Name != null)
        {
            if (previous.Name?.GivenName != current.Name?.GivenName)
            {
                drifts.Add(new AttributeDrift
                {
                    AttributeName = "name.givenName",
                    Path = "name.givenName",
                    EntraValue = previous.Name?.GivenName,
                    ProviderValue = current.Name?.GivenName
                });
            }

            if (previous.Name?.FamilyName != current.Name?.FamilyName)
            {
                drifts.Add(new AttributeDrift
                {
                    AttributeName = "name.familyName",
                    Path = "name.familyName",
                    EntraValue = previous.Name?.FamilyName,
                    ProviderValue = current.Name?.FamilyName
                });
            }
        }

        // Compare emails
        var previousEmails = previous.Emails?.Select(e => e.Value).OrderBy(v => v).ToList() ?? new List<string?>();
        var currentEmails = current.Emails?.Select(e => e.Value).OrderBy(v => v).ToList() ?? new List<string?>();
        
        if (!previousEmails.SequenceEqual(currentEmails))
        {
            drifts.Add(new AttributeDrift
            {
                AttributeName = "emails",
                Path = "emails",
                EntraValue = previousEmails,
                ProviderValue = currentEmails,
                IsMultiValued = true
            });
        }

        return drifts;
    }

    /// <summary>
    /// Compares group-specific attributes.
    /// </summary>
    private IEnumerable<AttributeDrift> CompareGroupAttributes(ScimGroup previous, ScimGroup current)
    {
        var drifts = new List<AttributeDrift>();

        if (previous.DisplayName != current.DisplayName)
        {
            drifts.Add(new AttributeDrift
            {
                AttributeName = "displayName",
                Path = "displayName",
                EntraValue = previous.DisplayName,
                ProviderValue = current.DisplayName
            });
        }

        if (previous.ExternalId != current.ExternalId)
        {
            drifts.Add(new AttributeDrift
            {
                AttributeName = "externalId",
                Path = "externalId",
                EntraValue = previous.ExternalId,
                ProviderValue = current.ExternalId
            });
        }

        return drifts;
    }

    /// <summary>
    /// Compares group membership.
    /// </summary>
    private DriftReport? CompareMembership(ScimGroup previous, ScimGroup current)
    {
        var previousMembers = previous.Members?.Select(m => m.Value).Where(v => v != null).Cast<string>().ToHashSet() ?? new HashSet<string>();
        var currentMembers = current.Members?.Select(m => m.Value).Where(v => v != null).Cast<string>().ToHashSet() ?? new HashSet<string>();

        var added = currentMembers.Except(previousMembers).ToList();
        var removed = previousMembers.Except(currentMembers).ToList();

        if (added.Any() || removed.Any())
        {
            return DriftReportFactory.CreateMembershipDrift(
                "", // Will be set by caller
                "", // Will be set by caller
                current.Id,
                added,
                removed,
                current.DisplayName);
        }

        return null;
    }

    /// <summary>
    /// Calculates statistics from drift reports.
    /// </summary>
    private void CalculateStatistics(ChangeDetectionResult result)
    {
        foreach (var drift in result.DriftReports)
        {
            switch (drift.ResourceType)
            {
                case "User":
                    switch (drift.DriftType)
                    {
                        case DriftType.Added:
                            result.UsersAdded++;
                            break;
                        case DriftType.Modified:
                        case DriftType.AttributeMismatch:
                            result.UsersModified++;
                            break;
                        case DriftType.Deleted:
                            result.UsersDeleted++;
                            break;
                    }
                    break;
                case "Group":
                    switch (drift.DriftType)
                    {
                        case DriftType.Added:
                            result.GroupsAdded++;
                            break;
                        case DriftType.Modified:
                        case DriftType.AttributeMismatch:
                        case DriftType.MembershipMismatch:
                            result.GroupsModified++;
                            break;
                        case DriftType.Deleted:
                            result.GroupsDeleted++;
                            break;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Extracts value from JsonElement.
    /// </summary>
    private static object? GetJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(GetJsonValue).ToList(),
            JsonValueKind.Object => element.ToString(),
            _ => element.ToString()
        };
    }
}
