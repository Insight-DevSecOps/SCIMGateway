// ==========================================================================
// T016: AuditLogger - Audit Logging to Application Insights
// ==========================================================================
// Logs all CRUD operations with PII redaction per FR-011
// ==========================================================================

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SCIMGateway.Core.Models;
using SCIMGateway.Core.Utilities;

namespace SCIMGateway.Core.Auditing;

/// <summary>
/// Type of resource being audited.
/// </summary>
public enum AuditResourceType
{
    /// <summary>
    /// User resource.
    /// </summary>
    User,

    /// <summary>
    /// Group resource.
    /// </summary>
    Group,

    /// <summary>
    /// Schema resource.
    /// </summary>
    Schema,

    /// <summary>
    /// Resource type definition.
    /// </summary>
    ResourceType,

    /// <summary>
    /// Service provider configuration.
    /// </summary>
    ServiceProviderConfig,

    /// <summary>
    /// Adapter configuration.
    /// </summary>
    Adapter,

    /// <summary>
    /// Transformation rules.
    /// </summary>
    TransformationRule,

    /// <summary>
    /// Audit log entry.
    /// </summary>
    AuditLog
}

/// <summary>
/// Interface for audit logging.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit event.
    /// </summary>
    /// <param name="entry">The audit log entry.</param>
    Task LogAsync(AuditLogEntry entry);

    /// <summary>
    /// Logs a user operation.
    /// </summary>
    /// <param name="operation">The operation type.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="actorId">The actor ID.</param>
    /// <param name="httpStatus">HTTP status code.</param>
    /// <param name="responseTimeMs">Response time in milliseconds.</param>
    Task LogUserOperationAsync(
        OperationType operation,
        string userId,
        string tenantId,
        string actorId,
        int httpStatus,
        long responseTimeMs);

    /// <summary>
    /// Logs a group operation.
    /// </summary>
    /// <param name="operation">The operation type.</param>
    /// <param name="groupId">The group ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="actorId">The actor ID.</param>
    /// <param name="httpStatus">HTTP status code.</param>
    /// <param name="responseTimeMs">Response time in milliseconds.</param>
    Task LogGroupOperationAsync(
        OperationType operation,
        string groupId,
        string tenantId,
        string actorId,
        int httpStatus,
        long responseTimeMs);

    /// <summary>
    /// Logs an authentication event.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="actorId">The actor ID.</param>
    /// <param name="success">Whether authentication succeeded.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    Task LogAuthenticationAsync(
        string tenantId,
        string actorId,
        bool success,
        string? errorMessage = null);

    /// <summary>
    /// Logs an adapter operation.
    /// </summary>
    /// <param name="adapterId">The adapter ID.</param>
    /// <param name="operation">The operation type.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="actorId">The actor ID.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    Task LogAdapterOperationAsync(
        string adapterId,
        OperationType operation,
        string tenantId,
        string actorId,
        bool success,
        string? errorMessage = null);

    /// <summary>
    /// Logs an adapter error with full context.
    /// T079: Adapter operation logging.
    /// </summary>
    /// <param name="adapterId">The adapter ID.</param>
    /// <param name="operation">The operation type.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="actorId">The actor ID.</param>
    /// <param name="resourceType">The resource type being operated on.</param>
    /// <param name="resourceId">The resource ID being operated on.</param>
    /// <param name="providerErrorCode">The provider-specific error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="httpStatusCode">The HTTP status code from the provider.</param>
    Task LogAdapterErrorAsync(
        string adapterId,
        OperationType operation,
        string tenantId,
        string actorId,
        string? resourceType,
        string? resourceId,
        string? providerErrorCode,
        string errorMessage,
        int? httpStatusCode = null);

    /// <summary>
    /// Logs a CRUD operation.
    /// </summary>
    /// <param name="operationType">Type of operation.</param>
    /// <param name="resourceType">Type of resource.</param>
    /// <param name="resourceId">Resource identifier.</param>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="actorId">Actor identifier.</param>
    /// <param name="httpStatus">HTTP status code.</param>
    /// <param name="responseTimeMs">Response time in milliseconds.</param>
    /// <param name="oldValue">Previous value (for updates).</param>
    /// <param name="newValue">New value (for creates/updates).</param>
    Task LogOperationAsync(
        OperationType operationType,
        string resourceType,
        string resourceId,
        string tenantId,
        string actorId,
        int httpStatus,
        long responseTimeMs,
        object? oldValue = null,
        object? newValue = null);

    /// <summary>
    /// Logs an error.
    /// </summary>
    /// <param name="operationType">Type of operation.</param>
    /// <param name="resourceType">Type of resource.</param>
    /// <param name="resourceId">Resource identifier.</param>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="actorId">Actor identifier.</param>
    /// <param name="exception">The exception that occurred.</param>
    Task LogErrorAsync(
        OperationType operationType,
        string resourceType,
        string? resourceId,
        string tenantId,
        string actorId,
        Exception exception);

    // ==================== Transformation Operations (T105) ====================

    /// <summary>
    /// Logs a transformation rule match.
    /// T105: Transformation operation logging.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="groupDisplayName">SCIM group display name that was transformed.</param>
    /// <param name="matchedRuleIds">IDs of rules that matched.</param>
    /// <param name="entitlementNames">Names of entitlements that resulted.</param>
    /// <param name="actorId">Actor identifier.</param>
    Task LogTransformationMatchAsync(
        string tenantId,
        string providerId,
        string groupDisplayName,
        IEnumerable<string> matchedRuleIds,
        IEnumerable<string> entitlementNames,
        string actorId);

    /// <summary>
    /// Logs a transformation application (entitlement assigned).
    /// T105: Transformation operation logging.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="userId">User who received the entitlement.</param>
    /// <param name="groupDisplayName">SCIM group display name.</param>
    /// <param name="entitlementName">Entitlement that was applied.</param>
    /// <param name="entitlementType">Type of entitlement.</param>
    /// <param name="success">Whether the application succeeded.</param>
    /// <param name="actorId">Actor identifier.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    Task LogTransformationApplicationAsync(
        string tenantId,
        string providerId,
        string userId,
        string groupDisplayName,
        string entitlementName,
        string entitlementType,
        bool success,
        string actorId,
        string? errorMessage = null);

    /// <summary>
    /// Logs a transformation conflict detection.
    /// T105: Transformation operation logging.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="groupDisplayName">SCIM group display name.</param>
    /// <param name="conflictingEntitlements">Names of conflicting entitlements.</param>
    /// <param name="resolutionStrategy">Strategy used to resolve conflict.</param>
    /// <param name="resolvedEntitlement">Entitlement that was selected (if resolved).</param>
    /// <param name="actorId">Actor identifier.</param>
    Task LogTransformationConflictAsync(
        string tenantId,
        string providerId,
        string groupDisplayName,
        IEnumerable<string> conflictingEntitlements,
        string resolutionStrategy,
        string? resolvedEntitlement,
        string actorId);

    /// <summary>
    /// Logs a reverse transformation (entitlement to group mapping).
    /// T105: Transformation operation logging.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="entitlementId">Provider entitlement ID.</param>
    /// <param name="entitlementType">Type of entitlement.</param>
    /// <param name="matchedGroups">SCIM groups that matched.</param>
    /// <param name="actorId">Actor identifier.</param>
    Task LogReverseTransformationAsync(
        string tenantId,
        string providerId,
        string entitlementId,
        string entitlementType,
        IEnumerable<string> matchedGroups,
        string actorId);
}

