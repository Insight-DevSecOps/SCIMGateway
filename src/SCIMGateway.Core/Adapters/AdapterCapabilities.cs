// ==========================================================================
// T071: AdapterCapabilities Type
// ==========================================================================
// Capability flags for adapters per contracts/adapter-interface.md
// ==========================================================================

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Capabilities and supported features of an adapter.
/// </summary>
public class AdapterCapabilities
{
    /// <summary>
    /// Whether the adapter supports user CRUD operations.
    /// </summary>
    public bool SupportsUsers { get; set; } = true;

    /// <summary>
    /// Whether the adapter supports group CRUD operations.
    /// </summary>
    public bool SupportsGroups { get; set; } = true;

    /// <summary>
    /// Whether the adapter supports SCIM filter expressions.
    /// </summary>
    public bool SupportsFiltering { get; set; } = true;

    /// <summary>
    /// Whether the adapter supports sorting.
    /// </summary>
    public bool SupportsSorting { get; set; } = true;

    /// <summary>
    /// Whether the adapter supports pagination.
    /// </summary>
    public bool SupportsPagination { get; set; } = true;

    /// <summary>
    /// Whether the adapter supports PATCH operations.
    /// </summary>
    public bool SupportsPatch { get; set; } = true;

    /// <summary>
    /// Whether the adapter supports bulk operations.
    /// </summary>
    public bool SupportsBulk { get; set; } = false;

    /// <summary>
    /// Whether the adapter supports the enterprise user extension.
    /// </summary>
    public bool SupportsEnterpriseExtension { get; set; } = true;

    /// <summary>
    /// Maximum page size for list operations.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Supported filter operators.
    /// </summary>
    public List<string> SupportedFilterOperators { get; set; } =
    [
        "eq", "ne", "co", "sw", "ew", "pr", "gt", "ge", "lt", "le", "and", "or", "not"
    ];

    /// <summary>
    /// Supported authentication methods.
    /// </summary>
    public List<string> SupportedAuthMethods { get; set; } =
    [
        "OAuth2", "ApiKey", "Basic"
    ];

    /// <summary>
    /// Whether the adapter supports roles/entitlements.
    /// </summary>
    public bool SupportsRoles { get; set; } = false;

    /// <summary>
    /// Whether the adapter supports organizational hierarchy.
    /// </summary>
    public bool SupportsOrgHierarchy { get; set; } = false;

    /// <summary>
    /// Whether the adapter supports custom attributes.
    /// </summary>
    public bool SupportsCustomAttributes { get; set; } = false;

    /// <summary>
    /// Whether the adapter supports delta queries (change detection).
    /// </summary>
    public bool SupportsDeltaQuery { get; set; } = false;

    /// <summary>
    /// Creates default capabilities.
    /// </summary>
    public static AdapterCapabilities Default() => new();

    /// <summary>
    /// Creates capabilities for a read-only adapter.
    /// </summary>
    public static AdapterCapabilities ReadOnly() => new()
    {
        SupportsUsers = true,
        SupportsGroups = true,
        SupportsPatch = false,
        SupportsBulk = false
    };

    /// <summary>
    /// Creates capabilities for a full-featured adapter.
    /// </summary>
    public static AdapterCapabilities Full() => new()
    {
        SupportsUsers = true,
        SupportsGroups = true,
        SupportsFiltering = true,
        SupportsSorting = true,
        SupportsPagination = true,
        SupportsPatch = true,
        SupportsBulk = true,
        SupportsEnterpriseExtension = true,
        SupportsRoles = true,
        SupportsOrgHierarchy = true,
        SupportsCustomAttributes = true,
        SupportsDeltaQuery = true,
        MaxPageSize = 1000
    };
}
