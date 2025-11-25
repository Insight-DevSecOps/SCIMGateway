// ==========================================================================
// Context Models - Request and Tenant Context for SCIM operations
// ==========================================================================

using SCIMGateway.Core.Authentication;

namespace SCIMGateway.Core.Models;

/// <summary>
/// Request context for SCIM operations.
/// </summary>
public class RequestContext
{
    /// <summary>
    /// Unique request identifier for correlation.
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Associated tenant context.
    /// </summary>
    public TenantContext? TenantContext { get; set; }

    /// <summary>
    /// Alias for TenantContext.
    /// </summary>
    public TenantContext? Tenant { get => TenantContext; set => TenantContext = value; }

    /// <summary>
    /// Request timestamp.
    /// </summary>
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Alias for RequestedAt.
    /// </summary>
    public DateTimeOffset Timestamp { get => RequestedAt; set => RequestedAt = value; }

    /// <summary>
    /// Actor/user identifier.
    /// </summary>
    public string? ActorId { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIp { get; set; }
}

/// <summary>
/// SCIM error response per RFC 7644.
/// </summary>
public class ScimErrorResponse
{
    /// <summary>
    /// SCIM schema URIs.
    /// </summary>
    public List<string> Schemas { get; set; } = ["urn:ietf:params:scim:api:messages:2.0:Error"];

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// SCIM error type (e.g., "invalidFilter", "tooMany", "uniqueness").
    /// </summary>
    public string? ScimType { get; set; }

    /// <summary>
    /// Human-readable error detail.
    /// </summary>
    public string? Detail { get; set; }
}

/// <summary>
/// Authentication result from token validation.
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Whether authentication succeeded.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Actor/subject identifier.
    /// </summary>
    public string? ActorId { get; set; }

    /// <summary>
    /// Tenant identifier from token.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Token claims.
    /// </summary>
    public Dictionary<string, string>? Claims { get; set; }
}

/// <summary>
/// Options for audit logging.
/// </summary>
public class AuditOptions
{
    /// <summary>
    /// Whether audit logging is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to log read operations.
    /// </summary>
    public bool LogReadOperations { get; set; } = true;

    /// <summary>
    /// Retention period in days.
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Paths to exclude from audit logging.
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = ["/health", "/ready"];
}