/// <summary>
/// Interface for audit log storage/repository.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Saves an audit log entry.
    /// </summary>
    /// <param name="entry">The audit log entry to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alias for SaveAsync for backward compatibility.
    /// </summary>
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default) => SaveAsync(entry, cancellationToken);

    /// <summary>
    /// Queries audit log entries.
    /// </summary>
    /// <param name="tenantId">Tenant to query.</param>
    /// <param name="filter">Optional filter expression.</param>
    /// <param name="startTime">Start time for the query range.</param>
    /// <param name="endTime">End time for the query range.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<AuditLogEntry>> QueryAsync(
        string tenantId,
        string? filter = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Alias for QueryAsync for backward compatibility.
    /// </summary>
    Task<IEnumerable<AuditLogEntry>> GetAsync(
        string tenantId,
        string? filter = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null,
        int maxResults = 100,
        CancellationToken cancellationToken = default) => QueryAsync(tenantId, filter, startTime, endTime, maxResults, cancellationToken);
}

/// <summary>
/// Options for audit logging.
/// </summary>
public class AuditLoggerOptions
{
    /// <summary>
    /// Application Insights instrumentation key.
    /// </summary>
    public string? InstrumentationKey { get; set; }

    /// <summary>
    /// Application Insights connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Whether to enable PII redaction.
    /// </summary>
    public bool EnablePiiRedaction { get; set; } = true;

