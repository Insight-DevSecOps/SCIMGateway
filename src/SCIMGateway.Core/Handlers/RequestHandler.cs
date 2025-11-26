// ==========================================================================
// T030: RequestHandler - SCIM Request Processing
// T077: Adapter routing logic
// ==========================================================================
// Handles SCIM request parsing, routing, and tenant extraction
// ==========================================================================

using SCIMGateway.Core.Adapters;
using SCIMGateway.Core.Validation;

namespace SCIMGateway.Core.Handlers;

/// <summary>
/// Interface for SCIM request handling.
/// </summary>
public interface IRequestHandler
{
    /// <summary>
    /// Parses an incoming HTTP request into a SCIM request.
    /// </summary>
    Task<ScimRequest> ParseRequestAsync(HttpRequestContext httpContext);

    /// <summary>
    /// Routes and handles a SCIM request.
    /// </summary>
    Task<ScimResponse> RouteRequestAsync(ScimRequest request);

    /// <summary>
    /// Handles a SCIM request (alias for RouteRequestAsync).
    /// </summary>
    Task<ScimResponse> HandleRequestAsync(ScimRequest request);

    /// <summary>
    /// Validates a SCIM request.
    /// </summary>
    Task<ValidationResult> ValidateRequestAsync(ScimRequest request);

    /// <summary>
    /// Validates a SCIM request (synchronous).
    /// </summary>
    ValidationResult ValidateRequest(ScimRequest request);
}

/// <summary>
/// HTTP request context for parsing.
/// </summary>
public class HttpRequestContext
{
    /// <summary>
    /// HTTP method.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Request path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Query string parameters.
    /// </summary>
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    /// <summary>
    /// Request headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = [];

    /// <summary>
    /// Request body content.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIp { get; set; }
}

/// <summary>
/// Parsed SCIM request model.
/// </summary>
public class ScimRequest
{
    /// <summary>
    /// HTTP method (GET, POST, PUT, PATCH, DELETE).
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Alias for HttpMethod.
    /// </summary>
    public string Method => HttpMethod;

    /// <summary>
    /// SCIM resource type.
    /// </summary>
    public ScimResourceType ResourceType { get; set; }

    /// <summary>
    /// Resource identifier for single-resource operations.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// SCIM filter expression.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Requested attributes for response.
    /// </summary>
    public List<string>? Attributes { get; set; }

    /// <summary>
    /// Attributes to exclude from response.
    /// </summary>
    public List<string>? ExcludedAttributes { get; set; }

    /// <summary>
    /// Pagination start index (1-based per RFC 7644).
    /// </summary>
    public int StartIndex { get; set; } = 1;

    /// <summary>
    /// Page size for queries.
    /// </summary>
    public int Count { get; set; } = 100;

    /// <summary>
    /// Request body content.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Alias for Body.
    /// </summary>
    public string? Content => Body;

    /// <summary>
    /// Sort by attribute.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order (ascending/descending).
    /// </summary>
    public string? SortOrder { get; set; }

    /// <summary>
    /// Correlation ID for request tracing.
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Request timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// Actor (authenticated user) ID.
    /// </summary>
    public string? ActorId { get; set; }

    /// <summary>
    /// Request headers.
    /// T077: Added for adapter routing via headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Provider ID for adapter routing.
    /// T077: Optional provider ID for direct routing.
    /// </summary>
    public string? ProviderId { get; set; }
}

/// <summary>
/// SCIM response model.
/// </summary>
public class ScimResponse
{
    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Response headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = [];

    /// <summary>
    /// Location header for created resources.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// ETag header for versioning.
    /// </summary>
    public string? ETag { get; set; }
}

/// <summary>
/// SCIM resource types.
/// </summary>
public enum ScimResourceType
{
    /// <summary>Unknown resource type.</summary>
    Unknown = 0,

    /// <summary>Users resource (/Users).</summary>
    Users,

