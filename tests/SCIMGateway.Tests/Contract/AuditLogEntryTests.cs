// ==========================================================================
// T022a: Contract Test for AuditLogEntry Model
// ==========================================================================
// Validates the AuditLogEntry model meets all requirements from:
// - FR-011: Comprehensive audit logging
// - tasks.md T022a specification
// 
// Required fields per FR-011:
// - timestamp, tenantId, actorId, operationType, resourceType, resourceId
// - httpStatus, responseTimeMs, requestId, adapterId
// - oldValue, newValue, errorCode, errorMessage
// ==========================================================================

using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for AuditLogEntry model.
/// These tests define the expected schema for audit log entries
/// stored in Cosmos DB and sent to Application Insights.
/// </summary>
public class AuditLogEntryTests
{
    #region Model Existence Tests

    [Fact]
    public void AuditLogEntry_Should_Exist_In_Core_Assembly()
    {
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
    }

    #endregion

    #region Required Fields Tests (per FR-011)

    [Fact]
    public void AuditLogEntry_Should_Have_Id_Property()
    {
        // Unique identifier for the log entry
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var idProperty = entryType.GetProperty("Id");
        Assert.NotNull(idProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_Timestamp_Property()
    {
        // ISO 8601 timestamp
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var timestampProperty = entryType.GetProperty("Timestamp");
        Assert.NotNull(timestampProperty);
        Assert.Equal(typeof(DateTimeOffset), timestampProperty.PropertyType);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_TenantId_Property()
    {
        // Tenant identifier for isolation
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var tenantIdProperty = entryType.GetProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ActorId_Property()
    {
        // oid claim from token (who performed the action)
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var actorIdProperty = entryType.GetProperty("ActorId");
        Assert.NotNull(actorIdProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ActorType_Property()
    {
        // User or ServicePrincipal
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var actorTypeProperty = entryType.GetProperty("ActorType");
        Assert.NotNull(actorTypeProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_OperationType_Property()
    {
        // CREATE, READ, UPDATE, DELETE, LIST, PATCH
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var operationTypeProperty = entryType.GetProperty("OperationType");
        Assert.NotNull(operationTypeProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ResourceType_Property()
    {
        // User, Group, Schema, etc.
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var resourceTypeProperty = entryType.GetProperty("ResourceType");
        Assert.NotNull(resourceTypeProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ResourceId_Property()
    {
        // SCIM resource identifier
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var resourceIdProperty = entryType.GetProperty("ResourceId");
        Assert.NotNull(resourceIdProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_UserName_Property()
    {
        // Target user's userName (if applicable)
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var userNameProperty = entryType.GetProperty("UserName");
        Assert.NotNull(userNameProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_HttpStatus_Property()
    {
        // HTTP status code of the response
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var httpStatusProperty = entryType.GetProperty("HttpStatus");
        Assert.NotNull(httpStatusProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ResponseTimeMs_Property()
    {
        // Response time in milliseconds
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var responseTimeMsProperty = entryType.GetProperty("ResponseTimeMs");
        Assert.NotNull(responseTimeMsProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_RequestId_Property()
    {
        // Correlation ID for request tracing
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var requestIdProperty = entryType.GetProperty("RequestId");
        Assert.NotNull(requestIdProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_AdapterId_Property()
    {
        // Provider adapter ID (if applicable)
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var adapterIdProperty = entryType.GetProperty("AdapterId");
        Assert.NotNull(adapterIdProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_OldValue_Property()
    {
        // Previous state (with PII redaction)
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var oldValueProperty = entryType.GetProperty("OldValue");
        Assert.NotNull(oldValueProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_NewValue_Property()
    {
        // New state (with PII redaction)
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var newValueProperty = entryType.GetProperty("NewValue");
        Assert.NotNull(newValueProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ErrorCode_Property()
    {
        // Error code (if operation failed)
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var errorCodeProperty = entryType.GetProperty("ErrorCode");
        Assert.NotNull(errorCodeProperty);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ErrorMessage_Property()
    {
        // Error message (if operation failed)
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var errorMessageProperty = entryType.GetProperty("ErrorMessage");
        Assert.NotNull(errorMessageProperty);
    }

    #endregion

    #region Cosmos DB Schema Tests

    [Fact]
    public void AuditLogEntry_TenantId_Should_Be_PartitionKey()
    {
        // TenantId is the partition key per cosmos-db-schema.md
        // This is documented and enforced in the schema, not the model
        
        // Arrange & Act
        var entryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(entryType);
        var tenantIdProperty = entryType.GetProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
        // Note: Partition key attribute validation would be done in schema tests
    }

    #endregion

    #region Helper Methods

    private static Type? GetAuditLogEntryType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.AuditLogEntry")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Logging.AuditLogEntry");
    }

    #endregion
}