    /// <summary>
    /// Whether to log request/response bodies.
    /// </summary>
    public bool LogRequestBodies { get; set; } = true;

    /// <summary>
    /// Maximum body size to log (in characters).
    /// </summary>
    public int MaxBodySize { get; set; } = 10000;

    /// <summary>
    /// Event names for custom telemetry.
    /// </summary>
    public AuditEventNames EventNames { get; set; } = new();
}

/// <summary>
/// Custom event names for audit telemetry.
/// </summary>
public class AuditEventNames
{
    public string UserCreated { get; set; } = "SCIM_UserCreated";
    public string UserRead { get; set; } = "SCIM_UserRead";
    public string UserUpdated { get; set; } = "SCIM_UserUpdated";
    public string UserDeleted { get; set; } = "SCIM_UserDeleted";
    public string UserListed { get; set; } = "SCIM_UserListed";
    public string GroupCreated { get; set; } = "SCIM_GroupCreated";
    public string GroupRead { get; set; } = "SCIM_GroupRead";
    public string GroupUpdated { get; set; } = "SCIM_GroupUpdated";
    public string GroupDeleted { get; set; } = "SCIM_GroupDeleted";
    public string GroupListed { get; set; } = "SCIM_GroupListed";
    public string MemberAdded { get; set; } = "SCIM_MemberAdded";
    public string MemberRemoved { get; set; } = "SCIM_MemberRemoved";
    public string AuthSuccess { get; set; } = "SCIM_AuthSuccess";
    public string AuthFailure { get; set; } = "SCIM_AuthFailure";
    public string SyncStarted { get; set; } = "SCIM_SyncStarted";
    public string SyncCompleted { get; set; } = "SCIM_SyncCompleted";
    public string SyncFailed { get; set; } = "SCIM_SyncFailed";
    public string DriftDetected { get; set; } = "SCIM_DriftDetected";
    public string ConflictDetected { get; set; } = "SCIM_ConflictDetected";
    public string Error { get; set; } = "SCIM_Error";
    
    // T105: Transformation event names
    public string TransformationMatch { get; set; } = "SCIM_TransformationMatch";
    public string TransformationApplied { get; set; } = "SCIM_TransformationApplied";
    public string TransformationFailed { get; set; } = "SCIM_TransformationFailed";
    public string TransformationConflict { get; set; } = "SCIM_TransformationConflict";
    public string ReverseTransformation { get; set; } = "SCIM_ReverseTransformation";
}