    /// <summary>Groups resource (/Groups).</summary>
    Groups,

    /// <summary>ServiceProviderConfig resource.</summary>
    ServiceProviderConfig,

    /// <summary>ResourceTypes resource.</summary>
    ResourceTypes,

    /// <summary>Schemas resource.</summary>
    Schemas,

    /// <summary>Bulk endpoint.</summary>
    Bulk,

    /// <summary>Me endpoint (current user).</summary>
    Me
}

/// <summary>
/// SCIM request handler implementation.
/// </summary>
public class RequestHandler : IRequestHandler
{
    private readonly ISchemaValidator _schemaValidator;
    private readonly IAdapterRegistry? _adapterRegistry;

    /// <summary>
    /// Initializes a new instance of RequestHandler.
    /// </summary>
    public RequestHandler(ISchemaValidator schemaValidator)
    {
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
    }

    /// <summary>
    /// Initializes a new instance of RequestHandler with adapter registry support.
    /// T077: Adapter routing support.
    /// </summary>
    /// <param name="schemaValidator">The schema validator.</param>
    /// <param name="adapterRegistry">The adapter registry for routing requests to adapters.</param>
    public RequestHandler(ISchemaValidator schemaValidator, IAdapterRegistry adapterRegistry)
        : this(schemaValidator)
    {
        _adapterRegistry = adapterRegistry ?? throw new ArgumentNullException(nameof(adapterRegistry));
    }

    /// <summary>
    /// Gets the adapter for the given provider ID.
    /// T077: Adapter routing.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The adapter, or null if not found.</returns>
    public IAdapter? GetAdapterForProvider(string providerId)
    {
        if (_adapterRegistry == null) return null;
        return _adapterRegistry.TryGetAdapter(providerId, out var adapter) ? adapter : null;
    }

