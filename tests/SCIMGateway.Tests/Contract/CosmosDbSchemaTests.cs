// ==========================================================================
// T028a: Contract Test for CosmosDbSchema
// ==========================================================================
// Validates the Cosmos DB schema configuration meets all requirements from:
// - FR-050: Azure Cosmos DB for data persistence
// - FR-007: Multi-tenant data isolation via /tenantId partition key
// - tasks.md T028a specification
// 
// Required behaviors to validate:
// - Partition key configuration (/tenantId for tenant isolation)
// - Indexing policies for query optimization
// - TTL configuration for audit logs
// ==========================================================================

using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for Cosmos DB schema configuration.
/// These tests define the expected behavior for partition keys,
/// indexing policies, and TTL configuration.
/// </summary>
public class CosmosDbSchemaTests
{
    #region Partition Key Tests - Users Container

    [Fact]
    public void UsersContainer_Should_Have_TenantId_PartitionKey()
    {
        // Per FR-007: Tenant isolation via partition key
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("users");
        
        // Assert
        Assert.NotNull(containerConfig);
        Assert.Equal("/tenantId", containerConfig.PartitionKeyPath);
    }

    [Fact]
    public void UsersContainer_Should_Support_High_Cardinality()
    {
        // Many tenants with many users
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("users");
        
        // Assert
        Assert.NotNull(containerConfig);
        // Partition key should enable even distribution
        Assert.Equal("/tenantId", containerConfig.PartitionKeyPath);
    }

    #endregion

    #region Partition Key Tests - Groups Container

    [Fact]
    public void GroupsContainer_Should_Have_TenantId_PartitionKey()
    {
        // Per FR-007: Tenant isolation via partition key
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("groups");
        
        // Assert
        Assert.NotNull(containerConfig);
        Assert.Equal("/tenantId", containerConfig.PartitionKeyPath);
    }

    #endregion

    #region Partition Key Tests - SyncState Container

    [Fact]
    public void SyncStateContainer_Should_Have_TenantId_PartitionKey()
    {
        // Per FR-007: Tenant isolation via partition key
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("sync-state");
        
        // Assert
        Assert.NotNull(containerConfig);
        Assert.Equal("/tenantId", containerConfig.PartitionKeyPath);
    }

    #endregion

    #region Partition Key Tests - TransformationRules Container

    [Fact]
    public void TransformationRulesContainer_Should_Have_TenantId_PartitionKey()
    {
        // Per FR-007: Tenant isolation via partition key
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("transformation-rules");
        
        // Assert
        Assert.NotNull(containerConfig);
        Assert.Equal("/tenantId", containerConfig.PartitionKeyPath);
    }

    #endregion

    #region Partition Key Tests - AuditLogs Container

    [Fact]
    public void AuditLogsContainer_Should_Have_TenantId_PartitionKey()
    {
        // Per FR-007: Tenant isolation via partition key
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("audit-logs");
        
        // Assert
        Assert.NotNull(containerConfig);
        Assert.Equal("/tenantId", containerConfig.PartitionKeyPath);
    }

    #endregion

    #region TTL Configuration Tests

    [Fact]
    public void AuditLogsContainer_Should_Have_TTL_Enabled()
    {
        // Audit logs should expire based on retention policy
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("audit-logs");
        
        // Assert
        Assert.NotNull(containerConfig);
        Assert.True(containerConfig.DefaultTimeToLive.HasValue || containerConfig.TimeToLivePropertyPath != null);
    }

    [Fact]
    public void AuditLogsContainer_TTL_Should_Default_To_90_Days()
    {
        // Default retention period: 90 days = 7,776,000 seconds
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("audit-logs");
        
        // Assert
        Assert.NotNull(containerConfig);
        // TTL in seconds: 90 days * 24 hours * 60 minutes * 60 seconds
        var expectedTtlSeconds = 90 * 24 * 60 * 60;
        Assert.Equal(expectedTtlSeconds, containerConfig.DefaultTimeToLive ?? 0);
    }

