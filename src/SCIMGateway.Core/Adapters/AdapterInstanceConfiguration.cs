// ==========================================================================
// T067: AdapterConfiguration Type
// ==========================================================================
// Configuration model for adapter instances per contracts/adapter-interface.md
// Note: This extends the existing AdapterConfiguration in Configuration folder
// with adapter-specific settings for IAdapter implementations.
// ==========================================================================

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Extended configuration for an adapter instance.
/// Used by IAdapter implementations for provider-specific settings.
/// </summary>
public class AdapterInstanceConfiguration
{
    /// <summary>
    /// Unique identifier for this adapter instance.
    /// Format: "{providerName}-{environment}" (e.g., "salesforce-prod")
    /// </summary>
    public string AdapterId { get; set; } = string.Empty;

    /// <summary>
    /// Provider name (Salesforce, Workday, ServiceNow, etc.)
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Provider API base URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Azure Key Vault path for credentials.
    /// </summary>
    public string CredentialKeyVaultPath { get; set; } = string.Empty;

    /// <summary>
    /// Group mapping strategy (ROLE_ASSIGNMENT, ORG_HIERARCHY, DIRECT_GROUP, etc.)
    /// </summary>
    public string GroupMappingStrategy { get; set; } = "DIRECT_GROUP";

    /// <summary>
    /// Whether this adapter is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Tenant ID this adapter belongs to.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Environment identifier (prod, sandbox, dev).
    /// </summary>
    public string Environment { get; set; } = "prod";

    /// <summary>
    /// Maximum concurrent requests to the provider.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retries for failed requests.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Provider-specific custom settings (JSON object).
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = [];
}
