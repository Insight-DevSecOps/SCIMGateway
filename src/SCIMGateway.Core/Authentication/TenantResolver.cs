// ==========================================================================
// T014: TenantResolver - Tenant Extraction and Isolation
// ==========================================================================
// Extracts tenant ID from token and enforces tenant isolation
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace SCIMGateway.Core.Authentication;

/// <summary>
/// Interface for tenant resolution and isolation.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves the tenant ID from the current request context.
    /// </summary>
    /// <param name="claims">Token claims.</param>
    /// <returns>Resolved tenant context.</returns>
    TenantContext ResolveTenant(TokenClaims claims);

    /// <summary>
    /// Validates that the requested resource belongs to the current tenant.
    /// </summary>
    /// <param name="tenantContext">Current tenant context.</param>
    /// <param name="resourceTenantId">Tenant ID of the resource.</param>
    /// <returns>True if access is allowed.</returns>
    bool ValidateTenantAccess(TenantContext tenantContext, string resourceTenantId);

    /// <summary>
    /// Gets the tenant filter for database queries.
    /// </summary>
    /// <param name="tenantContext">Current tenant context.</param>
    /// <returns>Tenant filter expression.</returns>
    string GetTenantFilter(TenantContext tenantContext);
}

/// <summary>
/// Resolved tenant context.
/// </summary>
public class TenantContext
{
    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Actor identifier (user or service principal).
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the actor is a service principal.
    /// </summary>
    public bool IsServicePrincipal { get; set; }

    /// <summary>
    /// Application ID (for service principals).
    /// </summary>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// User principal name (for users).
    /// </summary>
    public string? UserPrincipalName { get; set; }

    /// <summary>
    /// Display name of the actor.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Scopes available to the actor.
    /// </summary>
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Roles assigned to the actor.
    /// </summary>
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// Token expiration time.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Correlation ID for request tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Request ID for this specific request.
    /// </summary>
    public string? RequestId { get; set; }
}

/// <summary>
/// Tenant resolver implementation.
/// </summary>
public class TenantResolver : ITenantResolver
{
    private readonly ILogger<TenantResolver> _logger;

    public TenantResolver(ILogger<TenantResolver> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public TenantContext ResolveTenant(TokenClaims claims)
    {
        ArgumentNullException.ThrowIfNull(claims);

        if (string.IsNullOrEmpty(claims.TenantId))
        {
            _logger.LogError("Cannot resolve tenant: tid claim is missing");
            throw new TenantResolutionException("Token is missing required tenant identifier (tid) claim");
        }

        if (string.IsNullOrEmpty(claims.ObjectId))
        {
            _logger.LogError("Cannot resolve tenant: oid claim is missing");
            throw new TenantResolutionException("Token is missing required object identifier (oid) claim");
        }

        var isServicePrincipal = !string.IsNullOrEmpty(claims.ApplicationId) && 
                                  claims.ObjectId == claims.ApplicationId;

        var context = new TenantContext
        {
            TenantId = claims.TenantId,
            ActorId = claims.ObjectId,
            IsServicePrincipal = isServicePrincipal,
            ApplicationId = claims.ApplicationId,
            UserPrincipalName = claims.UserPrincipalName,
            DisplayName = claims.DisplayName,
            Scopes = claims.Scopes,
            Roles = claims.Roles,
            ExpiresAt = claims.ExpiresAt,
            RequestId = Guid.NewGuid().ToString()
        };

        _logger.LogDebug("Resolved tenant context: TenantId={TenantId}, ActorId={ActorId}, IsServicePrincipal={IsServicePrincipal}",
            context.TenantId, context.ActorId, context.IsServicePrincipal);

        return context;
    }

    /// <inheritdoc />
    public bool ValidateTenantAccess(TenantContext tenantContext, string resourceTenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);
        ArgumentException.ThrowIfNullOrEmpty(resourceTenantId);

        var hasAccess = string.Equals(tenantContext.TenantId, resourceTenantId, StringComparison.OrdinalIgnoreCase);

        if (!hasAccess)
        {
            _logger.LogWarning(
                "Cross-tenant access denied: Actor {ActorId} from tenant {ActorTenantId} attempted to access resource in tenant {ResourceTenantId}",
                tenantContext.ActorId, tenantContext.TenantId, resourceTenantId);
        }

        return hasAccess;
    }

    /// <inheritdoc />
    public string GetTenantFilter(TenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);
        
        // Return a filter expression for Cosmos DB queries
        return $"c.tenantId = '{tenantContext.TenantId}'";
    }
}

/// <summary>
/// Exception thrown when tenant resolution fails.
/// </summary>
public class TenantResolutionException : Exception
{
    public TenantResolutionException(string message) : base(message)
    {
    }

    public TenantResolutionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when cross-tenant access is attempted.
/// </summary>
public class CrossTenantAccessException : Exception
{
    public string ActorTenantId { get; }
    public string ResourceTenantId { get; }

    public CrossTenantAccessException(string actorTenantId, string resourceTenantId)
        : base($"Cross-tenant access denied: actor tenant '{actorTenantId}' cannot access resource in tenant '{resourceTenantId}'")
    {
        ActorTenantId = actorTenantId;
        ResourceTenantId = resourceTenantId;
    }
}