    [Fact]
    public void AuditLogsContainer_Should_Support_Per_Item_TTL()
    {
        // Individual items can override default TTL
        
        // Arrange & Act
        var containerConfig = GetContainerConfiguration("audit-logs");
        
        // Assert
        Assert.NotNull(containerConfig);
        // When TTL is enabled, per-item TTL is supported via /ttl property
        Assert.True(containerConfig.EnablePerItemTtl);
    }

    #endregion

    #region Indexing Policy Tests - Users Container

    [Fact]
    public void UsersContainer_Should_Index_UserName()
    {
        // Required for SCIM filter: userName eq "value"
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("users");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/userName"));
    }

    [Fact]
    public void UsersContainer_Should_Index_ExternalId()
    {
        // Required for SCIM filter: externalId eq "value"
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("users");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/externalId"));
    }

    [Fact]
    public void UsersContainer_Should_Index_Email()
    {
        // Required for SCIM filter: emails.value eq "value"
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("users");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/emails"));
    }

    [Fact]
    public void UsersContainer_Should_Index_Active()
    {
        // Required for SCIM filter: active eq true
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("users");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/active"));
    }

    #endregion

    #region Indexing Policy Tests - Groups Container

    [Fact]
    public void GroupsContainer_Should_Index_DisplayName()
    {
        // Required for SCIM filter: displayName eq "value"
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("groups");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/displayName"));
    }

    [Fact]
    public void GroupsContainer_Should_Index_ExternalId()
    {
        // Required for SCIM filter: externalId eq "value"
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("groups");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/externalId"));
    }

    #endregion

    #region Indexing Policy Tests - AuditLogs Container

    [Fact]
    public void AuditLogsContainer_Should_Index_Timestamp()
    {
        // Required for time-range queries
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("audit-logs");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/timestamp"));
    }

    [Fact]
    public void AuditLogsContainer_Should_Index_Operation()
    {
        // Required for filtering by operation type
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("audit-logs");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/operation"));
    }

    [Fact]
    public void AuditLogsContainer_Should_Index_ResourceType()
    {
        // Required for filtering by resource type
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("audit-logs");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.Contains(indexingPolicy.IncludedPaths, p => p.Contains("/resourceType"));
    }

    #endregion

    #region Composite Index Tests

    [Fact]
    public void UsersContainer_Should_Have_Composite_Index_For_Common_Queries()
    {
        // Composite index for efficient sorting and filtering
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("users");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.NotEmpty(indexingPolicy.CompositeIndexes);
    }

    [Fact]
    public void AuditLogsContainer_Should_Have_Composite_Index_For_Time_Queries()
    {
        // Composite index: /tenantId + /timestamp for efficient audit queries
        
        // Arrange & Act
        var indexingPolicy = GetIndexingPolicy("audit-logs");
        
        // Assert
        Assert.NotNull(indexingPolicy);
        Assert.NotEmpty(indexingPolicy.CompositeIndexes);
    }

    #endregion

    #region Unique Key Tests

    [Fact]
    public void UsersContainer_Should_Have_UniqueKey_For_TenantId_UserName()
    {
        // Enforce uniqueness: one userName per tenant
        
        // Arrange & Act
        var uniqueKeys = GetUniqueKeyPolicy("users");
        
        // Assert
        Assert.NotNull(uniqueKeys);
        // Should have unique key on /userName within partition (tenantId)
        Assert.Contains(uniqueKeys.UniqueKeyPaths, k => k.Contains("/userName"));
    }

    [Fact]
    public void GroupsContainer_Should_Have_UniqueKey_For_TenantId_DisplayName()
    {
        // Enforce uniqueness: one displayName per tenant
        
        // Arrange & Act
        var uniqueKeys = GetUniqueKeyPolicy("groups");
        
        // Assert
        Assert.NotNull(uniqueKeys);
        // Should have unique key on /displayName within partition (tenantId)
        Assert.Contains(uniqueKeys.UniqueKeyPaths, k => k.Contains("/displayName"));
    }

    #endregion

    #region Helper Types

    private class ContainerConfiguration
    {
        public string? PartitionKeyPath { get; set; }
        public int? DefaultTimeToLive { get; set; }
        public string? TimeToLivePropertyPath { get; set; }
        public bool EnablePerItemTtl { get; set; }
    }

