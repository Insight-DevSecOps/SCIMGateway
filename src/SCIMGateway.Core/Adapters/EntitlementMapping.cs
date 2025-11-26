// ==========================================================================
// EntitlementMapping Type
// ==========================================================================
// Mapping between SCIM groups and provider entitlements
// per contracts/adapter-interface.md
// ==========================================================================

using System.Text.Json.Serialization;

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Mapping between SCIM groups and provider-specific entitlements.
/// </summary>
public class EntitlementMapping
{
    /// <summary>
    /// Provider ID (Salesforce, Workday, ServiceNow).
    /// </summary>
    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific entitlement ID (role ID, org ID, group ID).
    /// </summary>
    [JsonPropertyName("providerEntitlementId")]
    public string ProviderEntitlementId { get; set; } = string.Empty;

    /// <summary>
    /// Entitlement name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Entitlement type.
    /// </summary>
    [JsonPropertyName("type")]
    public EntitlementType Type { get; set; } = EntitlementType.GROUP;

    /// <summary>
    /// List of SCIM group IDs mapped to this entitlement.
    /// </summary>
    [JsonPropertyName("mappedGroups")]
    public List<string> MappedGroups { get; set; } = [];

    /// <summary>
    /// Priority for conflict resolution (lower = higher priority).
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Whether this mapping is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Additional metadata (provider-specific).
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = [];
}

/// <summary>
/// Type of entitlement in the provider system.
/// </summary>
public enum EntitlementType
{
    /// <summary>
    /// Role-based entitlement (e.g., Salesforce Role).
    /// </summary>
    ROLE,

    /// <summary>
    /// Permission set (e.g., Salesforce Permission Set).
    /// </summary>
    PERMISSION_SET,

    /// <summary>
    /// Organizational hierarchy level (e.g., Workday Org Unit).
    /// </summary>
    ORG_HIERARCHY_LEVEL,

    /// <summary>
    /// Native group (e.g., ServiceNow Group).
    /// </summary>
    GROUP,

    /// <summary>
    /// Department assignment.
    /// </summary>
    DEPARTMENT,

    /// <summary>
    /// Custom entitlement type.
    /// </summary>
    CUSTOM
}
