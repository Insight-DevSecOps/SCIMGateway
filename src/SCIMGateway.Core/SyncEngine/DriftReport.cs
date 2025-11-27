// ==========================================================================
// T118: DriftReport Model
// ==========================================================================
// Report model for drift detection results between Entra ID and providers
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.SyncEngine;

/// <summary>
/// Report model for detected drift between Entra ID and provider state.
/// Represents a comprehensive report of detected changes that need attention.
/// </summary>
public class DriftReport
{
    /// <summary>
    /// Unique identifier for this drift report.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Unique identifier for the drift event (alias for Id for consistency).
    /// </summary>
    [JsonPropertyName("driftId")]
    [JsonProperty("driftId")]
    public string DriftId => Id;

    /// <summary>
    /// Tenant identifier for multi-tenant isolation.
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Provider identifier where drift was detected.
    /// </summary>
    [JsonPropertyName("providerId")]
    [JsonProperty("providerId")]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the drift was detected.
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the drift was detected (alias for Timestamp).
    /// </summary>
    [JsonPropertyName("detectedAt")]
    [JsonProperty("detectedAt")]
    public DateTime DetectedAt => Timestamp;

    /// <summary>
    /// Type of drift detected.
    /// </summary>
    [JsonPropertyName("driftType")]
    [JsonProperty("driftType")]
    public DriftType DriftType { get; set; }

