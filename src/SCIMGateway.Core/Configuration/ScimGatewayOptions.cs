namespace SCIMGateway.Core.Configuration;

/// <summary>
/// Configuration options for the SCIM Gateway SDK.
/// Bound from appsettings.json section "ScimGateway".
/// </summary>
public class ScimGatewayOptions
{
    /// <summary>
    /// Default sync direction for new tenant configurations.
    /// Values: "EntraToSaas" (default), "SaasToEntra"
    /// </summary>
    public string DefaultSyncDirection { get; set; } = "EntraToSaas";

    /// <summary>
    /// Audit log retention in days (minimum 90 per FR-016a).
    /// </summary>
    public int AuditLogRetentionDays { get; set; } = 90;

    /// <summary>
    /// Rate limiting configuration for failed authentication attempts.
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// Azure Key Vault URI for secrets (adapter credentials, signing keys).
    /// </summary>
    public string? KeyVaultUri { get; set; }

    /// <summary>
    /// Azure Cosmos DB configuration.
    /// </summary>
    public CosmosDbOptions CosmosDb { get; set; } = new();
}

/// <summary>
/// Rate limiting configuration options.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Maximum requests per minute per tenant (FR-047: 1000 concurrent).
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 1000;

    /// <summary>
    /// Maximum failed authentication attempts before lockout (FR-010).
    /// </summary>
    public int MaxFailedAuthAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes after exceeding failed auth attempts.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;
}

/// <summary>
/// Azure Cosmos DB configuration options.
/// </summary>
public class CosmosDbOptions
{
    /// <summary>
    /// Cosmos DB connection string (alternative to managed identity).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Cosmos DB account endpoint (for managed identity auth).
    /// </summary>
    public string? AccountEndpoint { get; set; }

    /// <summary>
    /// Alias for AccountEndpoint for backward compatibility.
    /// </summary>
    public string Endpoint
    {
        get => AccountEndpoint ?? string.Empty;
        set => AccountEndpoint = value;
    }

    /// <summary>
    /// Database name for SCIM Gateway data.
    /// </summary>
    public string DatabaseName { get; set; } = "scim-gateway";

    /// <summary>
    /// Whether to use managed identity for authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
}
