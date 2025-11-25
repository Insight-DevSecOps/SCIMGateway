// ==========================================================================
// T032: AuthenticationMiddleware - Authentication and Authorization Middleware
// ==========================================================================
// Handles bearer token validation, tenant isolation, and rate limiting
// ==========================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SCIMGateway.Core.Authentication;
using SCIMGateway.Core.Models;
using System.Security.Claims;

namespace SCIMGateway.Api.Middleware;

/// <summary>
/// Authentication middleware for SCIM API.
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBearerTokenValidator _tokenValidator;
    private readonly ITenantResolver _tenantResolver;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly AuthenticationOptions _options;

    /// <summary>
    /// Initializes a new instance of AuthenticationMiddleware.
    /// </summary>
    public AuthenticationMiddleware(
        RequestDelegate next,
        IBearerTokenValidator tokenValidator,
        ITenantResolver tenantResolver,
        IRateLimiter rateLimiter,
        IOptions<AuthenticationOptions> options,
        ILogger<AuthenticationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        _tenantResolver = tenantResolver ?? throw new ArgumentNullException(nameof(tenantResolver));
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
        _options = options?.Value ?? new AuthenticationOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes the HTTP request through the middleware pipeline.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if path should skip authentication
        if (ShouldSkipAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Set correlation ID
        var correlationId = context.Request.Headers["X-Request-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Request-Id"] = correlationId;

        // Extract and validate bearer token
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Missing or invalid Authorization header. CorrelationId: {CorrelationId}", correlationId);
            await WriteScimErrorResponse(context, 401, null, "Missing or invalid Authorization header");
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var validationResult = await _tokenValidator.ValidateTokenAsync(token);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Token validation failed: {Error}. CorrelationId: {CorrelationId}", 
                validationResult.ErrorMessage, correlationId);
            
            // Check if token is expired by looking at error code
            if (validationResult.ErrorCode == "token_expired")
            {
                await WriteScimErrorResponse(context, 401, "invalidToken", "Token has expired");
            }
            else
            {
                await WriteScimErrorResponse(context, 401, "invalidToken", validationResult.ErrorMessage ?? "Invalid token");
            }
            return;
        }

        // Get claims from validation result
        var tokenClaims = validationResult.Claims;
        if (tokenClaims == null)
        {
            _logger.LogWarning("Token claims are null. CorrelationId: {CorrelationId}", correlationId);
            await WriteScimErrorResponse(context, 401, "invalidToken", "Unable to extract token claims");
            return;
        }

        // Set user claims
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(tokenClaims.Subject))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, tokenClaims.Subject));
        if (!string.IsNullOrEmpty(tokenClaims.TenantId))
            claims.Add(new Claim("tenant_id", tokenClaims.TenantId));
        if (!string.IsNullOrEmpty(tokenClaims.ObjectId))
            claims.Add(new Claim("oid", tokenClaims.ObjectId));
        
        foreach (var scope in tokenClaims.Scopes)
            claims.Add(new Claim("scope", scope));

        var identity = new ClaimsIdentity(claims, "Bearer");
        context.User = new ClaimsPrincipal(identity);

        // Resolve tenant using synchronous method
        TenantContext tenantContext;
        try
        {
            tenantContext = _tenantResolver.ResolveTenant(tokenClaims);
        }
        catch (TenantResolutionException ex)
        {
            _logger.LogWarning(ex, "Unable to resolve tenant. CorrelationId: {CorrelationId}", correlationId);
            await WriteScimErrorResponse(context, 400, "invalidTenant", ex.Message);
            return;
        }

        // Validate tenant isolation - ensure request tenant matches token tenant
        var requestTenantId = ExtractTenantFromPath(context.Request.Path);
        if (!string.IsNullOrEmpty(requestTenantId) && 
            !string.IsNullOrEmpty(tenantContext.TenantId) &&
            !requestTenantId.Equals(tenantContext.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Cross-tenant access denied. Token tenant: {TokenTenant}, Request tenant: {RequestTenant}. CorrelationId: {CorrelationId}",
                tenantContext.TenantId, requestTenantId, correlationId);
            await WriteScimErrorResponse(context, 403, "forbidden", "Cross-tenant access is not allowed");
            return;
        }

        // Store tenant context
        context.Items["TenantContext"] = tenantContext;

        // Check rate limit using synchronous method
        var rateLimitResult = _rateLimiter.CheckRateLimit(tenantContext.TenantId, tenantContext.ActorId);
        if (!rateLimitResult.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for tenant {TenantId}. CorrelationId: {CorrelationId}",
                tenantContext.TenantId, correlationId);
            
            context.Response.Headers["Retry-After"] = rateLimitResult.RetryAfterSeconds.ToString();
            await WriteScimErrorResponse(context, 429, "tooManyRequests", 
                $"Rate limit exceeded. Retry after {rateLimitResult.RetryAfterSeconds} seconds");
            return;
        }

        // Create request context
        var requestContext = new RequestContext
        {
            CorrelationId = correlationId,
            TenantContext = tenantContext,
            Tenant = tenantContext,
            RequestedAt = DateTimeOffset.UtcNow,
            Timestamp = DateTimeOffset.UtcNow,
            ActorId = tokenClaims.ObjectId ?? tokenClaims.Subject
        };
        context.Items["RequestContext"] = requestContext;

        _logger.LogInformation("Request authenticated. Tenant: {TenantId}, Actor: {ActorId}, CorrelationId: {CorrelationId}",
            tenantContext.TenantId, tenantContext.ActorId, correlationId);

        await _next(context);
    }

    /// <summary>
    /// Alias for InvokeAsync.
    /// </summary>
    public Task Invoke(HttpContext context) => InvokeAsync(context);

    private bool ShouldSkipAuthentication(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        // Check excluded paths
        if (_options.ExcludedPaths != null)
        {
            foreach (var excludedPath in _options.ExcludedPaths)
            {
                if (pathValue.StartsWith(excludedPath.ToLowerInvariant()))
                    return true;
            }
        }

        // Check anonymous paths
        if (_options.AnonymousPaths != null)
        {
            foreach (var anonPath in _options.AnonymousPaths)
            {
                if (pathValue.StartsWith(anonPath.ToLowerInvariant()))
                    return true;
            }
        }

        // Default skip paths
        if (pathValue.Contains("/health") ||
            pathValue.Contains("/serviceproviderconfig"))
        {
            return true;
        }

        return false;
    }

    private static string? ExtractTenantFromPath(PathString path)
    {
        var segments = path.Value?.Trim('/').Split('/') ?? [];
        
        // Format: /scim/{tenantId}/... or /{tenantId}/...
        if (segments.Length >= 2)
        {
            var index = segments[0].Equals("scim", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            if (index < segments.Length && !IsResourceType(segments[index]))
            {
                return segments[index];
            }
        }
        
        return null;
    }

    private static bool IsResourceType(string segment)
    {
        return segment.ToLowerInvariant() switch
        {
            "users" or "groups" or "serviceproviderconfig" or 
            "resourcetypes" or "schemas" or "bulk" or "me" or "v2" => true,
            _ => false
        };
    }

    private static async Task WriteScimErrorResponse(HttpContext context, int statusCode, string? scimType, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/scim+json";

        var scimTypeJson = scimType != null ? $"\"scimType\": \"{scimType}\"," : "";

        var errorBody = $@"{{
    ""schemas"": [""urn:ietf:params:scim:api:messages:2.0:Error""],
    ""status"": ""{statusCode}"",
    {scimTypeJson}
    ""detail"": ""{detail}""
}}";

        await context.Response.WriteAsync(errorBody);
    }
}

