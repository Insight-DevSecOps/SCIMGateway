// ==========================================================================
// T015a: Contract Test for Application Insights SDK Integration
// ==========================================================================
// Validates the Application Insights integration meets all requirements from:
// - FR-011: Comprehensive audit logging to Application Insights
// - FR-012: Custom telemetry events for CRUD operations
// - tasks.md T015a specification
// 
// Required behaviors to validate:
// - Telemetry client initialization
// - Custom event schema for SCIM operations
// - Correlation ID propagation
// - PII handling in telemetry
// ==========================================================================

using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for Application Insights SDK integration.
/// These tests define the expected behavior for telemetry collection
/// and custom event tracking.
/// </summary>
public class ApplicationInsightsTests
{
    #region Telemetry Client Tests

    [Fact]
    public void TelemetryService_Should_Exist_In_Core_Assembly()
    {
        // Arrange & Act
        var telemetryServiceType = GetTelemetryServiceType();
        
        // Assert
        Assert.NotNull(telemetryServiceType);
    }

    [Fact]
    public void TelemetryService_Should_Implement_ITelemetryService_Interface()
    {
        // Arrange & Act
        var telemetryServiceType = GetTelemetryServiceType();
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(telemetryServiceType);
        Assert.NotNull(interfaceType);
        Assert.True(interfaceType.IsAssignableFrom(telemetryServiceType));
    }

    #endregion

    #region Custom Event Methods Tests

    [Fact]
    public void ITelemetryService_Should_Have_TrackScimOperationAsync_Method()
    {
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("TrackScimOperationAsync") 
            ?? interfaceType.GetMethod("TrackScimOperation");
        Assert.NotNull(method);
    }

    [Fact]
    public void ITelemetryService_Should_Have_TrackAdapterOperationAsync_Method()
    {
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("TrackAdapterOperationAsync")
            ?? interfaceType.GetMethod("TrackAdapterOperation");
        Assert.NotNull(method);
    }

    [Fact]
    public void ITelemetryService_Should_Have_TrackTransformationAsync_Method()
    {
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("TrackTransformationAsync")
            ?? interfaceType.GetMethod("TrackTransformation");
        Assert.NotNull(method);
    }

    [Fact]
    public void ITelemetryService_Should_Have_TrackDriftDetectionAsync_Method()
    {
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("TrackDriftDetectionAsync")
            ?? interfaceType.GetMethod("TrackDriftDetection");
        Assert.NotNull(method);
    }

    [Fact]
    public void ITelemetryService_Should_Have_TrackExceptionAsync_Method()
    {
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("TrackExceptionAsync")
            ?? interfaceType.GetMethod("TrackException");
        Assert.NotNull(method);
    }

    [Fact]
    public void ITelemetryService_Should_Have_TrackDependencyAsync_Method()
    {
        // Track external dependencies (Cosmos DB, Key Vault, Provider APIs)
        
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("TrackDependencyAsync")
            ?? interfaceType.GetMethod("TrackDependency");
        Assert.NotNull(method);
    }

    [Fact]
    public void ITelemetryService_Should_Have_TrackMetricAsync_Method()
    {
        // Track performance metrics
        
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("TrackMetricAsync")
            ?? interfaceType.GetMethod("TrackMetric");
        Assert.NotNull(method);
    }

    #endregion

    #region SCIM Operation Event Schema Tests

