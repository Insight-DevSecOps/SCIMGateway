// ==========================================================================
// T011: AdapterConfiguration Model
// ==========================================================================
// Configuration model for SaaS provider adapters
// Contains: credentials, endpoints, transformation rules, rate limits,
// timeouts, and retry policy
// ==========================================================================

using System.Net;

namespace SCIMGateway.Core.Configuration;

/// <summary>
/// Configuration for a SaaS provider adapter.
/// Contains all settings needed to connect to and interact with a provider.
/// </summary>
public class AdapterConfiguration
{
    /// <summary>
    /// Unique identifier for the provider (e.g., "salesforce", "workday", "servicenow").
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Tenant identifier for multi-tenant isolation.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this adapter configuration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Authentication credentials for the provider.
    /// </summary>
    public AdapterCredentials Credentials { get; set; } = new();

    /// <summary>
    /// API endpoint configuration.
    /// </summary>
    public AdapterEndpoints Endpoints { get; set; } = new();

    /// <summary>
    /// Transformation rules for group/entitlement mapping.
    /// </summary>
    public List<TransformationRuleReference> TransformationRules { get; set; } = [];

    /// <summary>
    /// Rate limiting configuration.
    /// </summary>
    public RateLimitConfiguration RateLimits { get; set; } = new();

    /// <summary>
    /// Timeout configuration for API calls.
    /// </summary>
    public TimeoutConfiguration Timeouts { get; set; } = new();

    /// <summary>
    /// Retry policy for failed requests.
    /// </summary>
    public RetryPolicyConfiguration RetryPolicy { get; set; } = new();
}

/// <summary>
/// Authentication credentials for a provider adapter.
/// </summary>
public class AdapterCredentials
{
    /// <summary>
    /// Type of authentication to use.
    /// </summary>
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.OAuth2;

    /// <summary>
    /// API key for API key authentication.
    /// Retrieved from Key Vault - do not store in configuration.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// OAuth 2.0 Client ID.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// OAuth 2.0 Client Secret.
    /// Retrieved from Key Vault - do not store in configuration.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// OAuth 2.0 Token Endpoint URL.
    /// </summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// OAuth 2.0 scopes to request.
    /// </summary>
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Key Vault secret name for the API key or client secret.
    /// </summary>
    public string? KeyVaultSecretName { get; set; }

    /// <summary>
    /// Username for basic authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for basic authentication.
    /// Retrieved from Key Vault - do not store in configuration.
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Type of authentication used by the adapter.
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// API Key authentication via header or query parameter.
    /// </summary>
    ApiKey,

    /// <summary>
    /// OAuth 2.0 Client Credentials flow.
    /// </summary>
    OAuth2,

    /// <summary>
    /// OAuth 2.0 with JWT Bearer assertion.
    /// </summary>
    OAuth2JwtBearer,

    /// <summary>
    /// HTTP Basic authentication.
    /// </summary>
    Basic,

    /// <summary>
    /// Custom authentication (adapter-specific).
    /// </summary>
    Custom
}

/// <summary>
/// API endpoint configuration for a provider adapter.
/// </summary>
public class AdapterEndpoints
{
    /// <summary>
    /// Base URL for the provider's API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API version to use.
    /// </summary>
    public string ApiVersion { get; set; } = string.Empty;

    /// <summary>
    /// Users endpoint path (relative to base URL).
    /// </summary>
    public string UsersPath { get; set; } = "/users";

    /// <summary>
    /// Groups endpoint path (relative to base URL).
    /// </summary>
    public string GroupsPath { get; set; } = "/groups";

    /// <summary>
    /// Custom endpoint paths for provider-specific operations.
    /// </summary>
    public Dictionary<string, string> CustomPaths { get; set; } = [];
}

/// <summary>
/// Reference to a transformation rule.
/// </summary>
public class TransformationRuleReference
{
    /// <summary>
    /// Unique identifier for the transformation rule.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Priority order (lower = higher priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this rule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Rate limiting configuration for API calls.
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Maximum requests allowed per minute.
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 1000;

    /// <summary>
    /// Maximum requests allowed per second.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; } = 20;

    /// <summary>
    /// Maximum burst size for token bucket algorithm.
    /// </summary>
    public int BurstLimit { get; set; } = 50;

    /// <summary>
    /// Whether to queue requests when rate limited (vs. immediate rejection).
    /// </summary>
    public bool QueueOnLimit { get; set; } = true;

    /// <summary>
    /// Maximum queue wait time before rejecting.
    /// </summary>
    public TimeSpan MaxQueueTime { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Timeout configuration for API calls.
/// </summary>
public class TimeoutConfiguration
{
    /// <summary>
    /// Connection establishment timeout.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Individual request timeout.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Total operation timeout (including retries).
    /// </summary>
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Idle connection timeout before recycling.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Retry policy configuration for failed requests.
/// </summary>
public class RetryPolicyConfiguration
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay before first retry.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Whether to add jitter to retry delays.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// HTTP status codes that should trigger a retry.
    /// </summary>
    public List<HttpStatusCode> RetryableStatusCodes { get; set; } =
    [
        HttpStatusCode.RequestTimeout,           // 408
        HttpStatusCode.TooManyRequests,          // 429
        HttpStatusCode.InternalServerError,      // 500
        HttpStatusCode.BadGateway,               // 502
        HttpStatusCode.ServiceUnavailable,       // 503
        HttpStatusCode.GatewayTimeout            // 504
    ];
}