/// <summary>
/// Authentication options.
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// Paths that skip authentication.
    /// </summary>
    public List<string>? ExcludedPaths { get; set; }

    /// <summary>
    /// Alias for ExcludedPaths.
    /// </summary>
    public List<string>? AnonymousPaths { get; set; }

    /// <summary>
    /// Issuer for token validation.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Audience for token validation.
    /// </summary>
    public string? Audience { get; set; }
}

/// <summary>
/// Request context for the current request.
/// </summary>
public class RequestContext
{
    /// <summary>
    /// Unique request correlation ID.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Tenant context.
    /// </summary>
    public TenantContext? TenantContext { get; set; }

    /// <summary>
    /// Alias for TenantContext.
    /// </summary>
    public TenantContext? Tenant { get; set; }

    /// <summary>
    /// When the request was received.
    /// </summary>
    public DateTimeOffset RequestedAt { get; set; }

    /// <summary>
    /// Alias for RequestedAt.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Actor (user) who made the request.
    /// </summary>
    public string? ActorId { get; set; }
}

/// <summary>
/// SCIM error response model.
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
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// SCIM error type.
    /// </summary>
    public string? ScimType { get; set; }

    /// <summary>
    /// Human-readable error detail.
    /// </summary>
    public string Detail { get; set; } = string.Empty;
}