    /// <summary>
    /// Type of resource affected (User, Group).
    /// </summary>
    [JsonPropertyName("resourceType")]
    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected resource.
    /// </summary>
    [JsonPropertyName("resourceId")]
    [JsonProperty("resourceId")]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// External ID of the affected resource (if available).
    /// </summary>
    [JsonPropertyName("externalId")]
    [JsonProperty("externalId")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Resource display name for reference.
    /// </summary>
    [JsonPropertyName("resourceDisplayName")]
    [JsonProperty("resourceDisplayName")]
    public string? ResourceDisplayName { get; set; }

    /// <summary>
    /// Detailed information about the drift.
    /// </summary>
    [JsonPropertyName("details")]
    [JsonProperty("details")]
    public DriftDetails? Details { get; set; }

    /// <summary>
    /// Summary description of the drift.
    /// </summary>
    [JsonPropertyName("summary")]
    [JsonProperty("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Severity level of the drift.
    /// </summary>
    [JsonPropertyName("severity")]
    [JsonProperty("severity")]
    public DriftSeverity Severity { get; set; } = DriftSeverity.Low;

    /// <summary>
    /// Whether this drift has been reconciled.
    /// </summary>
    [JsonPropertyName("reconciled")]
    [JsonProperty("reconciled")]
    public bool Reconciled { get; set; }

    /// <summary>
    /// When the drift was reconciled.
    /// </summary>
    [JsonPropertyName("reconciledAt")]
    [JsonProperty("reconciledAt")]
    public DateTime? ReconciledAt { get; set; }

    /// <summary>
    /// Action taken to reconcile the drift.
    /// </summary>
    [JsonPropertyName("reconciliationAction")]
    [JsonProperty("reconciliationAction")]
    public ReconciliationAction? ReconciliationAction { get; set; }

    /// <summary>
    /// User or system that performed the reconciliation.
    /// </summary>
    [JsonPropertyName("reconciledBy")]
    [JsonProperty("reconciledBy")]
    public string? ReconciledBy { get; set; }

    /// <summary>
    /// Notes about the reconciliation.
    /// </summary>
    [JsonPropertyName("reconciliationNotes")]
    [JsonProperty("reconciliationNotes")]
    public string? ReconciliationNotes { get; set; }

    /// <summary>
    /// When this report was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this report was last modified.
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    [JsonProperty("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detailed information about detected drift.
/// </summary>
public class DriftDetails
{
    /// <summary>
    /// Attributes that have drifted.
    /// </summary>
    [JsonPropertyName("driftedAttributes")]
    [JsonProperty("driftedAttributes")]
    public List<AttributeDrift>? DriftedAttributes { get; set; }

    /// <summary>
    /// The Entra ID state of the resource.
    /// </summary>
    [JsonPropertyName("entraState")]
    [JsonProperty("entraState")]
    public object? EntraState { get; set; }

    /// <summary>
    /// The provider state of the resource.
    /// </summary>
    [JsonPropertyName("providerState")]
    [JsonProperty("providerState")]
    public object? ProviderState { get; set; }

    /// <summary>
    /// Hash of the Entra state for comparison.
    /// </summary>
    [JsonPropertyName("entraStateHash")]
    [JsonProperty("entraStateHash")]
    public string? EntraStateHash { get; set; }

    /// <summary>
    /// Hash of the provider state for comparison.
    /// </summary>
    [JsonPropertyName("providerStateHash")]
    [JsonProperty("providerStateHash")]
    public string? ProviderStateHash { get; set; }

    /// <summary>
    /// Members added (for group membership drift).
    /// </summary>
    [JsonPropertyName("membersAdded")]
    [JsonProperty("membersAdded")]
    public List<string>? MembersAdded { get; set; }

    /// <summary>
    /// Members removed (for group membership drift).
    /// </summary>
    [JsonPropertyName("membersRemoved")]
    [JsonProperty("membersRemoved")]
    public List<string>? MembersRemoved { get; set; }
}

/// <summary>
/// Information about a specific attribute drift.
/// </summary>
public class AttributeDrift
{
    /// <summary>
    /// Name of the attribute that drifted.
    /// </summary>
    [JsonPropertyName("attributeName")]
    [JsonProperty("attributeName")]
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// SCIM path to the attribute.
    /// </summary>
    [JsonPropertyName("path")]
    [JsonProperty("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Value in Entra ID.
    /// </summary>
    [JsonPropertyName("entraValue")]
    [JsonProperty("entraValue")]
    public object? EntraValue { get; set; }

    /// <summary>
    /// Value in provider.
    /// </summary>
    [JsonPropertyName("providerValue")]
    [JsonProperty("providerValue")]
    public object? ProviderValue { get; set; }

    /// <summary>
    /// Whether this is a multi-valued attribute.
    /// </summary>
    [JsonPropertyName("isMultiValued")]
    [JsonProperty("isMultiValued")]
    public bool IsMultiValued { get; set; }
}

/// <summary>
/// Severity level of drift.
/// </summary>
public enum DriftSeverity
{
    /// <summary>
    /// Low severity - informational only.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity - should be reviewed.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity - requires attention.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity - immediate action required.
    /// </summary>
    Critical
}

/// <summary>
/// Action taken to reconcile drift.
/// </summary>
public enum ReconciliationAction
{
    /// <summary>
    /// Automatically applied based on sync direction.
    /// </summary>
    AutoApplied,

    /// <summary>
    /// Manually reviewed and approved.
    /// </summary>
    ManualApproved,

    /// <summary>
    /// Manually reviewed and rejected.
    /// </summary>
    ManualRejected,

    /// <summary>
    /// Drift was ignored per policy.
    /// </summary>
    Ignored,

    /// <summary>
    /// Drift was rolled back.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Custom reconciliation action.
    /// </summary>
    Custom
}

/// <summary>
/// Factory for creating DriftReport instances.
/// </summary>
public static class DriftReportFactory
{
    /// <summary>
    /// Creates a drift report for a newly added resource.
    /// </summary>
    public static DriftReport CreateAddedDrift(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        object? providerState = null,
        string? displayName = null)
    {
        return new DriftReport
        {
            TenantId = tenantId,
            ProviderId = providerId,
            DriftType = DriftType.Added,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceDisplayName = displayName,
            Summary = $"{resourceType} '{resourceId}' was added in provider but not in Entra ID",
            Severity = DriftSeverity.Medium,
            Details = new DriftDetails
            {
                ProviderState = providerState
            }
        };
    }

    /// <summary>
    /// Creates a drift report for a modified resource.
    /// </summary>
    public static DriftReport CreateModifiedDrift(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        List<AttributeDrift>? driftedAttributes = null,
        object? entraState = null,
        object? providerState = null,
        string? displayName = null)
    {
        var attributeCount = driftedAttributes?.Count ?? 0;
        var severity = attributeCount switch
        {
            0 => DriftSeverity.Low,
            1 => DriftSeverity.Low,
            <= 3 => DriftSeverity.Medium,
            _ => DriftSeverity.High
        };

        return new DriftReport
        {
            TenantId = tenantId,
            ProviderId = providerId,
            DriftType = DriftType.Modified,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceDisplayName = displayName,
            Summary = $"{resourceType} '{resourceId}' has {attributeCount} drifted attribute(s)",
            Severity = severity,
            Details = new DriftDetails
            {
                DriftedAttributes = driftedAttributes,
                EntraState = entraState,
                ProviderState = providerState
            }
        };
    }

    /// <summary>
    /// Creates a drift report for a deleted resource.
    /// </summary>
    public static DriftReport CreateDeletedDrift(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        object? entraState = null,
        string? displayName = null)
    {
        return new DriftReport
        {
            TenantId = tenantId,
            ProviderId = providerId,
            DriftType = DriftType.Deleted,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceDisplayName = displayName,
            Summary = $"{resourceType} '{resourceId}' was deleted in provider but exists in Entra ID",
            Severity = DriftSeverity.High,
            Details = new DriftDetails
            {
                EntraState = entraState
            }
        };
    }

    /// <summary>
    /// Creates a drift report for a membership mismatch.
    /// </summary>
    public static DriftReport CreateMembershipDrift(
        string tenantId,
        string providerId,
        string groupId,
        List<string>? membersAdded = null,
        List<string>? membersRemoved = null,
        string? displayName = null)
    {
        var addedCount = membersAdded?.Count ?? 0;
        var removedCount = membersRemoved?.Count ?? 0;
        var totalChanges = addedCount + removedCount;
        var severity = totalChanges switch
        {
            0 => DriftSeverity.Low,
            <= 5 => DriftSeverity.Medium,
            _ => DriftSeverity.High
        };

        return new DriftReport
        {
            TenantId = tenantId,
            ProviderId = providerId,
            DriftType = DriftType.MembershipMismatch,
            ResourceType = "Group",
            ResourceId = groupId,
            ResourceDisplayName = displayName,
            Summary = $"Group '{groupId}' has membership drift: {addedCount} added, {removedCount} removed in provider",
            Severity = severity,
            Details = new DriftDetails
            {
                MembersAdded = membersAdded,
                MembersRemoved = membersRemoved
            }
        };
    }
}
