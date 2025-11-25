// ==========================================================================
// T019: ScimGroup Model - RFC 7643 Compliant Group Schema
// ==========================================================================
// SCIM 2.0 Group resource model per RFC 7643
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models;

/// <summary>
/// SCIM 2.0 Group resource per RFC 7643.
/// </summary>
public class ScimGroup
{
    /// <summary>
    /// SCIM schemas included in this resource.
    /// </summary>
    [JsonPropertyName("schemas")]
    [JsonProperty("schemas")]
    public List<string> Schemas { get; set; } =
    [
        ScimConstants.Schemas.Group
    ];

    /// <summary>
    /// Unique identifier for the resource (assigned by service provider).
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Identifier from the provisioning client.
    /// </summary>
    [JsonPropertyName("externalId")]
    [JsonProperty("externalId")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Human-readable name for the group (required).
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Members of this group.
    /// </summary>
    [JsonPropertyName("members")]
    [JsonProperty("members")]
    public List<ScimGroupMember>? Members { get; set; }

    /// <summary>
    /// Resource metadata.
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonProperty("meta")]
    public ScimMeta? Meta { get; set; }

    // ===== Internal SDK Attributes (not part of SCIM spec) =====

    /// <summary>
    /// Internal: Tenant identifier for multi-tenant isolation.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? TenantId { get; set; }

    /// <summary>
    /// Internal: Mappings to provider-specific representations.
    /// Key is providerId, value is provider-specific group/role/entitlement ID.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Dictionary<string, ProviderMapping>? ProviderMappings { get; set; }

    /// <summary>
    /// Internal: Entitlement mapping for transformation engine.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public EntitlementMappingReference? EntitlementMapping { get; set; }
}

/// <summary>
/// SCIM Group Member complex type per RFC 7643.
/// </summary>
public class ScimGroupMember
{
    /// <summary>
    /// Member's ID.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string? Value { get; set; }

    /// <summary>
    /// URI reference to the member resource.
    /// Maps to "$ref" in JSON.
    /// </summary>
    [JsonPropertyName("$ref")]
    [JsonProperty("$ref")]
    public string? Ref { get; set; }

    /// <summary>
    /// Alias for Ref to support both property names.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string? Reference
    {
        get => Ref;
        set => Ref = value;
    }

    /// <summary>
    /// Type of member ("User" or "Group").
    /// </summary>
    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Display name of the member.
    /// </summary>
    [JsonPropertyName("display")]
    [JsonProperty("display")]
    public string? Display { get; set; }
}

/// <summary>
/// Mapping to a provider-specific representation.
/// </summary>
public class ProviderMapping
{
    /// <summary>
    /// Provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific resource ID.
    /// </summary>
    public string ProviderResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Type of provider resource (e.g., "Role", "PermissionSet", "Group").
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Additional provider-specific metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Last sync timestamp.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
}

/// <summary>
/// Reference to an entitlement mapping rule.
/// </summary>
public class EntitlementMappingReference
{
    /// <summary>
    /// ID of the entitlement mapping.
    /// </summary>
    public string MappingId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the transformation rule used.
    /// </summary>
    public string? TransformationRuleId { get; set; }

    /// <summary>
    /// When the mapping was applied.
    /// </summary>
    public DateTime? AppliedAt { get; set; }
}