    [Fact]
    public void ScimOperationEvent_Should_Exist()
    {
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_OperationType_Property()
    {
        // CREATE, READ, UPDATE, DELETE, LIST
        
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var operationTypeProperty = eventType.GetProperty("OperationType");
        Assert.NotNull(operationTypeProperty);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_ResourceType_Property()
    {
        // User, Group, etc.
        
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var resourceTypeProperty = eventType.GetProperty("ResourceType");
        Assert.NotNull(resourceTypeProperty);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_ResourceId_Property()
    {
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var resourceIdProperty = eventType.GetProperty("ResourceId");
        Assert.NotNull(resourceIdProperty);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_TenantId_Property()
    {
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var tenantIdProperty = eventType.GetProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_ActorId_Property()
    {
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var actorIdProperty = eventType.GetProperty("ActorId");
        Assert.NotNull(actorIdProperty);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_HttpStatusCode_Property()
    {
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var httpStatusCodeProperty = eventType.GetProperty("HttpStatusCode");
        Assert.NotNull(httpStatusCodeProperty);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_DurationMs_Property()
    {
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var durationMsProperty = eventType.GetProperty("DurationMs");
        Assert.NotNull(durationMsProperty);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_RequestId_Property()
    {
        // Correlation ID
        
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var requestIdProperty = eventType.GetProperty("RequestId");
        Assert.NotNull(requestIdProperty);
    }

    [Fact]
    public void ScimOperationEvent_Should_Have_Timestamp_Property()
    {
        // Arrange & Act
        var eventType = GetScimOperationEventType();
        
        // Assert
        Assert.NotNull(eventType);
        var timestampProperty = eventType.GetProperty("Timestamp");
        Assert.NotNull(timestampProperty);
    }

    #endregion

    #region Custom Event Names Tests

    [Fact]
    public void TelemetryEventNames_Should_Exist()
    {
        // Arrange & Act
        var eventNamesType = GetTelemetryEventNamesType();
        
        // Assert
        Assert.NotNull(eventNamesType);
    }

    [Fact]
    public void TelemetryEventNames_Should_Have_ScimUserCreated_Constant()
    {
        // Arrange & Act
        var eventNamesType = GetTelemetryEventNamesType();
        
        // Assert
        Assert.NotNull(eventNamesType);
        var field = eventNamesType.GetField("ScimUserCreated");
        Assert.NotNull(field);
    }

    [Fact]
    public void TelemetryEventNames_Should_Have_ScimUserUpdated_Constant()
    {
        // Arrange & Act
        var eventNamesType = GetTelemetryEventNamesType();
        
        // Assert
        Assert.NotNull(eventNamesType);
        var field = eventNamesType.GetField("ScimUserUpdated");
        Assert.NotNull(field);
    }

    [Fact]
    public void TelemetryEventNames_Should_Have_ScimUserDeleted_Constant()
    {
        // Arrange & Act
        var eventNamesType = GetTelemetryEventNamesType();
        
        // Assert
        Assert.NotNull(eventNamesType);
        var field = eventNamesType.GetField("ScimUserDeleted");
        Assert.NotNull(field);
    }

    [Fact]
    public void TelemetryEventNames_Should_Have_ScimGroupCreated_Constant()
    {
        // Arrange & Act
        var eventNamesType = GetTelemetryEventNamesType();
        
        // Assert
        Assert.NotNull(eventNamesType);
        var field = eventNamesType.GetField("ScimGroupCreated");
        Assert.NotNull(field);
    }

    [Fact]
    public void TelemetryEventNames_Should_Have_DriftDetected_Constant()
    {
        // Arrange & Act
        var eventNamesType = GetTelemetryEventNamesType();
        
        // Assert
        Assert.NotNull(eventNamesType);
        var field = eventNamesType.GetField("DriftDetected");
        Assert.NotNull(field);
    }

    [Fact]
    public void TelemetryEventNames_Should_Have_ConflictDetected_Constant()
    {
        // Arrange & Act
        var eventNamesType = GetTelemetryEventNamesType();
        
        // Assert
        Assert.NotNull(eventNamesType);
        var field = eventNamesType.GetField("ConflictDetected");
        Assert.NotNull(field);
    }

    #endregion

    #region Correlation Context Tests

    [Fact]
    public void ITelemetryService_Should_Have_SetCorrelationContext_Method()
    {
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("SetCorrelationContext");
        Assert.NotNull(method);
    }

    [Fact]
    public void CorrelationContext_Should_Exist()
    {
        // Arrange & Act
        var contextType = GetCorrelationContextType();
        
        // Assert
        Assert.NotNull(contextType);
    }

    [Fact]
    public void CorrelationContext_Should_Have_RequestId_Property()
    {
        // Arrange & Act
        var contextType = GetCorrelationContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var requestIdProperty = contextType.GetProperty("RequestId");
        Assert.NotNull(requestIdProperty);
    }

    [Fact]
    public void CorrelationContext_Should_Have_OperationId_Property()
    {
        // Arrange & Act
        var contextType = GetCorrelationContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var operationIdProperty = contextType.GetProperty("OperationId");
        Assert.NotNull(operationIdProperty);
    }

    [Fact]
    public void CorrelationContext_Should_Have_ParentId_Property()
    {
        // Arrange & Act
        var contextType = GetCorrelationContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var parentIdProperty = contextType.GetProperty("ParentId");
        Assert.NotNull(parentIdProperty);
    }

    #endregion

    #region Flush and Dispose Tests

    [Fact]
    public void ITelemetryService_Should_Have_FlushAsync_Method()
    {
        // Ensure telemetry is flushed before app shutdown
        
        // Arrange & Act
        var interfaceType = GetITelemetryServiceType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("FlushAsync") 
            ?? interfaceType.GetMethod("Flush");
        Assert.NotNull(method);
    }

    [Fact]
    public void TelemetryService_Should_Implement_IDisposable()
    {
        // Arrange & Act
        var telemetryServiceType = GetTelemetryServiceType();
        
        // Assert
        Assert.NotNull(telemetryServiceType);
        Assert.True(typeof(IDisposable).IsAssignableFrom(telemetryServiceType) ||
                    typeof(IAsyncDisposable).IsAssignableFrom(telemetryServiceType));
    }

    #endregion

    #region Helper Methods

    private static Type? GetTelemetryServiceType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Telemetry.TelemetryService")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Core.TelemetryService");
    }

    private static Type? GetITelemetryServiceType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Telemetry.ITelemetryService")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Core.ITelemetryService");
    }

    private static Type? GetScimOperationEventType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Telemetry.ScimOperationEvent")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Events.ScimOperationEvent");
    }

    private static Type? GetTelemetryEventNamesType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Telemetry.TelemetryEventNames")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Constants.TelemetryEventNames");
    }

    private static Type? GetCorrelationContextType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Telemetry.CorrelationContext")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Context.CorrelationContext");
    }

    #endregion
}
