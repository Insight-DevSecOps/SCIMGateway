// ==========================================================================
// T027a: Contract Test for CosmosDbClient
// ==========================================================================
// Validates the CosmosDbClient component meets all requirements from:
// - FR-050: Azure Cosmos DB for data persistence
// - tasks.md T027a specification
// 
// Required behaviors to validate:
// - Connection with managed identity
// - Database and container references
// - Container operations for all 5 containers
// ==========================================================================

using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for CosmosDbClient.
/// These tests define the expected behavior for Azure Cosmos DB integration
/// using managed identity authentication.
/// </summary>
public class CosmosDbClientTests
{
    #region Interface Contract Tests

    [Fact]
    public void CosmosDbClient_Should_Exist_In_Core_Assembly()
    {
        // Arrange & Act
        var clientType = GetCosmosDbClientType();
        
        // Assert
        Assert.NotNull(clientType);
    }

    [Fact]
    public void CosmosDbClient_Should_Implement_ICosmosDbClient_Interface()
    {
        // Arrange & Act
        var clientType = GetCosmosDbClientType();
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(clientType);
        Assert.NotNull(interfaceType);
        Assert.True(interfaceType.IsAssignableFrom(clientType));
    }

    #endregion

    #region Container Access Methods Tests

    [Fact]
    public void ICosmosDbClient_Should_Have_GetUsersContainer_Method()
    {
        // Access users container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("GetUsersContainer");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICosmosDbClient_Should_Have_GetGroupsContainer_Method()
    {
        // Access groups container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("GetGroupsContainer");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICosmosDbClient_Should_Have_GetSyncStateContainer_Method()
    {
        // Access sync-state container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("GetSyncStateContainer");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICosmosDbClient_Should_Have_GetTransformationRulesContainer_Method()
    {
        // Access transformation-rules container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("GetTransformationRulesContainer");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICosmosDbClient_Should_Have_GetAuditLogsContainer_Method()
    {
        // Access audit-logs container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("GetAuditLogsContainer");
        Assert.NotNull(method);
    }

    #endregion

    #region CRUD Operations Methods Tests

    [Fact]
    public void ICosmosDbClient_Should_Have_CreateItemAsync_Method()
    {
        // Create item in container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("CreateItemAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICosmosDbClient_Should_Have_ReadItemAsync_Method()
    {
        // Read item from container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ReadItemAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICosmosDbClient_Should_Have_UpsertItemAsync_Method()
    {
        // Upsert item in container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("UpsertItemAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICosmosDbClient_Should_Have_DeleteItemAsync_Method()
    {
        // Delete item from container
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("DeleteItemAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ICosmosDbClient_Should_Have_QueryItemsAsync_Method()
    {
        // Query items with SQL or LINQ
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("QueryItemsAsync");
        Assert.NotNull(method);
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public void ICosmosDbClient_Should_Have_CheckHealthAsync_Method()
    {
        // Check database connectivity
        
        // Arrange & Act
        var interfaceType = GetICosmosDbClientType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("CheckHealthAsync");
        Assert.NotNull(method);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void CosmosDbOptions_Should_Exist()
    {
        // Arrange & Act
        var optionsType = GetCosmosDbOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
    }

    [Fact]
    public void CosmosDbOptions_Should_Have_Endpoint_Property()
    {
        // Cosmos DB account endpoint
        
        // Arrange & Act
        var optionsType = GetCosmosDbOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var endpointProperty = optionsType.GetProperty("Endpoint");
        Assert.NotNull(endpointProperty);
    }

    [Fact]
    public void CosmosDbOptions_Should_Have_DatabaseName_Property()
    {
        // Database name
        
        // Arrange & Act
        var optionsType = GetCosmosDbOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var databaseNameProperty = optionsType.GetProperty("DatabaseName");
        Assert.NotNull(databaseNameProperty);
    }

    [Fact]
    public void CosmosDbOptions_Should_Have_UseManagedIdentity_Property()
    {
        // Use managed identity authentication
        
        // Arrange & Act
        var optionsType = GetCosmosDbOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var useManagedIdentityProperty = optionsType.GetProperty("UseManagedIdentity");
        Assert.NotNull(useManagedIdentityProperty);
    }

    #endregion

    #region Container Names Tests

    [Fact]
    public void CosmosContainerNames_Should_Exist()
    {
        // Constants for container names
        
        // Arrange & Act
        var namesType = GetCosmosContainerNamesType();
        
        // Assert
        Assert.NotNull(namesType);
    }

    [Fact]
    public void CosmosContainerNames_Should_Have_Users_Constant()
    {
        // "users" container
        
        // Arrange & Act
        var namesType = GetCosmosContainerNamesType();
        
        // Assert
        Assert.NotNull(namesType);
        var field = namesType.GetField("Users");
        Assert.NotNull(field);
    }

    [Fact]
    public void CosmosContainerNames_Should_Have_Groups_Constant()
    {
        // "groups" container
        
        // Arrange & Act
        var namesType = GetCosmosContainerNamesType();
        
        // Assert
        Assert.NotNull(namesType);
        var field = namesType.GetField("Groups");
        Assert.NotNull(field);
    }

    [Fact]
    public void CosmosContainerNames_Should_Have_SyncState_Constant()
    {
        // "sync-state" container
        
        // Arrange & Act
        var namesType = GetCosmosContainerNamesType();
        
        // Assert
        Assert.NotNull(namesType);
        var field = namesType.GetField("SyncState");
        Assert.NotNull(field);
    }

    [Fact]
    public void CosmosContainerNames_Should_Have_TransformationRules_Constant()
    {
        // "transformation-rules" container
        
        // Arrange & Act
        var namesType = GetCosmosContainerNamesType();
        
        // Assert
        Assert.NotNull(namesType);
        var field = namesType.GetField("TransformationRules");
        Assert.NotNull(field);
    }

    [Fact]
    public void CosmosContainerNames_Should_Have_AuditLogs_Constant()
    {
        // "audit-logs" container
        
        // Arrange & Act
        var namesType = GetCosmosContainerNamesType();
        
        // Assert
        Assert.NotNull(namesType);
        var field = namesType.GetField("AuditLogs");
        Assert.NotNull(field);
    }

    #endregion

    #region Helper Methods

    private static Type? GetCosmosDbClientType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.CosmosDbClient")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Data.CosmosDbClient");
    }

    private static Type? GetICosmosDbClientType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.ICosmosDbClient")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Data.ICosmosDbClient");
    }

    private static Type? GetCosmosDbOptionsType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Configuration.CosmosDbOptions")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Data.CosmosDbOptions");
    }

    private static Type? GetCosmosContainerNamesType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Constants.CosmosContainerNames")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Data.CosmosContainerNames");
    }

    #endregion
}
