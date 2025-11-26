// ==========================================================================
// T070: AdapterHealthStatus Type
// ==========================================================================
// Health status model for adapters per contracts/adapter-interface.md
// ==========================================================================

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Health status of an adapter.
/// </summary>
public class AdapterHealthStatus
{
    /// <summary>
    /// Overall health status level.
    /// </summary>
    public HealthStatusLevel Status { get; set; } = HealthStatusLevel.Unknown;

    /// <summary>
    /// Last time health was checked.
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the adapter can connect to the provider.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Whether authentication credentials are valid.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Current rate limit status.
    /// </summary>
    public RateLimitStatus? RateLimitStatus { get; set; }

    /// <summary>
    /// Average response time in milliseconds (p95 latency).
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// Error rate in the last 5 minutes (0.0 to 1.0).
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Additional health check details or messages.
    /// </summary>
    public List<string> Details { get; set; } = [];

    /// <summary>
    /// Creates a healthy status.
    /// </summary>
    public static AdapterHealthStatus Healthy(double responseTimeMs = 0) => new()
    {
        Status = HealthStatusLevel.Healthy,
        LastChecked = DateTime.UtcNow,
        IsConnected = true,
        IsAuthenticated = true,
        ResponseTimeMs = responseTimeMs,
        ErrorRate = 0
    };

    /// <summary>
    /// Creates a degraded status.
    /// </summary>
    public static AdapterHealthStatus Degraded(string reason) => new()
    {
        Status = HealthStatusLevel.Degraded,
        LastChecked = DateTime.UtcNow,
        IsConnected = true,
        IsAuthenticated = true,
        Details = [reason]
    };

    /// <summary>
    /// Creates an unhealthy status.
    /// </summary>
    public static AdapterHealthStatus Unhealthy(string reason) => new()
    {
        Status = HealthStatusLevel.Unhealthy,
        LastChecked = DateTime.UtcNow,
        IsConnected = false,
        IsAuthenticated = false,
        Details = [reason]
    };

    /// <summary>
    /// Creates an unknown status.
    /// </summary>
    public static AdapterHealthStatus Unknown() => new()
    {
        Status = HealthStatusLevel.Unknown,
        LastChecked = DateTime.UtcNow
    };
}

/// <summary>
/// Overall health status level.
/// </summary>
public enum HealthStatusLevel
{
    /// <summary>
    /// Adapter is fully operational.
    /// </summary>
    Healthy,

    /// <summary>
    /// Adapter is operational but experiencing issues.
    /// </summary>
    Degraded,

    /// <summary>
    /// Adapter is not operational.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Health status is unknown (not yet checked).
    /// </summary>
    Unknown
}

/// <summary>
/// Rate limit status from the provider.
/// </summary>
public class RateLimitStatus
{
    /// <summary>
    /// Remaining requests in current window.
    /// </summary>
    public int Remaining { get; set; }

    /// <summary>
    /// Maximum requests allowed in window.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// When the rate limit resets.
    /// </summary>
    public DateTime ResetsAt { get; set; }

    /// <summary>
    /// Percentage of rate limit used (0.0 to 1.0).
    /// </summary>
    public double UsagePercent => Limit > 0 ? 1.0 - ((double)Remaining / Limit) : 0;

    /// <summary>
    /// Whether the rate limit is nearly exhausted (>80% used).
    /// </summary>
    public bool IsNearLimit => UsagePercent > 0.8;

    /// <summary>
    /// Whether the rate limit is exhausted.
    /// </summary>
    public bool IsExhausted => Remaining <= 0;
}
