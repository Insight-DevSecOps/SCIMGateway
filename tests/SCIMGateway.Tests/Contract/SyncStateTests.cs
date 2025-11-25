// ==========================================================================
// T021a: Contract Test for SyncState Model
// ==========================================================================
// Validates the SyncState model meets all requirements from:
// - FR-029-035: Bidirectional sync and change detection
// - tasks.md T021a specification
// 
// Required fields to validate:
// - tenantId, providerId, lastSyncTimestamp
// - syncDirection, lastKnownState
// - State transitions and drift/conflict logs
// ==========================================================================

using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for SyncState model.
/// These tests define the expected schema for tracking synchronization state
/// between Entra ID and SaaS providers.
/// </summary>
public class SyncStateTests
{
    #region Model Existence Tests

    [Fact]
    public void SyncState_Should_Exist_In_Core_Assembly()
    {
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
    }

    #endregion

    #region Required Fields Tests

    [Fact]
    public void SyncState_Should_Have_Id_Property()
    {
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var idProperty = syncStateType.GetProperty("Id");
        Assert.NotNull(idProperty);
    }

    [Fact]
    public void SyncState_Should_Have_TenantId_Property()
    {
        // Partition key for tenant isolation
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var tenantIdProperty = syncStateType.GetProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
    }

    [Fact]
    public void SyncState_Should_Have_ProviderId_Property()
    {
        // Which provider this state tracks
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var providerIdProperty = syncStateType.GetProperty("ProviderId");
        Assert.NotNull(providerIdProperty);
    }

    [Fact]
    public void SyncState_Should_Have_LastSyncTimestamp_Property()
    {
        // When last sync completed
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var lastSyncTimestampProperty = syncStateType.GetProperty("LastSyncTimestamp");
        Assert.NotNull(lastSyncTimestampProperty);
    }

    [Fact]
    public void SyncState_Should_Have_SyncDirection_Property()
    {
        // ENTRA_TO_SAAS or SAAS_TO_ENTRA
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var syncDirectionProperty = syncStateType.GetProperty("SyncDirection");
        Assert.NotNull(syncDirectionProperty);
    }

    [Fact]
    public void SyncState_Should_Have_LastKnownState_Property()
    {
        // Snapshot of last known provider state
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var lastKnownStateProperty = syncStateType.GetProperty("LastKnownState");
        Assert.NotNull(lastKnownStateProperty);
    }

    #endregion

    #region Optional Fields Tests

    [Fact]
    public void SyncState_Should_Have_DriftLog_Property()
    {
        // Log of detected drifts
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var driftLogProperty = syncStateType.GetProperty("DriftLog");
        Assert.NotNull(driftLogProperty);
    }

    [Fact]
    public void SyncState_Should_Have_ConflictLog_Property()
    {
        // Log of detected conflicts
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var conflictLogProperty = syncStateType.GetProperty("ConflictLog");
        Assert.NotNull(conflictLogProperty);
    }

    [Fact]
    public void SyncState_Should_Have_ErrorLog_Property()
    {
        // Log of sync errors
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var errorLogProperty = syncStateType.GetProperty("ErrorLog");
        Assert.NotNull(errorLogProperty);
    }

    [Fact]
    public void SyncState_Should_Have_SnapshotChecksum_Property()
    {
        // Checksum for detecting changes
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var snapshotChecksumProperty = syncStateType.GetProperty("SnapshotChecksum");
        Assert.NotNull(snapshotChecksumProperty);
    }

    [Fact]
    public void SyncState_Should_Have_UserCount_Property()
    {
        // Number of users in last snapshot
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var userCountProperty = syncStateType.GetProperty("UserCount");
        Assert.NotNull(userCountProperty);
    }

    [Fact]
    public void SyncState_Should_Have_GroupCount_Property()
    {
        // Number of groups in last snapshot
        
        // Arrange & Act
        var syncStateType = GetSyncStateType();
        
        // Assert
        Assert.NotNull(syncStateType);
        var groupCountProperty = syncStateType.GetProperty("GroupCount");
        Assert.NotNull(groupCountProperty);
    }

    #endregion

    #region SyncDirection Enum Tests

    [Fact]
    public void SyncDirection_Enum_Should_Exist()
    {
        // Arrange & Act
        var enumType = GetSyncDirectionEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.True(enumType.IsEnum);
    }

    [Fact]
    public void SyncDirection_Should_Have_EntraToSaas_Value()
    {
        // Arrange & Act
        var enumType = GetSyncDirectionEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Any(n => 
            n.Equals("EntraToSaas", StringComparison.OrdinalIgnoreCase) ||
            n.Equals("ENTRA_TO_SAAS", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void SyncDirection_Should_Have_SaasToEntra_Value()
    {
        // Arrange & Act
        var enumType = GetSyncDirectionEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Any(n => 
            n.Equals("SaasToEntra", StringComparison.OrdinalIgnoreCase) ||
            n.Equals("SAAS_TO_ENTRA", StringComparison.OrdinalIgnoreCase)));
    }

    #endregion

    #region SyncStatus Enum Tests

    [Fact]
    public void SyncStatus_Enum_Should_Exist()
    {
        // Arrange & Act
        var enumType = GetSyncStatusEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.True(enumType.IsEnum);
    }

    [Fact]
    public void SyncStatus_Should_Have_Idle_Value()
    {
        // Arrange & Act
        var enumType = GetSyncStatusEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("Idle", Enum.GetNames(enumType));
    }

    [Fact]
    public void SyncStatus_Should_Have_InProgress_Value()
    {
        // Arrange & Act
        var enumType = GetSyncStatusEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("InProgress", Enum.GetNames(enumType));
    }

    [Fact]
    public void SyncStatus_Should_Have_Completed_Value()
    {
        // Arrange & Act
        var enumType = GetSyncStatusEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("Completed", Enum.GetNames(enumType));
    }

    [Fact]
    public void SyncStatus_Should_Have_Failed_Value()
    {
        // Arrange & Act
        var enumType = GetSyncStatusEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("Failed", Enum.GetNames(enumType));
    }

    #endregion

    #region Helper Methods

    private static Type? GetSyncStateType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.SyncState")
            ?? coreAssembly?.GetType("SCIMGateway.Core.SyncEngine.SyncState");
    }

    private static Type? GetSyncDirectionEnumType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.SyncDirection")
            ?? coreAssembly?.GetType("SCIMGateway.Core.SyncEngine.SyncDirection");
    }

    private static Type? GetSyncStatusEnumType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.SyncStatus")
            ?? coreAssembly?.GetType("SCIMGateway.Core.SyncEngine.SyncStatus");
    }

    #endregion
}