/// <summary>
/// Audit logger implementation using Application Insights.
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<AuditLogger> _logger;
    private readonly AuditLoggerOptions _options;
    private readonly IPiiRedactor _piiRedactor;

    public AuditLogger(
        IOptions<AuditLoggerOptions> options,
        ILogger<AuditLogger> logger,
        IPiiRedactor piiRedactor,
        TelemetryClient? telemetryClient = null)
    {
        _options = options.Value;
        _logger = logger;
        _piiRedactor = piiRedactor;
        _telemetryClient = telemetryClient;
    }

    /// <inheritdoc />
    public Task LogAsync(AuditLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // Log to Application Insights
        if (_telemetryClient != null)
        {
            var eventName = GetEventName(entry.OperationType, entry.ResourceType, entry.HttpStatus >= 400);
            var telemetry = new EventTelemetry(eventName)
            {
                Timestamp = entry.Timestamp
            };

            // Add standard properties
            telemetry.Properties["tenantId"] = entry.TenantId;
            telemetry.Properties["actorId"] = entry.ActorId;
            telemetry.Properties["actorType"] = entry.ActorType.ToString();
            telemetry.Properties["operationType"] = entry.OperationType.ToString();
            telemetry.Properties["resourceType"] = entry.ResourceType;
            telemetry.Properties["resourceId"] = entry.ResourceId;
            telemetry.Properties["httpStatus"] = entry.HttpStatus.ToString();
            
            if (!string.IsNullOrEmpty(entry.RequestId))
                telemetry.Properties["requestId"] = entry.RequestId;
            if (!string.IsNullOrEmpty(entry.CorrelationId))
                telemetry.Properties["correlationId"] = entry.CorrelationId;
            if (!string.IsNullOrEmpty(entry.AdapterId))
                telemetry.Properties["adapterId"] = entry.AdapterId;
            if (!string.IsNullOrEmpty(entry.SyncDirection))
                telemetry.Properties["syncDirection"] = entry.SyncDirection;
            if (!string.IsNullOrEmpty(entry.HttpMethod))
                telemetry.Properties["httpMethod"] = entry.HttpMethod;
            if (!string.IsNullOrEmpty(entry.RequestPath))
                telemetry.Properties["requestPath"] = entry.RequestPath;

            // Add PII-redacted values if configured
            if (_options.LogRequestBodies)
            {
                if (!string.IsNullOrEmpty(entry.OldValue))
                    telemetry.Properties["oldValue"] = RedactAndTruncate(entry.OldValue);
                if (!string.IsNullOrEmpty(entry.NewValue))
                    telemetry.Properties["newValue"] = RedactAndTruncate(entry.NewValue);
            }

            // Add error info if present
            if (!string.IsNullOrEmpty(entry.ErrorCode))
                telemetry.Properties["errorCode"] = entry.ErrorCode;
            if (!string.IsNullOrEmpty(entry.ErrorMessage))
                telemetry.Properties["errorMessage"] = entry.ErrorMessage;
            if (!string.IsNullOrEmpty(entry.ScimErrorType))
                telemetry.Properties["scimErrorType"] = entry.ScimErrorType;

            // Add metrics
            telemetry.Metrics["responseTimeMs"] = entry.ResponseTimeMs;

            _telemetryClient.TrackEvent(telemetry);
        }

        // Also log to ILogger for local debugging
        _logger.LogInformation(
            "Audit: {OperationType} {ResourceType}/{ResourceId} by {ActorId} in tenant {TenantId} - HTTP {HttpStatus} ({ResponseTimeMs}ms)",
            entry.OperationType,
            entry.ResourceType,
            entry.ResourceId,
            entry.ActorId,
            entry.TenantId,
            entry.HttpStatus,
            entry.ResponseTimeMs);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task LogOperationAsync(
        OperationType operationType,
        string resourceType,
        string resourceId,
        string tenantId,
        string actorId,
        int httpStatus,
        long responseTimeMs,
        object? oldValue = null,
        object? newValue = null)
    {
        var entry = new AuditLogEntry
        {
            OperationType = operationType,
            ResourceType = resourceType,
            ResourceId = resourceId,
            TenantId = tenantId,
            ActorId = actorId,
            HttpStatus = httpStatus,
            ResponseTimeMs = responseTimeMs,
            OldValue = oldValue != null ? SerializeValue(oldValue) : null,
            NewValue = newValue != null ? SerializeValue(newValue) : null
        };

        await LogAsync(entry);
    }

    /// <inheritdoc />
    public async Task LogErrorAsync(
        OperationType operationType,
        string resourceType,
        string? resourceId,
        string tenantId,
        string actorId,
        Exception exception)
    {
        var entry = new AuditLogEntry
        {
            OperationType = operationType,
            ResourceType = resourceType,
            ResourceId = resourceId ?? string.Empty,
            TenantId = tenantId,
            ActorId = actorId,
            HttpStatus = 500,
            ErrorCode = exception.GetType().Name,
            ErrorMessage = exception.Message
        };

        if (exception is Errors.ScimException scimEx)
        {
            entry.HttpStatus = (int)scimEx.StatusCode;
            entry.ScimErrorType = scimEx.ScimType?.ToString();
        }

        // Log exception to Application Insights
        _telemetryClient?.TrackException(exception, new Dictionary<string, string>
        {
            ["tenantId"] = tenantId,
            ["actorId"] = actorId,
            ["operationType"] = operationType.ToString(),
            ["resourceType"] = resourceType,
            ["resourceId"] = resourceId ?? string.Empty
        });

        await LogAsync(entry);
    }

    /// <inheritdoc />
    public async Task LogUserOperationAsync(
        OperationType operation,
        string userId,
        string tenantId,
        string actorId,
        int httpStatus,
        long responseTimeMs)
    {
        await LogOperationAsync(operation, "User", userId, tenantId, actorId, httpStatus, responseTimeMs);
    }

    /// <inheritdoc />
    public async Task LogGroupOperationAsync(
        OperationType operation,
        string groupId,
        string tenantId,
        string actorId,
        int httpStatus,
        long responseTimeMs)
    {
        await LogOperationAsync(operation, "Group", groupId, tenantId, actorId, httpStatus, responseTimeMs);
    }

    /// <inheritdoc />
    public async Task LogAuthenticationAsync(
        string tenantId,
        string actorId,
        bool success,
        string? errorMessage = null)
    {
        var entry = new AuditLogEntry
        {
            OperationType = OperationType.Authenticate,
            ResourceType = "Authentication",
            ResourceId = string.Empty,
            TenantId = tenantId,
            ActorId = actorId,
            HttpStatus = success ? 200 : 401,
            ErrorMessage = errorMessage
        };

        await LogAsync(entry);
    }

    /// <inheritdoc />
    public async Task LogAdapterOperationAsync(
        string adapterId,
        OperationType operation,
        string tenantId,
        string actorId,
        bool success,
        string? errorMessage = null)
    {
        var entry = new AuditLogEntry
        {
            OperationType = operation,
            ResourceType = "Adapter",
            ResourceId = adapterId,
            TenantId = tenantId,
            ActorId = actorId,
            AdapterId = adapterId,
            HttpStatus = success ? 200 : 500,
            ErrorMessage = errorMessage
        };

        await LogAsync(entry);
    }

    /// <inheritdoc />
    public async Task LogAdapterErrorAsync(
        string adapterId,
        OperationType operation,
        string tenantId,
        string actorId,
        string? resourceType,
        string? resourceId,
        string? providerErrorCode,
        string errorMessage,
        int? httpStatusCode = null)
    {
        var entry = new AuditLogEntry
        {
            OperationType = operation,
            ResourceType = resourceType ?? "Unknown",
            ResourceId = resourceId ?? string.Empty,
            TenantId = tenantId,
            ActorId = actorId,
            AdapterId = adapterId,
            HttpStatus = httpStatusCode ?? 500,
            ErrorCode = providerErrorCode,
            ErrorMessage = errorMessage
        };

        // Log to Application Insights as an exception event
        _telemetryClient?.TrackEvent("AdapterError", new Dictionary<string, string>
        {
            ["tenantId"] = tenantId,
            ["actorId"] = actorId,
            ["adapterId"] = adapterId,
            ["operationType"] = operation.ToString(),
            ["resourceType"] = resourceType ?? "Unknown",
            ["resourceId"] = resourceId ?? string.Empty,
            ["providerErrorCode"] = providerErrorCode ?? string.Empty,
            ["errorMessage"] = errorMessage,
            ["httpStatusCode"] = (httpStatusCode ?? 500).ToString()
        });

        await LogAsync(entry);
    }

    private string GetEventName(OperationType operationType, string resourceType, bool isError)
    {
        if (isError)
            return _options.EventNames.Error;

        return (operationType, resourceType.ToLowerInvariant()) switch
        {
            (OperationType.Create, "user") => _options.EventNames.UserCreated,
            (OperationType.Read, "user") => _options.EventNames.UserRead,
            (OperationType.Update or OperationType.Patch, "user") => _options.EventNames.UserUpdated,
            (OperationType.Delete, "user") => _options.EventNames.UserDeleted,
            (OperationType.List, "user") => _options.EventNames.UserListed,
            (OperationType.Create, "group") => _options.EventNames.GroupCreated,
            (OperationType.Read, "group") => _options.EventNames.GroupRead,
            (OperationType.Update or OperationType.Patch, "group") => _options.EventNames.GroupUpdated,
            (OperationType.Delete, "group") => _options.EventNames.GroupDeleted,
            (OperationType.List, "group") => _options.EventNames.GroupListed,
            (OperationType.AddMember, _) => _options.EventNames.MemberAdded,
            (OperationType.RemoveMember, _) => _options.EventNames.MemberRemoved,
            (OperationType.Authenticate, _) when !isError => _options.EventNames.AuthSuccess,
            (OperationType.Authenticate, _) => _options.EventNames.AuthFailure,
            (OperationType.Sync, _) => _options.EventNames.SyncStarted,
            _ => $"SCIM_{operationType}_{resourceType}"
        };
    }

    private string SerializeValue(object value)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(value);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    private string RedactAndTruncate(string value)
    {
        var redacted = _options.EnablePiiRedaction 
            ? _piiRedactor.RedactJson(value)
            : value;

        if (redacted.Length > _options.MaxBodySize)
        {
            return redacted[.._options.MaxBodySize] + "...[truncated]";
        }

        return redacted;
    }

    // ==================== Transformation Operations (T105) ====================

    /// <inheritdoc />
    public async Task LogTransformationMatchAsync(
        string tenantId,
        string providerId,
        string groupDisplayName,
        IEnumerable<string> matchedRuleIds,
        IEnumerable<string> entitlementNames,
        string actorId)
    {
        var entry = new AuditLogEntry
        {
            OperationType = OperationType.Transform,
            ResourceType = "TransformationRule",
            ResourceId = groupDisplayName,
            TenantId = tenantId,
            ActorId = actorId,
            AdapterId = providerId,
            HttpStatus = 200,
            Metadata = new Dictionary<string, string>
            {
                ["providerId"] = providerId,
                ["groupDisplayName"] = groupDisplayName,
                ["matchedRuleIds"] = string.Join(",", matchedRuleIds),
                ["entitlementNames"] = string.Join(",", entitlementNames),
                ["eventType"] = "TransformationMatch"
            }
        };

        // Log to Application Insights
        _telemetryClient?.TrackEvent("SCIM_TransformationMatch", new Dictionary<string, string>
        {
            ["tenantId"] = tenantId,
            ["providerId"] = providerId,
            ["groupDisplayName"] = groupDisplayName,
            ["matchedRuleCount"] = matchedRuleIds.Count().ToString(),
            ["entitlementCount"] = entitlementNames.Count().ToString()
        });

        _logger.LogInformation(
            "Transformation match: Group {GroupName} matched {RuleCount} rules, producing {EntitlementCount} entitlements for provider {ProviderId}",
            groupDisplayName, matchedRuleIds.Count(), entitlementNames.Count(), providerId);

        await LogAsync(entry);
    }

    /// <inheritdoc />
    public async Task LogTransformationApplicationAsync(
        string tenantId,
        string providerId,
        string userId,
        string groupDisplayName,
        string entitlementName,
        string entitlementType,
        bool success,
        string actorId,
        string? errorMessage = null)
    {
        var entry = new AuditLogEntry
        {
            OperationType = success ? OperationType.AddMember : OperationType.Transform,
            ResourceType = "TransformationRule",
            ResourceId = $"{userId}:{entitlementName}",
            TenantId = tenantId,
            ActorId = actorId,
            AdapterId = providerId,
            HttpStatus = success ? 200 : 500,
            ErrorMessage = errorMessage,
            Metadata = new Dictionary<string, string>
            {
                ["providerId"] = providerId,
                ["userId"] = userId,
                ["groupDisplayName"] = groupDisplayName,
                ["entitlementName"] = entitlementName,
                ["entitlementType"] = entitlementType,
                ["success"] = success.ToString(),
                ["eventType"] = "TransformationApplication"
            }
        };

        // Log to Application Insights
        var eventName = success ? "SCIM_TransformationApplied" : "SCIM_TransformationFailed";
        _telemetryClient?.TrackEvent(eventName, new Dictionary<string, string>
        {
            ["tenantId"] = tenantId,
            ["providerId"] = providerId,
            ["userId"] = userId,
            ["groupDisplayName"] = groupDisplayName,
            ["entitlementName"] = entitlementName,
            ["entitlementType"] = entitlementType
        });

        if (success)
        {
            _logger.LogInformation(
                "Transformation applied: User {UserId} assigned entitlement {EntitlementName} ({EntitlementType}) via group {GroupName} to provider {ProviderId}",
                userId, entitlementName, entitlementType, groupDisplayName, providerId);
        }
        else
        {
            _logger.LogWarning(
                "Transformation failed: User {UserId} failed to receive entitlement {EntitlementName} ({EntitlementType}) via group {GroupName} to provider {ProviderId}: {Error}",
                userId, entitlementName, entitlementType, groupDisplayName, providerId, errorMessage);
        }

        await LogAsync(entry);
    }

    /// <inheritdoc />
    public async Task LogTransformationConflictAsync(
        string tenantId,
        string providerId,
        string groupDisplayName,
        IEnumerable<string> conflictingEntitlements,
        string resolutionStrategy,
        string? resolvedEntitlement,
        string actorId)
    {
        var entry = new AuditLogEntry
        {
            OperationType = OperationType.Transform,
            ResourceType = "TransformationConflict",
            ResourceId = groupDisplayName,
            TenantId = tenantId,
            ActorId = actorId,
            AdapterId = providerId,
            HttpStatus = 200,
            Metadata = new Dictionary<string, string>
            {
                ["providerId"] = providerId,
                ["groupDisplayName"] = groupDisplayName,
                ["conflictingEntitlements"] = string.Join(",", conflictingEntitlements),
                ["resolutionStrategy"] = resolutionStrategy,
                ["resolvedEntitlement"] = resolvedEntitlement ?? "none",
                ["eventType"] = "TransformationConflict"
            }
        };

        // Log to Application Insights
        _telemetryClient?.TrackEvent("SCIM_TransformationConflict", new Dictionary<string, string>
        {
            ["tenantId"] = tenantId,
            ["providerId"] = providerId,
            ["groupDisplayName"] = groupDisplayName,
            ["conflictCount"] = conflictingEntitlements.Count().ToString(),
            ["resolutionStrategy"] = resolutionStrategy,
            ["resolved"] = (!string.IsNullOrEmpty(resolvedEntitlement)).ToString()
        });

        _logger.LogWarning(
            "Transformation conflict: Group {GroupName} matched {ConflictCount} conflicting entitlements [{Entitlements}], resolved with {Strategy} to {Resolved}",
            groupDisplayName, conflictingEntitlements.Count(), string.Join(", ", conflictingEntitlements),
            resolutionStrategy, resolvedEntitlement ?? "none");

        await LogAsync(entry);
    }

    /// <inheritdoc />
    public async Task LogReverseTransformationAsync(
        string tenantId,
        string providerId,
        string entitlementId,
        string entitlementType,
        IEnumerable<string> matchedGroups,
        string actorId)
    {
        var entry = new AuditLogEntry
        {
            OperationType = OperationType.Transform,
            ResourceType = "ReverseTransformation",
            ResourceId = entitlementId,
            TenantId = tenantId,
            ActorId = actorId,
            AdapterId = providerId,
            HttpStatus = 200,
            Metadata = new Dictionary<string, string>
            {
                ["providerId"] = providerId,
                ["entitlementId"] = entitlementId,
                ["entitlementType"] = entitlementType,
                ["matchedGroups"] = string.Join(",", matchedGroups),
                ["matchCount"] = matchedGroups.Count().ToString(),
                ["eventType"] = "ReverseTransformation"
            }
        };

        // Log to Application Insights
        _telemetryClient?.TrackEvent("SCIM_ReverseTransformation", new Dictionary<string, string>
        {
            ["tenantId"] = tenantId,
            ["providerId"] = providerId,
            ["entitlementId"] = entitlementId,
            ["entitlementType"] = entitlementType,
            ["matchCount"] = matchedGroups.Count().ToString()
        });

        _logger.LogInformation(
            "Reverse transformation: Entitlement {EntitlementId} ({EntitlementType}) from provider {ProviderId} mapped to {MatchCount} groups [{Groups}]",
            entitlementId, entitlementType, providerId, matchedGroups.Count(), string.Join(", ", matchedGroups));

        await LogAsync(entry);
    }
}
