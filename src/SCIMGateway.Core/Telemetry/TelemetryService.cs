// ==========================================================================
// T015: TelemetryService - Application Insights Integration
// ==========================================================================
// Provides telemetry collection and custom event tracking
// ==========================================================================

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace SCIMGateway.Core.Telemetry;

/// <summary>
/// Interface for telemetry service.
/// </summary>
public interface ITelemetryService : IDisposable
{
    /// <summary>
    /// Tracks a SCIM operation event.
    /// </summary>
    Task TrackScimOperationAsync(ScimOperationEvent operationEvent);

    /// <summary>
    /// Tracks a SCIM operation event (synchronous).
    /// </summary>
    void TrackScimOperation(ScimOperationEvent operationEvent);

    /// <summary>
    /// Tracks an adapter operation.
    /// </summary>
    Task TrackAdapterOperationAsync(string adapterId, string operation, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks an adapter operation (synchronous).
    /// </summary>
    void TrackAdapterOperation(string adapterId, string operation, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks a transformation operation.
    /// </summary>
    Task TrackTransformationAsync(string transformationType, TimeSpan duration, int itemCount);

    /// <summary>
    /// Tracks a transformation operation (synchronous).
    /// </summary>
    void TrackTransformation(string transformationType, TimeSpan duration, int itemCount);

    /// <summary>
    /// Tracks a drift detection event.
    /// </summary>
    Task TrackDriftDetectionAsync(string tenantId, string resourceType, int driftCount);

    /// <summary>
    /// Tracks a drift detection event (synchronous).
    /// </summary>
    void TrackDriftDetection(string tenantId, string resourceType, int driftCount);

    /// <summary>
    /// Tracks an exception.
    /// </summary>
    Task TrackExceptionAsync(Exception exception, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks an exception (synchronous).
    /// </summary>
    void TrackException(Exception exception, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks a dependency call.
    /// </summary>
    Task TrackDependencyAsync(string dependencyType, string target, string name, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks a dependency call (synchronous).
    /// </summary>
    void TrackDependency(string dependencyType, string target, string name, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks a metric.
    /// </summary>
    Task TrackMetricAsync(string metricName, double value, IDictionary<string, string>? dimensions = null);

    /// <summary>
    /// Tracks a metric (synchronous).
    /// </summary>
    void TrackMetric(string metricName, double value, IDictionary<string, string>? dimensions = null);

    /// <summary>
    /// Sets the correlation context for telemetry.
    /// </summary>
    void SetCorrelationContext(CorrelationContext context);

    /// <summary>
    /// Flushes all telemetry.
    /// </summary>
    Task FlushAsync();

    /// <summary>
    /// Flushes all telemetry (synchronous).
    /// </summary>
    void Flush();
}

/// <summary>
/// SCIM operation event for telemetry.
/// </summary>
public class ScimOperationEvent
{
    /// <summary>
    /// Type of operation (CREATE, READ, UPDATE, DELETE, LIST).
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Type of resource (User, Group).
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Resource identifier.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Actor (user) who performed the operation.
    /// </summary>
    public string? ActorId { get; set; }

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public int HttpStatusCode { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Request correlation ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the operation.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Correlation context for distributed tracing.
/// </summary>
public class CorrelationContext
{
    /// <summary>
    /// Request correlation ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Operation ID for distributed tracing.
    /// </summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>
    /// Parent ID for distributed tracing.
    /// </summary>
    public string? ParentId { get; set; }
}

/// <summary>
/// Constants for telemetry event names.
/// </summary>
public static class TelemetryEventNames
{
    /// <summary>SCIM user created event.</summary>
    public const string ScimUserCreated = "ScimUserCreated";

    /// <summary>SCIM user updated event.</summary>
    public const string ScimUserUpdated = "ScimUserUpdated";

    /// <summary>SCIM user deleted event.</summary>
    public const string ScimUserDeleted = "ScimUserDeleted";

    /// <summary>SCIM group created event.</summary>
    public const string ScimGroupCreated = "ScimGroupCreated";

    /// <summary>SCIM group updated event.</summary>
    public const string ScimGroupUpdated = "ScimGroupUpdated";

    /// <summary>SCIM group deleted event.</summary>
    public const string ScimGroupDeleted = "ScimGroupDeleted";

    /// <summary>Drift detected event.</summary>
    public const string DriftDetected = "DriftDetected";

    /// <summary>Conflict detected event.</summary>
    public const string ConflictDetected = "ConflictDetected";

    /// <summary>Adapter operation event.</summary>
    public const string AdapterOperation = "AdapterOperation";

    /// <summary>Transformation event.</summary>
    public const string Transformation = "Transformation";
}

/// <summary>
/// Application Insights telemetry service implementation.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private CorrelationContext? _correlationContext;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the TelemetryService.
    /// </summary>
    public TelemetryService(TelemetryConfiguration configuration)
    {
        _telemetryClient = new TelemetryClient(configuration);
    }

    /// <summary>
    /// Initializes a new instance with connection string.
    /// </summary>
    public TelemetryService(string connectionString)
    {
        var config = TelemetryConfiguration.CreateDefault();
        config.ConnectionString = connectionString;
        _telemetryClient = new TelemetryClient(config);
    }

    /// <inheritdoc />
    public Task TrackScimOperationAsync(ScimOperationEvent operationEvent)
    {
        TrackScimOperation(operationEvent);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TrackScimOperation(ScimOperationEvent operationEvent)
    {
        var eventName = GetEventName(operationEvent.OperationType, operationEvent.ResourceType);
        var eventTelemetry = new EventTelemetry(eventName);
        
        eventTelemetry.Properties["OperationType"] = operationEvent.OperationType;
        eventTelemetry.Properties["ResourceType"] = operationEvent.ResourceType;
        eventTelemetry.Properties["TenantId"] = operationEvent.TenantId;
        eventTelemetry.Properties["HttpStatusCode"] = operationEvent.HttpStatusCode.ToString();
        eventTelemetry.Properties["RequestId"] = operationEvent.RequestId;
        
        if (!string.IsNullOrEmpty(operationEvent.ResourceId))
            eventTelemetry.Properties["ResourceId"] = operationEvent.ResourceId;
        if (!string.IsNullOrEmpty(operationEvent.ActorId))
            eventTelemetry.Properties["ActorId"] = operationEvent.ActorId;
        
        eventTelemetry.Metrics["DurationMs"] = operationEvent.DurationMs;
        eventTelemetry.Timestamp = operationEvent.Timestamp;

        ApplyCorrelationContext(eventTelemetry);
        _telemetryClient.TrackEvent(eventTelemetry);
    }

    /// <inheritdoc />
    public Task TrackAdapterOperationAsync(string adapterId, string operation, TimeSpan duration, bool success)
    {
        TrackAdapterOperation(adapterId, operation, duration, success);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TrackAdapterOperation(string adapterId, string operation, TimeSpan duration, bool success)
    {
        var eventTelemetry = new EventTelemetry(TelemetryEventNames.AdapterOperation);
        eventTelemetry.Properties["AdapterId"] = adapterId;
        eventTelemetry.Properties["Operation"] = operation;
        eventTelemetry.Properties["Success"] = success.ToString();
        eventTelemetry.Metrics["DurationMs"] = duration.TotalMilliseconds;

        ApplyCorrelationContext(eventTelemetry);
        _telemetryClient.TrackEvent(eventTelemetry);
    }

    /// <inheritdoc />
    public Task TrackTransformationAsync(string transformationType, TimeSpan duration, int itemCount)
    {
        TrackTransformation(transformationType, duration, itemCount);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TrackTransformation(string transformationType, TimeSpan duration, int itemCount)
    {
        var eventTelemetry = new EventTelemetry(TelemetryEventNames.Transformation);
        eventTelemetry.Properties["TransformationType"] = transformationType;
        eventTelemetry.Metrics["DurationMs"] = duration.TotalMilliseconds;
        eventTelemetry.Metrics["ItemCount"] = itemCount;

        ApplyCorrelationContext(eventTelemetry);
        _telemetryClient.TrackEvent(eventTelemetry);
    }

    /// <inheritdoc />
    public Task TrackDriftDetectionAsync(string tenantId, string resourceType, int driftCount)
    {
        TrackDriftDetection(tenantId, resourceType, driftCount);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TrackDriftDetection(string tenantId, string resourceType, int driftCount)
    {
        var eventTelemetry = new EventTelemetry(TelemetryEventNames.DriftDetected);
        eventTelemetry.Properties["TenantId"] = tenantId;
        eventTelemetry.Properties["ResourceType"] = resourceType;
        eventTelemetry.Metrics["DriftCount"] = driftCount;

        ApplyCorrelationContext(eventTelemetry);
        _telemetryClient.TrackEvent(eventTelemetry);
    }

    /// <inheritdoc />
    public Task TrackExceptionAsync(Exception exception, IDictionary<string, string>? properties = null)
    {
        TrackException(exception, properties);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null)
    {
        var exceptionTelemetry = new ExceptionTelemetry(exception);
        
        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                exceptionTelemetry.Properties[kvp.Key] = kvp.Value;
            }
        }

        ApplyCorrelationContext(exceptionTelemetry);
        _telemetryClient.TrackException(exceptionTelemetry);
    }

    /// <inheritdoc />
    public Task TrackDependencyAsync(string dependencyType, string target, string name, TimeSpan duration, bool success)
    {
        TrackDependency(dependencyType, target, name, duration, success);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TrackDependency(string dependencyType, string target, string name, TimeSpan duration, bool success)
    {
        var dependencyTelemetry = new DependencyTelemetry(
            dependencyType,
            target,
            name,
            data: null,
            DateTimeOffset.UtcNow - duration,
            duration,
            resultCode: success ? "200" : "500",
            success);

        ApplyCorrelationContext(dependencyTelemetry);
        _telemetryClient.TrackDependency(dependencyTelemetry);
    }

    /// <inheritdoc />
    public Task TrackMetricAsync(string metricName, double value, IDictionary<string, string>? dimensions = null)
    {
        TrackMetric(metricName, value, dimensions);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void TrackMetric(string metricName, double value, IDictionary<string, string>? dimensions = null)
    {
        var metricTelemetry = new MetricTelemetry(metricName, value);
        
        if (dimensions != null)
        {
            foreach (var kvp in dimensions)
            {
                metricTelemetry.Properties[kvp.Key] = kvp.Value;
            }
        }

        ApplyCorrelationContext(metricTelemetry);
        _telemetryClient.TrackMetric(metricTelemetry);
    }

    /// <inheritdoc />
    public void SetCorrelationContext(CorrelationContext context)
    {
        _correlationContext = context;
    }

    /// <inheritdoc />
    public Task FlushAsync()
    {
        Flush();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Flush()
    {
        _telemetryClient.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _telemetryClient.Flush();
        }

        _disposed = true;
    }

    private static string GetEventName(string operationType, string resourceType)
    {
        return (operationType.ToUpperInvariant(), resourceType.ToLowerInvariant()) switch
        {
            ("CREATE", "user") => TelemetryEventNames.ScimUserCreated,
            ("UPDATE", "user") => TelemetryEventNames.ScimUserUpdated,
            ("DELETE", "user") => TelemetryEventNames.ScimUserDeleted,
            ("CREATE", "group") => TelemetryEventNames.ScimGroupCreated,
            ("UPDATE", "group") => TelemetryEventNames.ScimGroupUpdated,
            ("DELETE", "group") => TelemetryEventNames.ScimGroupDeleted,
            _ => $"Scim{operationType}{resourceType}"
        };
    }

    private void ApplyCorrelationContext(ITelemetry telemetry)
    {
        if (_correlationContext == null) return;

        telemetry.Context.Operation.Id = _correlationContext.OperationId;
        telemetry.Context.Operation.ParentId = _correlationContext.ParentId;
        
        if (telemetry is ISupportProperties supportProperties)
        {
            supportProperties.Properties["RequestId"] = _correlationContext.RequestId;
        }
    }
}
