// ==========================================================================
// T073: AdapterBase Abstract Class
// ==========================================================================
// Provides common functionality for adapter implementations
// ==========================================================================

using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Configuration;
using SCIMGateway.Core.Errors;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Factory for creating HttpClient instances for adapters.
/// </summary>
public interface IHttpClientFactory
{
    /// <summary>
    /// Creates an HttpClient instance.
    /// </summary>
    /// <param name="name">The logical name of the client to create.</param>
    /// <returns>A configured HttpClient instance.</returns>
    HttpClient CreateClient(string name);
}

/// <summary>
/// Abstract base class for adapter implementations.
/// Provides common helper methods for authentication, error handling, and logging.
/// </summary>
public abstract class AdapterBase : IAdapter
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private AdapterHealthStatus _healthStatus;

    /// <summary>
    /// Gets the unique identifier for this adapter instance.
    /// </summary>
    public string AdapterId { get; }

    /// <summary>
    /// Gets the name of the provider this adapter connects to.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Gets the adapter configuration.
    /// </summary>
    public AdapterConfiguration Configuration { get; }

    /// <summary>
    /// Gets the current health status of the adapter.
    /// </summary>
    public AdapterHealthStatus HealthStatus => _healthStatus;

    /// <summary>
    /// Gets the HttpClient for making API calls.
    /// </summary>
    protected HttpClient HttpClient => _httpClient;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterBase"/> class.
    /// </summary>
    /// <param name="adapterId">The unique adapter identifier.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="configuration">The adapter configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    protected AdapterBase(
        string adapterId,
        string providerName,
        AdapterConfiguration configuration,
        ILogger logger,
        IHttpClientFactory httpClientFactory)
    {
        AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory?.CreateClient(providerName) ?? throw new ArgumentNullException(nameof(httpClientFactory));
        
        _healthStatus = new AdapterHealthStatus
        {
            Status = HealthStatusLevel.Unknown,
            LastChecked = DateTime.UtcNow
        };
    }

    // ==================== User Operations (Abstract) ====================

    /// <inheritdoc />
    public abstract Task<ScimUser> CreateUserAsync(ScimUser user, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<ScimUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<ScimUser> UpdateUserAsync(string userId, ScimUser user, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<PagedResult<ScimUser>> ListUsersAsync(QueryFilter filter, CancellationToken cancellationToken = default);

    // ==================== Group Operations (Abstract) ====================

    /// <inheritdoc />
    public abstract Task<ScimGroup> CreateGroupAsync(ScimGroup group, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<ScimGroup?> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<ScimGroup> UpdateGroupAsync(string groupId, ScimGroup group, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<PagedResult<ScimGroup>> ListGroupsAsync(QueryFilter filter, CancellationToken cancellationToken = default);

    // ==================== Membership Operations (Abstract) ====================

    /// <inheritdoc />
    public abstract Task AddUserToGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task RemoveUserFromGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<IEnumerable<string>> GetGroupMembersAsync(string groupId, CancellationToken cancellationToken = default);

    // ==================== Transformation Operations (Abstract) ====================

    /// <inheritdoc />
    public abstract Task<EntitlementMapping> MapGroupToEntitlementAsync(ScimGroup group, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<ScimGroup> MapEntitlementToGroupAsync(EntitlementMapping entitlement, CancellationToken cancellationToken = default);

    // ==================== Health & Diagnostics ====================

    /// <inheritdoc />
    public virtual async Task<AdapterHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Default implementation: check if we can get an access token
            var token = await GetAccessTokenAsync(cancellationToken);
            stopwatch.Stop();

            _healthStatus = new AdapterHealthStatus
            {
                Status = !string.IsNullOrEmpty(token) ? HealthStatusLevel.Healthy : HealthStatusLevel.Unhealthy,
                LastChecked = startTime,
                IsConnected = true,
                IsAuthenticated = !string.IsNullOrEmpty(token),
                ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Health check failed for adapter {AdapterId}", AdapterId);

            _healthStatus = new AdapterHealthStatus
            {
                Status = HealthStatusLevel.Unhealthy,
                LastChecked = startTime,
                IsConnected = false,
                IsAuthenticated = false,
                ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                Details = [ex.Message]
            };
        }

        return _healthStatus;
    }

    /// <inheritdoc />
    public virtual AdapterCapabilities GetCapabilities()
    {
        return new AdapterCapabilities
        {
            SupportsUsers = true,
            SupportsGroups = true,
            SupportsFiltering = true,
            SupportsSorting = true,
            SupportsPagination = true,
            SupportsPatch = true,
            SupportsBulk = false,
            SupportsEnterpriseExtension = true,
            MaxPageSize = 100,
            SupportedFilterOperators = new List<string> { "eq", "ne", "co", "sw", "ew", "pr" },
            SupportedAuthMethods = new List<string> { "OAuth2", "ApiKey" }
        };
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Gets an access token for API calls.
    /// Override this method to implement provider-specific authentication.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token.</returns>
    protected virtual Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Default implementation returns null - override in derived classes
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Translates a provider-specific error to an AdapterException.
    /// </summary>
    /// <param name="httpStatusCode">The HTTP status code from the provider.</param>
    /// <param name="message">The error message.</param>
    /// <param name="providerErrorCode">Optional provider-specific error code.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <returns>An AdapterException with the appropriate SCIM error type.</returns>
    protected virtual AdapterException TranslateError(
        int httpStatusCode,
        string message,
        string? providerErrorCode = null,
        Exception? innerException = null)
    {
        var scimErrorType = httpStatusCode switch
        {
            400 => ScimErrorType.InvalidSyntax,
            401 => ScimErrorType.Unauthorized,
            403 => ScimErrorType.Forbidden,
            404 => ScimErrorType.ResourceNotFound,
            408 => ScimErrorType.Timeout,
            409 => ScimErrorType.Uniqueness,
            429 => ScimErrorType.RateLimitExceeded,
            503 => ScimErrorType.ServerUnavailable,
            _ => httpStatusCode >= 500 ? ScimErrorType.InternalError : ScimErrorType.Unknown
        };

        var exception = innerException != null
            ? new AdapterException(message, ProviderName, innerException)
            : new AdapterException(message, ProviderName, scimErrorType, httpStatusCode);

        if (innerException == null)
        {
            return new AdapterException(message, ProviderName, scimErrorType, httpStatusCode)
            {
                ProviderErrorCode = providerErrorCode,
                AdapterId = AdapterId,
                IsRetryable = httpStatusCode == 429 || httpStatusCode == 503 || httpStatusCode == 408
            };
        }

        return new AdapterException(message, ProviderName, innerException)
        {
            HttpStatusCode = httpStatusCode,
            ProviderErrorCode = providerErrorCode,
            ScimErrorType = scimErrorType,
            AdapterId = AdapterId,
            IsRetryable = httpStatusCode == 429 || httpStatusCode == 503 || httpStatusCode == 408
        };
    }

    /// <summary>
    /// Logs an adapter operation.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="resourceType">The resource type (User, Group).</param>
    /// <param name="resourceId">The resource ID (optional).</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="duration">The operation duration.</param>
    /// <param name="details">Additional details (optional).</param>
    protected virtual void LogOperation(
        string operation,
        string resourceType,
        string? resourceId,
        bool success,
        TimeSpan duration,
        string? details = null)
    {
        if (success)
        {
            _logger.LogInformation(
                "Adapter operation completed: {Operation} {ResourceType} {ResourceId} in {Duration}ms - Adapter: {AdapterId}, Provider: {ProviderName}",
                operation,
                resourceType,
                resourceId ?? "N/A",
                duration.TotalMilliseconds,
                AdapterId,
                ProviderName);
        }
        else
        {
            _logger.LogWarning(
                "Adapter operation failed: {Operation} {ResourceType} {ResourceId} in {Duration}ms - Adapter: {AdapterId}, Provider: {ProviderName}, Details: {Details}",
                operation,
                resourceType,
                resourceId ?? "N/A",
                duration.TotalMilliseconds,
                AdapterId,
                ProviderName,
                details ?? "No details");
        }
    }

    /// <summary>
    /// Executes an operation with standardized logging and error handling.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation name.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID (optional).</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The operation result.</returns>
    protected async Task<T> ExecuteOperationAsync<T>(
        string operation,
        string resourceType,
        string? resourceId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var success = false;
        string? errorDetails = null;

        try
        {
            var result = await action(cancellationToken);
            success = true;
            return result;
        }
        catch (AdapterException)
        {
            throw; // Re-throw adapter exceptions as-is
        }
        catch (HttpRequestException ex)
        {
            errorDetails = ex.Message;
            throw TranslateError(
                ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : 500,
                ex.Message,
                innerException: ex);
        }
        catch (TaskCanceledException ex)
        {
            errorDetails = "Operation timed out";
            throw TranslateError(408, "Request timed out", innerException: ex);
        }
        catch (Exception ex)
        {
            errorDetails = ex.Message;
            throw TranslateError(500, ex.Message, innerException: ex);
        }
        finally
        {
            stopwatch.Stop();
            LogOperation(operation, resourceType, resourceId, success, stopwatch.Elapsed, errorDetails);
        }
    }

    /// <summary>
    /// Executes a void operation with standardized logging and error handling.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID (optional).</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ExecuteOperationAsync(
        string operation,
        string resourceType,
        string? resourceId,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await ExecuteOperationAsync(
            operation,
            resourceType,
            resourceId,
            async ct =>
            {
                await action(ct);
                return true;
            },
            cancellationToken);
    }
}