    private class IndexingPolicyConfiguration
    {
        public List<string> IncludedPaths { get; set; } = new();
        public List<string> ExcludedPaths { get; set; } = new();
        public List<List<string>> CompositeIndexes { get; set; } = new();
    }

    private class UniqueKeyPolicyConfiguration
    {
        public List<string> UniqueKeyPaths { get; set; } = new();
    }

    #endregion

    #region Helper Methods

    private static ContainerConfiguration? GetContainerConfiguration(string containerName)
    {
        // Use reflection to call the static method in CosmosDbSchema
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        var schemaType = coreAssembly?.GetType("SCIMGateway.Core.Data.CosmosDbSchema");
        if (schemaType == null) return null;

        var method = schemaType.GetMethod("GetContainerConfiguration", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null) return null;

        try
        {
            var result = method.Invoke(null, new object[] { containerName });
            if (result == null) return null;

            // Map the result to our local ContainerConfiguration type
            var resultType = result.GetType();
            return new ContainerConfiguration
            {
                PartitionKeyPath = resultType.GetProperty("PartitionKeyPath")?.GetValue(result) as string,
                DefaultTimeToLive = resultType.GetProperty("DefaultTimeToLive")?.GetValue(result) as int?,
                TimeToLivePropertyPath = resultType.GetProperty("TimeToLivePropertyPath")?.GetValue(result) as string,
                EnablePerItemTtl = (bool)(resultType.GetProperty("EnablePerItemTtl")?.GetValue(result) ?? false)
            };
        }
        catch
        {
            return null;
        }
    }

    private static IndexingPolicyConfiguration? GetIndexingPolicy(string containerName)
    {
        // Use reflection to call the static method in CosmosDbSchema
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        var schemaType = coreAssembly?.GetType("SCIMGateway.Core.Data.CosmosDbSchema");
        if (schemaType == null) return null;

        var method = schemaType.GetMethod("GetIndexingPolicy", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null) return null;

        try
        {
            var result = method.Invoke(null, new object[] { containerName });
            if (result == null) return null;

            // Map the result to our local IndexingPolicyConfiguration type
            var resultType = result.GetType();
            var includedPaths = resultType.GetProperty("IncludedPaths")?.GetValue(result) as IEnumerable<string>;
            var excludedPaths = resultType.GetProperty("ExcludedPaths")?.GetValue(result) as IEnumerable<string>;
            var compositeIndexes = resultType.GetProperty("CompositeIndexes")?.GetValue(result);

            var config = new IndexingPolicyConfiguration
            {
                IncludedPaths = includedPaths?.ToList() ?? new List<string>(),
                ExcludedPaths = excludedPaths?.ToList() ?? new List<string>()
            };

            // Handle composite indexes (List<List<string>>)
            if (compositeIndexes is System.Collections.IEnumerable compositeList)
            {
                foreach (var composite in compositeList)
                {
                    if (composite is IEnumerable<string> paths)
                    {
                        config.CompositeIndexes.Add(paths.ToList());
                    }
                }
            }

            return config;
        }
        catch
        {
            return null;
        }
    }

    private static UniqueKeyPolicyConfiguration? GetUniqueKeyPolicy(string containerName)
    {
        // Use reflection to call the static method in CosmosDbSchema
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        var schemaType = coreAssembly?.GetType("SCIMGateway.Core.Data.CosmosDbSchema");
        if (schemaType == null) return null;

        var method = schemaType.GetMethod("GetUniqueKeyPolicy", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null) return null;

        try
        {
            var result = method.Invoke(null, new object[] { containerName });
            if (result == null) return null;

            // Map the result to our local UniqueKeyPolicyConfiguration type
            var resultType = result.GetType();
            var uniqueKeyPaths = resultType.GetProperty("UniqueKeyPaths")?.GetValue(result) as IEnumerable<string>;

            return new UniqueKeyPolicyConfiguration
            {
                UniqueKeyPaths = uniqueKeyPaths?.ToList() ?? new List<string>()
            };
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
