// ==========================================================================
// T087: Entitlement Model
// ==========================================================================
// Model for provider-specific entitlements (roles, permissions, org units)
// Result of transformation from SCIM Groups
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models.Transformations;

/// <summary>
/// Represents a provider-specific entitlement (role, permission, org unit, etc.).
/// This is the result of transforming a SCIM Group.
/// </summary>
public class Entitlement
{
    /// <summary>
    /// Provider-specific entitlement ID (e.g., Salesforce role ID).
    /// </summary>
    [JsonPropertyName("providerEntitlementId")]
    [JsonProperty("providerEntitlementId")]
    public string ProviderEntitlementId { get; set; } = string.Empty;

    /// <summary>
    /// Entitlement display name (e.g., "Sales_Representative").
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Entitlement type (ROLE, PERMISSION, ORG_UNIT, GROUP, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public EntitlementType Type { get; set; } = EntitlementType.ROLE;

    /// <summary>
    /// Source SCIM groups that mapped to this entitlement.
    /// </summary>
    [JsonPropertyName("mappedGroups")]
    [JsonProperty("mappedGroups")]
    public List<string> MappedGroups { get; set; } = [];

    /// <summary>
    /// Priority (from transformation rule, lower = higher priority).
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonProperty("priority")]
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Additional metadata (provider-specific attributes).
    /// May contain privilege level for HIGHEST_PRIVILEGE conflict resolution.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonProperty("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// The rule ID that generated this entitlement.
    /// </summary>
    [JsonPropertyName("sourceRuleId")]
    [JsonProperty("sourceRuleId")]
    public string? SourceRuleId { get; set; }

    /// <summary>
    /// When this entitlement was assigned (null for preview).
    /// </summary>
    [JsonPropertyName("appliedAt")]
    [JsonProperty("appliedAt")]
    public DateTime? AppliedAt { get; set; }
}

/// <summary>
/// Types of entitlements in target providers.
/// </summary>
public enum EntitlementType
{
    /// <summary>
    /// Salesforce Role.
    /// </summary>
    ROLE,

    /// <summary>
    /// Salesforce Permission Set.
    /// </summary>
    PERMISSION_SET,

    /// <summary>
    /// Salesforce Profile (immutable).
    /// </summary>
    PROFILE,

    /// <summary>
    /// Workday Organization Unit.
    /// </summary>
    ORG_UNIT,

    /// <summary>
    /// ServiceNow Group (sys_user_group).
    /// </summary>
    GROUP,

    /// <summary>
    /// Generic permission.
    /// </summary>
    PERMISSION,

    /// <summary>
    /// License assignment.
    /// </summary>
    LICENSE,

    /// <summary>
    /// Provider-specific custom type.
    /// </summary>
    CUSTOM
}
