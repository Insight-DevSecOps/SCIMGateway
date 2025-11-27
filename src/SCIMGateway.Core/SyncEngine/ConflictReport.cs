// ==========================================================================
// T119: ConflictReport Model
// ==========================================================================
// Report model for conflicts detected when both Entra ID and provider
// have changes to the same resource
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.SyncEngine;

/// <summary>
/// Report model for detected conflicts between Entra ID and provider changes.
/// Conflicts occur when the same resource has been modified in both systems.
/// </summary>
public class ConflictReport
{
    /// <summary>
    /// Unique identifier for this conflict report.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Unique identifier for the conflict (alias for Id for consistency).
    /// </summary>
    [JsonPropertyName("conflictId")]
    [JsonProperty("conflictId")]
    public string ConflictId => Id;

    /// <summary>
    /// Tenant identifier for multi-tenant isolation.
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Provider identifier where conflict was detected.
    /// </summary>
    [JsonPropertyName("providerId")]
    [JsonProperty("providerId")]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the conflict was detected.
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the conflict was detected (alias for Timestamp).
    /// </summary>
    [JsonPropertyName("detectedAt")]
    [JsonProperty("detectedAt")]
    public DateTime DetectedAt => Timestamp;

    /// <summary>
    /// Type of conflict detected.
    /// </summary>
    [JsonPropertyName("conflictType")]
    [JsonProperty("conflictType")]
    public ConflictType ConflictType { get; set; }

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
    /// Change detected in Entra ID.
    /// </summary>
    [JsonPropertyName("entraChange")]
    [JsonProperty("entraChange")]
    public ChangeInfo? EntraChange { get; set; }

    /// <summary>
    /// Change detected in the provider.
    /// </summary>
    [JsonPropertyName("providerChange")]
    [JsonProperty("providerChange")]
    public ChangeInfo? ProviderChange { get; set; }

    /// <summary>
    /// Detailed information about the conflicting attributes.
    /// </summary>
    [JsonPropertyName("conflictingAttributes")]
    [JsonProperty("conflictingAttributes")]
    public List<AttributeConflict>? ConflictingAttributes { get; set; }