    /// <summary>
    /// Gets the adapter for the given tenant.
    /// T077: Tenant-based adapter routing.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="providerId">The provider ID (optional, uses default if not specified).</param>
    /// <returns>The adapter, or null if not found.</returns>
    public IAdapter? GetAdapterForTenant(string tenantId, string? providerId = null)
    {
        if (_adapterRegistry == null) return null;
        try
        {
            // If no providerId specified, try to use a default
            var effectiveProviderId = providerId ?? "default";
            return _adapterRegistry.GetAdapterForTenant(tenantId, effectiveProviderId);
        }
        catch (AdapterNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the provider ID from a SCIM request.
    /// T077: Provider extraction from request context.
    /// </summary>
    /// <param name="request">The SCIM request.</param>
    /// <returns>The provider ID, or null if not found.</returns>
    public string? ExtractProviderId(ScimRequest request)
    {
        // Try to extract from request headers or path
        // Provider can be in X-Provider-Id header or part of the path
        return request.Headers?.GetValueOrDefault("X-Provider-Id");
    }

    /// <inheritdoc />
    public Task<ScimRequest> ParseRequestAsync(HttpRequestContext httpContext)
    {
        var request = new ScimRequest
        {
            HttpMethod = httpContext.Method.ToUpperInvariant(),
            Body = httpContext.Body,
            ClientIp = httpContext.ClientIp,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = httpContext.Headers.GetValueOrDefault("X-Request-Id") ?? Guid.NewGuid().ToString()
        };

        // Parse path to extract resource type, ID, and tenant
        ParsePath(httpContext.Path, request);

        // Parse query parameters
        ParseQueryParameters(httpContext.QueryParameters, request);

        // Extract tenant from various sources
        ExtractTenant(httpContext, request);

        return Task.FromResult(request);
    }

    /// <inheritdoc />
    public async Task<ScimResponse> RouteRequestAsync(ScimRequest request)
    {
        // Validate the request first
        var validationResult = await ValidateRequestAsync(request);
        if (!validationResult.IsValid)
        {
            return CreateErrorResponse(400, "invalidSyntax", 
                string.Join("; ", validationResult.Errors.Select(e => e.Message)));
        }

        // Route based on resource type and method
        return request.ResourceType switch
        {
            ScimResourceType.Users => await HandleUsersRequestAsync(request),
            ScimResourceType.Groups => await HandleGroupsRequestAsync(request),
            ScimResourceType.ServiceProviderConfig => await HandleServiceProviderConfigAsync(request),
            ScimResourceType.ResourceTypes => await HandleResourceTypesAsync(request),
            ScimResourceType.Schemas => await HandleSchemasAsync(request),
            ScimResourceType.Bulk => await HandleBulkAsync(request),
            _ => CreateErrorResponse(404, "invalidPath", "Resource type not found")
        };
    }

    /// <inheritdoc />
    public Task<ScimResponse> HandleRequestAsync(ScimRequest request)
    {
        return RouteRequestAsync(request);
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateRequestAsync(ScimRequest request)
    {
        return Task.FromResult(ValidateRequest(request));
    }

    /// <inheritdoc />
    public ValidationResult ValidateRequest(ScimRequest request)
    {
        var errors = new List<ValidationError>();

        // Validate tenant ID
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            errors.Add(new ValidationError
            {
                Path = "tenantId",
                Message = "Tenant ID is required",
                ErrorType = ValidationErrorType.RequiredAttributeMissing
            });
        }

        // Validate HTTP method
        var validMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };
        if (!validMethods.Contains(request.HttpMethod))
        {
            errors.Add(new ValidationError
            {
                Path = "method",
                Message = $"Invalid HTTP method: {request.HttpMethod}",
                ErrorType = ValidationErrorType.InvalidValue
            });
        }

        // Validate resource type
        if (request.ResourceType == ScimResourceType.Unknown)
        {
            errors.Add(new ValidationError
            {
                Path = "resourceType",
                Message = "Unknown resource type",
                ErrorType = ValidationErrorType.InvalidValue
            });
        }

        // Validate filter if present
        if (!string.IsNullOrEmpty(request.Filter))
        {
            var filterResult = _schemaValidator.ValidateFilter(request.Filter);
            if (!filterResult.IsValid)
            {
                errors.AddRange(filterResult.Errors);
            }
        }

        // Validate pagination
        if (request.StartIndex < 1)
        {
            errors.Add(new ValidationError
            {
                Path = "startIndex",
                Message = "startIndex must be >= 1",
                ErrorType = ValidationErrorType.InvalidValue
            });
        }

        if (request.Count < 1 || request.Count > 1000)
        {
            errors.Add(new ValidationError
            {
                Path = "count",
                Message = "count must be between 1 and 1000",
                ErrorType = ValidationErrorType.InvalidValue
            });
        }

        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }

    private static void ParsePath(string path, ScimRequest request)
    {
        // Expected formats:
        // /scim/v2/Users
        // /scim/v2/Users/{id}
        // /scim/{tenantId}/Users
        // /scim/{tenantId}/Users/{id}
        // /{tenantId}/Users
        // /{tenantId}/Users/{id}

        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return;

        int resourceIndex = 0;

        // Skip "scim" prefix if present
        if (segments[0].Equals("scim", StringComparison.OrdinalIgnoreCase))
        {
            resourceIndex++;
            if (resourceIndex < segments.Length && segments[resourceIndex].StartsWith("v"))
            {
                // Skip version (v2)
                resourceIndex++;
            }
        }

        // Check if next segment is a tenant ID (not a known resource type)
        if (resourceIndex < segments.Length && !IsKnownResourceType(segments[resourceIndex]))
        {
            request.TenantId = segments[resourceIndex];
            resourceIndex++;
        }

        // Parse resource type
        if (resourceIndex < segments.Length)
        {
            request.ResourceType = ParseResourceType(segments[resourceIndex]);
            resourceIndex++;
        }

        // Parse resource ID
        if (resourceIndex < segments.Length)
        {
            request.ResourceId = segments[resourceIndex];
        }
    }

    private static bool IsKnownResourceType(string segment)
    {
        return segment.ToLowerInvariant() switch
        {
            "users" or "groups" or "serviceproviderconfig" or 
            "resourcetypes" or "schemas" or "bulk" or "me" => true,
            _ => false
        };
    }

    private static ScimResourceType ParseResourceType(string resourceType)
    {
        return resourceType.ToLowerInvariant() switch
        {
            "users" => ScimResourceType.Users,
            "groups" => ScimResourceType.Groups,
            "serviceproviderconfig" => ScimResourceType.ServiceProviderConfig,
            "resourcetypes" => ScimResourceType.ResourceTypes,
            "schemas" => ScimResourceType.Schemas,
            "bulk" => ScimResourceType.Bulk,
            "me" => ScimResourceType.Me,
            _ => ScimResourceType.Unknown
        };
    }

    private static void ParseQueryParameters(Dictionary<string, string> queryParams, ScimRequest request)
    {
        if (queryParams.TryGetValue("filter", out var filter))
            request.Filter = filter;

        if (queryParams.TryGetValue("attributes", out var attributes))
            request.Attributes = attributes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (queryParams.TryGetValue("excludedAttributes", out var excludedAttributes))
            request.ExcludedAttributes = excludedAttributes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (queryParams.TryGetValue("startIndex", out var startIndex) && int.TryParse(startIndex, out var si))
            request.StartIndex = si;

        if (queryParams.TryGetValue("count", out var count) && int.TryParse(count, out var c))
            request.Count = c;

        if (queryParams.TryGetValue("sortBy", out var sortBy))
            request.SortBy = sortBy;

        if (queryParams.TryGetValue("sortOrder", out var sortOrder))
            request.SortOrder = sortOrder;
    }

    private static void ExtractTenant(HttpRequestContext httpContext, ScimRequest request)
    {
        // If already extracted from path, skip
        if (!string.IsNullOrWhiteSpace(request.TenantId)) return;

        // Try X-Tenant-Id header
        if (httpContext.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
        {
            request.TenantId = tenantHeader;
            return;
        }

        // Try Authorization header (JWT claims would be parsed elsewhere)
        // For now, we just note that tenant could come from token
    }

    private Task<ScimResponse> HandleUsersRequestAsync(ScimRequest request)
    {
        // Placeholder - actual implementation would call user service
        return Task.FromResult(new ScimResponse { StatusCode = 200 });
    }

    private Task<ScimResponse> HandleGroupsRequestAsync(ScimRequest request)
    {
        // Placeholder - actual implementation would call group service
        return Task.FromResult(new ScimResponse { StatusCode = 200 });
    }

    private Task<ScimResponse> HandleServiceProviderConfigAsync(ScimRequest request)
    {
        return Task.FromResult(new ScimResponse { StatusCode = 200 });
    }

    private Task<ScimResponse> HandleResourceTypesAsync(ScimRequest request)
    {
        return Task.FromResult(new ScimResponse { StatusCode = 200 });
    }

    private Task<ScimResponse> HandleSchemasAsync(ScimRequest request)
    {
        return Task.FromResult(new ScimResponse { StatusCode = 200 });
    }

    private Task<ScimResponse> HandleBulkAsync(ScimRequest request)
    {
        return Task.FromResult(new ScimResponse { StatusCode = 200 });
    }

    private static ScimResponse CreateErrorResponse(int statusCode, string scimType, string detail)
    {
        var errorBody = $$"""
        {
            "schemas": ["urn:ietf:params:scim:api:messages:2.0:Error"],
            "status": "{{statusCode}}",
            "scimType": "{{scimType}}",
            "detail": "{{detail}}"
        }
        """;

        return new ScimResponse
        {
            StatusCode = statusCode,
            Body = errorBody,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/scim+json"
            }
        };
    }
}
