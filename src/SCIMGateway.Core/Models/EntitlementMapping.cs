// ==========================================================================
// T020: EntitlementMapping Model
// ==========================================================================
// Model for mapping SCIM groups to provider-specific entitlements
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models;

/// <summary>
/// Entitlement mapping model for group/entitlement transformation.
/// </summary>
public class EntitlementMapping
{
    /// <summary>
    /// Unique identifier for this mapping.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tenant identifier for multi-tenant isolation.
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Provider identifier (e.g., "salesforce", "workday").
    /// </summary>
    [JsonPropertyName("providerId")]
    [JsonProperty("providerId")]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific entitlement ID.
    /// </summary>
    [JsonPropertyName("providerEntitlementId")]
    [JsonProperty("providerEntitlementId")]
    public string ProviderEntitlementId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for this entitlement.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of entitlement.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public EntitlementType Type { get; set; } = EntitlementType.Role;

    /// <summary>
    /// SCIM groups that map to this entitlement.
    /// </summary>
    [JsonPropertyName("mappedGroups")]
    [JsonProperty("mappedGroups")]
    public List<MappedGroup> MappedGroups { get; set; } = [];

    /// <summary>
    /// Priority for conflict resolution (lower = higher priority).
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonProperty("priority")]
    public int Priority { get; set; }

    /// <summary>
    /// Description of this entitlement mapping.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this mapping is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Additional metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// When this mapping was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this mapping was last modified.
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    [JsonProperty("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Type of entitlement in the target provider.
/// </summary>
public enum EntitlementType
{
    /// <summary>
    /// Role-based entitlement (e.g., Salesforce Profile, Workday Security Group).
    /// </summary>
    Role,

    /// <summary>
    /// Permission set (e.g., Salesforce Permission Set).
    /// </summary>
    PermissionSet,

    /// <summary>
    /// Profile-based entitlement.
    /// </summary>
    Profile,

    /// <summary>
    /// Organizational unit placement.
    /// </summary>
    OrgUnit,

    /// <summary>
    /// Native group (provider supports groups natively).
    /// </summary>
    Group,

    /// <summary>
    /// License assignment.
    /// </summary>
    License,

    /// <summary>
    /// Custom entitlement type.
    /// </summary>
    Custom
}

/// <summary>
/// A SCIM group mapped to an entitlement.
/// </summary>
public class MappedGroup
{
    /// <summary>
    /// SCIM group ID.
    /// </summary>
    [JsonPropertyName("groupId")]
    [JsonProperty("groupId")]
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// SCIM group display name.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Whether this is an exact match or pattern-based.
    /// </summary>
    [JsonPropertyName("matchType")]
    [JsonProperty("matchType")]
    public GroupMatchType MatchType { get; set; } = GroupMatchType.Exact;

    /// <summary>
    /// Pattern for pattern-based matching.
    /// </summary>
    [JsonPropertyName("pattern")]
    [JsonProperty("pattern")]
    public string? Pattern { get; set; }
}

/// <summary>
/// Type of group matching for entitlement mapping.
/// </summary>
public enum GroupMatchType
{
    /// <summary>
    /// Exact match on group ID or display name.
    /// </summary>
    Exact,

    /// <summary>
    /// Pattern-based matching using transformation rules.
    /// </summary>
    Pattern,

    /// <summary>
    /// Hierarchical matching based on group path.
    /// </summary>
    Hierarchical
}