    /// <summary>
    /// Summary description of the conflict.
    /// </summary>
    [JsonPropertyName("summary")]
    [JsonProperty("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Severity level of the conflict.
    /// </summary>
    [JsonPropertyName("severity")]
    [JsonProperty("severity")]
    public ConflictSeverity Severity { get; set; } = ConflictSeverity.Medium;

    /// <summary>
    /// Suggested resolution for this conflict.
    /// </summary>
    [JsonPropertyName("suggestedResolution")]
    [JsonProperty("suggestedResolution")]
    public ConflictResolution? SuggestedResolution { get; set; }

    /// <summary>
    /// Resolution that was applied.
    /// </summary>
    [JsonPropertyName("resolution")]
    [JsonProperty("resolution")]
    public ConflictResolution? Resolution { get; set; }

    /// <summary>
    /// Whether this conflict has been resolved.
    /// </summary>
    [JsonPropertyName("resolved")]
    [JsonProperty("resolved")]
    public bool Resolved { get; set; }

    /// <summary>
    /// When the conflict was resolved.
    /// </summary>
    [JsonPropertyName("resolvedAt")]
    [JsonProperty("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// User or system that resolved the conflict.
    /// </summary>
    [JsonPropertyName("resolvedBy")]
    [JsonProperty("resolvedBy")]
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Notes about the resolution.
    /// </summary>
    [JsonPropertyName("resolutionNotes")]
    [JsonProperty("resolutionNotes")]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Whether sync is blocked for this resource until conflict is resolved.
    /// </summary>
    [JsonPropertyName("syncBlocked")]
    [JsonProperty("syncBlocked")]
    public bool SyncBlocked { get; set; } = true;

    /// <summary>
    /// Number of times this conflict has been escalated.
    /// </summary>
    [JsonPropertyName("escalationCount")]
    [JsonProperty("escalationCount")]
    public int EscalationCount { get; set; }

    /// <summary>
    /// When this conflict was last escalated.
    /// </summary>
    [JsonPropertyName("lastEscalatedAt")]
    [JsonProperty("lastEscalatedAt")]
    public DateTime? LastEscalatedAt { get; set; }

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
/// Information about a change in one system.
/// </summary>
public class ChangeInfo
{
    /// <summary>
    /// Type of change (Create, Update, Delete).
    /// </summary>
    [JsonPropertyName("changeType")]
    [JsonProperty("changeType")]
    public ChangeType ChangeType { get; set; }

    /// <summary>
    /// When the change was detected.
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User or system that made the change.
    /// </summary>
    [JsonPropertyName("changedBy")]
    [JsonProperty("changedBy")]
    public string? ChangedBy { get; set; }

    /// <summary>
    /// Previous value before the change.
    /// </summary>
    [JsonPropertyName("previousValue")]
    [JsonProperty("previousValue")]
    public object? PreviousValue { get; set; }

    /// <summary>
    /// New value after the change.
    /// </summary>
    [JsonPropertyName("newValue")]
    [JsonProperty("newValue")]
    public object? NewValue { get; set; }

    /// <summary>
    /// Attributes that were changed.
    /// </summary>
    [JsonPropertyName("changedAttributes")]
    [JsonProperty("changedAttributes")]
    public List<string>? ChangedAttributes { get; set; }

    /// <summary>
    /// Hash of the state after change.
    /// </summary>
    [JsonPropertyName("stateHash")]
    [JsonProperty("stateHash")]
    public string? StateHash { get; set; }
}

/// <summary>
/// Type of change.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// Resource was created.
    /// </summary>
    Create,

    /// <summary>
    /// Resource was updated.
    /// </summary>
    Update,

    /// <summary>
    /// Resource was deleted.
    /// </summary>
    Delete,

    /// <summary>
    /// Resource membership was modified.
    /// </summary>
    MembershipChange
}

/// <summary>
/// Information about a specific attribute conflict.
/// </summary>
public class AttributeConflict
{
    /// <summary>
    /// Name of the conflicting attribute.
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
    /// Original value before both changes (if known).
    /// </summary>
    [JsonPropertyName("originalValue")]
    [JsonProperty("originalValue")]
    public object? OriginalValue { get; set; }

    /// <summary>
    /// Suggested resolution for this attribute.
    /// </summary>
    [JsonPropertyName("suggestedValue")]
    [JsonProperty("suggestedValue")]
    public object? SuggestedValue { get; set; }

    /// <summary>
    /// Whether this is a multi-valued attribute.
    /// </summary>
    [JsonPropertyName("isMultiValued")]
    [JsonProperty("isMultiValued")]
    public bool IsMultiValued { get; set; }
}

/// <summary>
/// Severity level of conflict.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>
    /// Low severity - can be auto-resolved in most cases.
    /// </summary>
    Low,

    /// <summary>
    /// Medium severity - requires review but not urgent.
    /// </summary>
    Medium,

    /// <summary>
    /// High severity - requires prompt attention.
    /// </summary>
    High,

    /// <summary>
    /// Critical severity - immediate resolution required.
    /// </summary>
    Critical
}

/// <summary>
/// Resolution strategy for a conflict.
/// </summary>
public enum ConflictResolution
{
    /// <summary>
    /// Use Entra ID value.
    /// </summary>
    UseEntraValue,

    /// <summary>
    /// Use provider value.
    /// </summary>
    UseProviderValue,

    /// <summary>
    /// Merge both values (for multi-valued attributes).
    /// </summary>
    MergeValues,

    /// <summary>
    /// Keep the most recent value.
    /// </summary>
    UseMostRecent,

    /// <summary>
    /// Ignore the conflict (no action taken).
    /// </summary>
    Ignore,

    /// <summary>
    /// Custom resolution applied manually.
    /// </summary>
    Custom
}

/// <summary>
/// Factory for creating ConflictReport instances.
/// </summary>
public static class ConflictReportFactory
{
    /// <summary>
    /// Creates a conflict report for dual modification (same resource modified in both systems).
    /// </summary>
    public static ConflictReport CreateDualModificationConflict(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        ChangeInfo entraChange,
        ChangeInfo providerChange,
        List<AttributeConflict>? conflictingAttributes = null,
        string? displayName = null)
    {
        var attributeCount = conflictingAttributes?.Count ?? 0;
        var severity = attributeCount switch
        {
            0 => ConflictSeverity.Low,
            1 => ConflictSeverity.Medium,
            <= 3 => ConflictSeverity.Medium,
            _ => ConflictSeverity.High
        };

        return new ConflictReport
        {
            TenantId = tenantId,
            ProviderId = providerId,
            ConflictType = ConflictType.DualModification,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceDisplayName = displayName,
            EntraChange = entraChange,
            ProviderChange = providerChange,
            ConflictingAttributes = conflictingAttributes,
            Summary = $"{resourceType} '{resourceId}' was modified in both Entra ID and provider ({attributeCount} conflicting attributes)",
            Severity = severity,
            SuggestedResolution = ConflictResolution.UseMostRecent,
            SyncBlocked = true
        };
    }

    /// <summary>
    /// Creates a conflict report for delete-modify conflict.
    /// </summary>
    public static ConflictReport CreateDeleteModifyConflict(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        bool deletedInEntra,
        ChangeInfo deleteChange,
        ChangeInfo modifyChange,
        string? displayName = null)
    {
        var entraChange = deletedInEntra ? deleteChange : modifyChange;
        var providerChange = deletedInEntra ? modifyChange : deleteChange;
        var deletedIn = deletedInEntra ? "Entra ID" : "provider";
        var modifiedIn = deletedInEntra ? "provider" : "Entra ID";

        return new ConflictReport
        {
            TenantId = tenantId,
            ProviderId = providerId,
            ConflictType = ConflictType.DeleteModifyConflict,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceDisplayName = displayName,
            EntraChange = entraChange,
            ProviderChange = providerChange,
            Summary = $"{resourceType} '{resourceId}' was deleted in {deletedIn} but modified in {modifiedIn}",
            Severity = ConflictSeverity.High,
            SuggestedResolution = deletedInEntra ? ConflictResolution.UseEntraValue : ConflictResolution.UseProviderValue,
            SyncBlocked = true
        };
    }

    /// <summary>
    /// Creates a conflict report for uniqueness violation.
    /// </summary>
    public static ConflictReport CreateUniquenessConflict(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        string conflictingAttribute,
        object? conflictingValue,
        string? existingResourceId = null,
        string? displayName = null)
    {
        return new ConflictReport
        {
            TenantId = tenantId,
            ProviderId = providerId,
            ConflictType = ConflictType.UniquenessViolation,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceDisplayName = displayName,
            ConflictingAttributes = new List<AttributeConflict>
            {
                new()
                {
                    AttributeName = conflictingAttribute,
                    EntraValue = conflictingValue,
                    ProviderValue = conflictingValue
                }
            },
            Summary = $"{resourceType} '{resourceId}' has uniqueness conflict on '{conflictingAttribute}'" +
                      (existingResourceId != null ? $" (conflicts with '{existingResourceId}')" : ""),
            Severity = ConflictSeverity.High,
            SyncBlocked = true
        };
    }

    /// <summary>
    /// Creates a conflict report for transformation conflict.
    /// </summary>
    public static ConflictReport CreateTransformationConflict(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        string transformationRuleId,
        string errorMessage,
        string? displayName = null)
    {
        return new ConflictReport
        {
            TenantId = tenantId,
            ProviderId = providerId,
            ConflictType = ConflictType.TransformationConflict,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceDisplayName = displayName,
            Summary = $"Transformation rule '{transformationRuleId}' failed for {resourceType} '{resourceId}': {errorMessage}",
            Severity = ConflictSeverity.Medium,
            ResolutionNotes = $"Transformation error: {errorMessage}",
            SyncBlocked = false // Transformation conflicts might not block sync
        };
    }
}
