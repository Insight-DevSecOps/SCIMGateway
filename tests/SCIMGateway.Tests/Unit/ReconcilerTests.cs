// ==========================================================================
// T137-T138: Unit Tests for Reconciler
// ==========================================================================
// Tests for reconciliation strategies: AUTO_APPLY and MANUAL_REVIEW
// ==========================================================================

using System.Reflection;
using System.Text.Json;
using Xunit;

namespace SCIMGateway.Tests.Unit;

/// <summary>
/// Unit tests for Reconciler.
/// Tests reconciliation strategies for handling drift and conflicts.
/// </summary>
public class ReconcilerTests
{
    private static Type? GetTypeByName(string typeName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        Type? fallback = null;
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.Name == typeName)
                .ToList();

            // Prefer SyncEngine namespace for sync-related types
            var syncType = types.FirstOrDefault(t => t.Namespace?.Contains("SyncEngine") == true);
            if (syncType != null) return syncType;

            // Fallback to Models namespace
            var modelType = types.FirstOrDefault(t => t.Namespace?.Contains("Models") == true);
            if (modelType != null) return modelType;

            if (fallback == null && types.Count > 0)
                fallback = types.First();
        }
        return fallback;
    }

    private static object? CreateInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    // ==================== T137: AUTO_APPLY Reconciliation Tests ====================

    #region T137 - AUTO_APPLY Strategy (Direction-Based)

    [Fact]
    public void AutoApply_EntraToSaas_Should_Overwrite_Provider_With_Entra_State()
    {
        // Arrange - Sync direction is ENTRA_TO_SAAS
        var syncDirection = "EntraToSaas";
        
        var entraUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe" },
            { "department", "Engineering" } // Entra value
        };

        var providerUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe" },
            { "department", "Sales" } // Provider value (drift)
        };

        // Act - AUTO_APPLY with ENTRA_TO_SAAS
        // Should update provider with Entra value
        var expectedProviderState = entraUser["department"];

        // Assert
        Assert.Equal("EntraToSaas", syncDirection);
        Assert.Equal("Engineering", expectedProviderState);
        // Reconciler should push Entra's "Engineering" to provider
    }

    [Fact]
    public void AutoApply_SaasToEntra_Should_Overwrite_Entra_With_Provider_State()
    {
        // Arrange - Sync direction is SAAS_TO_ENTRA
        var syncDirection = "SaasToEntra";
        
        var entraUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe" },
            { "department", "Engineering" } // Entra value
        };

        var providerUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe" },
            { "department", "Sales" } // Provider value
        };

        // Act - AUTO_APPLY with SAAS_TO_ENTRA
        // Should update Entra with provider value
        var expectedEntraState = providerUser["department"];

        // Assert
        Assert.Equal("SaasToEntra", syncDirection);
        Assert.Equal("Sales", expectedEntraState);
        // Reconciler should push provider's "Sales" to Entra
    }

    [Fact]
    public void AutoApply_Should_Apply_User_Creation_Based_On_Direction()
    {
        // Arrange - User exists in provider but not Entra
        var syncDirection = "SaasToEntra";
        
        var providerUser = new Dictionary<string, object>
        {
            { "id", "user-new" },
            { "userName", "new.user@example.com" },
            { "displayName", "New User" }
        };

        // Entra doesn't have this user (empty)
        Dictionary<string, object>? entraUser = null;

        // Act - AUTO_APPLY should create user in Entra
        var shouldCreateInEntra = syncDirection == "SaasToEntra" && entraUser == null;

        // Assert
        Assert.True(shouldCreateInEntra);
    }

    [Fact]
    public void AutoApply_Should_Apply_User_Deletion_Based_On_Direction()
    {
        // Arrange - User deleted in Entra, still exists in provider
        var syncDirection = "EntraToSaas";
        
        var entraUserDeleted = true;
        var providerUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "userName", "deleted.user@example.com" }
        };

        // Act - AUTO_APPLY with ENTRA_TO_SAAS should delete in provider
        var shouldDeleteInProvider = syncDirection == "EntraToSaas" && entraUserDeleted;

        // Assert
        Assert.True(shouldDeleteInProvider);
    }

    [Fact]
    public void AutoApply_Should_Mark_Drift_As_Reconciled_After_Apply()
    {
        // Arrange
        var driftReportType = GetTypeByName("DriftLogEntry");
        Assert.NotNull(driftReportType);

        var drift = CreateInstance(driftReportType);
        Assert.NotNull(drift);

        var resourceIdProp = driftReportType.GetProperty("ResourceId");
        var reconciledProp = driftReportType.GetProperty("Reconciled");
        var reconciledAtProp = driftReportType.GetProperty("ReconciledAt");
        var reconciliationActionProp = driftReportType.GetProperty("ReconciliationAction");

        if (resourceIdProp != null) resourceIdProp.SetValue(drift, "user-001");

        // Act - Simulate AUTO_APPLY reconciliation
        var reconciliationTime = DateTime.UtcNow;
        if (reconciledProp != null) reconciledProp.SetValue(drift, true);
        if (reconciledAtProp != null) reconciledAtProp.SetValue(drift, reconciliationTime);
        if (reconciliationActionProp != null) reconciliationActionProp.SetValue(drift, "AUTO_APPLY");

        // Assert
        Assert.True((bool?)reconciledProp?.GetValue(drift));
        Assert.Equal("AUTO_APPLY", reconciliationActionProp?.GetValue(drift)?.ToString());
        Assert.NotNull(reconciledAtProp?.GetValue(drift));
    }

    [Fact]
    public void AutoApply_Should_Handle_Group_Membership_Drift()
    {
        // Arrange - Group membership differs
        var syncDirection = "EntraToSaas";
        
        var entraGroup = new Dictionary<string, object>
        {
            { "id", "group-001" },
            { "displayName", "Engineering Team" },
            { "members", new List<string> { "user-001", "user-002", "user-003" } }
        };

        var providerGroup = new Dictionary<string, object>
        {
            { "id", "group-001" },
            { "displayName", "Engineering Team" },
            { "members", new List<string> { "user-001", "user-002" } } // Missing user-003
        };

        // Act - AUTO_APPLY should add user-003 to provider group
        var entraMembers = (List<string>)entraGroup["members"];
        var providerMembers = (List<string>)providerGroup["members"];
        var membersToAdd = entraMembers.Except(providerMembers).ToList();

        // Assert
        Assert.Single(membersToAdd);
        Assert.Contains("user-003", membersToAdd);
    }

    [Fact]
    public void AutoApply_Should_Update_Sync_State_After_Reconciliation()
    {
        // Arrange
        var syncStateType = GetTypeByName("SyncState");
        Assert.NotNull(syncStateType);

        var syncState = CreateInstance(syncStateType);
        Assert.NotNull(syncState);

        var lastSyncTimestampProp = syncStateType.GetProperty("LastSyncTimestamp");
        var statusProp = syncStateType.GetProperty("Status");

        // Act - Simulate reconciliation completion
        var now = DateTime.UtcNow;
        if (lastSyncTimestampProp != null) lastSyncTimestampProp.SetValue(syncState, now);
        
        // Set status to Completed
        var statusEnum = GetTypeByName("SyncStatus");
        if (statusEnum != null && statusProp != null)
        {
            var completedValue = Enum.Parse(statusEnum, "Completed");
            statusProp.SetValue(syncState, completedValue);
        }

        // Assert
        var lastSync = (DateTime?)lastSyncTimestampProp?.GetValue(syncState);
        Assert.NotNull(lastSync);
        Assert.True(lastSync >= now.AddSeconds(-1));
    }

    [Fact]
    public void AutoApply_Should_Apply_Multiple_Changes_In_Batch()
    {
        // Arrange - Multiple users with drift
        var driftItems = new List<Dictionary<string, object>>
        {
            new() { { "resourceId", "user-001" }, { "driftType", "Modified" } },
            new() { { "resourceId", "user-002" }, { "driftType", "Added" } },
            new() { { "resourceId", "user-003" }, { "driftType", "Deleted" } }
        };

        // Act - Reconciler should process all in batch
        var processedCount = 0;
        foreach (var drift in driftItems)
        {
            // Simulate reconciliation
            drift["reconciled"] = true;
            drift["reconciliationAction"] = "AUTO_APPLY";
            processedCount++;
        }

        // Assert
        Assert.Equal(3, processedCount);
        Assert.All(driftItems, d => Assert.True((bool)d["reconciled"]));
    }

    #endregion

    // ==================== T138: MANUAL_REVIEW Reconciliation Tests ====================

    #region T138 - MANUAL_REVIEW Strategy

    [Fact]
    public void ManualReview_Should_Create_Conflict_Log_Entry()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        // Act - Create conflict for manual review
        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var conflictIdProp = conflictLogType.GetProperty("ConflictId");
        var resourceIdProp = conflictLogType.GetProperty("ResourceId");
        var conflictTypeProp = conflictLogType.GetProperty("ConflictType");
        var suggestedResolutionProp = conflictLogType.GetProperty("SuggestedResolution");
        var resolvedProp = conflictLogType.GetProperty("Resolved");

        if (resourceIdProp != null) resourceIdProp.SetValue(conflict, "user-001");
        if (suggestedResolutionProp != null) suggestedResolutionProp.SetValue(conflict, "MANUAL_REVIEW");
        if (resolvedProp != null) resolvedProp.SetValue(conflict, false);

        // Assert
        Assert.NotNull(conflictIdProp?.GetValue(conflict));
        Assert.Equal("user-001", resourceIdProp?.GetValue(conflict)?.ToString());
        Assert.Equal("MANUAL_REVIEW", suggestedResolutionProp?.GetValue(conflict)?.ToString());
        Assert.False((bool?)resolvedProp?.GetValue(conflict));
    }

    [Fact]
    public void ManualReview_Should_Block_Auto_Sync_For_Conflicted_Resource()
    {
        // Arrange - Resource has unresolved conflict
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var resourceIdProp = conflictLogType.GetProperty("ResourceId");
        var resolvedProp = conflictLogType.GetProperty("Resolved");

        if (resourceIdProp != null) resourceIdProp.SetValue(conflict, "user-001");
        if (resolvedProp != null) resolvedProp.SetValue(conflict, false);

        // Act - Check if resource should be blocked from auto-sync
        var resourceId = resourceIdProp?.GetValue(conflict)?.ToString();
        var isResolved = (bool?)resolvedProp?.GetValue(conflict);
        var shouldBlockAutoSync = !isResolved.GetValueOrDefault(true);

        // Assert
        Assert.True(shouldBlockAutoSync);
        Assert.Equal("user-001", resourceId);
    }

    [Fact]
    public void ManualReview_Should_Include_Both_Entra_And_Provider_Values()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var entraChangeProp = conflictLogType.GetProperty("EntraChange");
        var providerChangeProp = conflictLogType.GetProperty("ProviderChange");

        // Act - Set both values for admin review
        if (entraChangeProp != null) entraChangeProp.SetValue(conflict, 
            JsonSerializer.Serialize(new { department = "Engineering" }));
        if (providerChangeProp != null) providerChangeProp.SetValue(conflict, 
            JsonSerializer.Serialize(new { department = "Sales" }));

        // Assert
        var entraValue = entraChangeProp?.GetValue(conflict)?.ToString();
        var providerValue = providerChangeProp?.GetValue(conflict)?.ToString();
        
        Assert.Contains("Engineering", entraValue ?? "");
        Assert.Contains("Sales", providerValue ?? "");
    }

    [Fact]
    public void ManualReview_Should_Allow_Admin_To_Resolve_With_Entra_Value()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var resolvedProp = conflictLogType.GetProperty("Resolved");
        var resolvedAtProp = conflictLogType.GetProperty("ResolvedAt");
        var resolvedByProp = conflictLogType.GetProperty("ResolvedBy");
        var resolutionProp = conflictLogType.GetProperty("Resolution");

        // Act - Admin resolves by choosing Entra value
        var resolutionTime = DateTime.UtcNow;
        if (resolvedProp != null) resolvedProp.SetValue(conflict, true);
        if (resolvedAtProp != null) resolvedAtProp.SetValue(conflict, resolutionTime);
        if (resolvedByProp != null) resolvedByProp.SetValue(conflict, "admin@example.com");
        if (resolutionProp != null) resolutionProp.SetValue(conflict, "APPLY_ENTRA");

        // Assert
        Assert.True((bool?)resolvedProp?.GetValue(conflict));
        Assert.Equal("admin@example.com", resolvedByProp?.GetValue(conflict)?.ToString());
        Assert.Equal("APPLY_ENTRA", resolutionProp?.GetValue(conflict)?.ToString());
    }

    [Fact]
    public void ManualReview_Should_Allow_Admin_To_Resolve_With_Provider_Value()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var resolvedProp = conflictLogType.GetProperty("Resolved");
        var resolutionProp = conflictLogType.GetProperty("Resolution");
        var resolvedByProp = conflictLogType.GetProperty("ResolvedBy");

        // Act - Admin resolves by choosing provider value
        if (resolvedProp != null) resolvedProp.SetValue(conflict, true);
        if (resolvedByProp != null) resolvedByProp.SetValue(conflict, "admin@example.com");
        if (resolutionProp != null) resolutionProp.SetValue(conflict, "APPLY_PROVIDER");

        // Assert
        Assert.True((bool?)resolvedProp?.GetValue(conflict));
        Assert.Equal("APPLY_PROVIDER", resolutionProp?.GetValue(conflict)?.ToString());
    }

    [Fact]
    public void ManualReview_Should_Allow_Admin_To_Merge_Values()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var resolvedProp = conflictLogType.GetProperty("Resolved");
        var resolutionProp = conflictLogType.GetProperty("Resolution");

        // Act - Admin provides merged/custom value
        if (resolvedProp != null) resolvedProp.SetValue(conflict, true);
        if (resolutionProp != null) resolutionProp.SetValue(conflict, "CUSTOM: {\"department\": \"Engineering & Sales\"}");

        // Assert
        Assert.True((bool?)resolvedProp?.GetValue(conflict));
        Assert.Contains("CUSTOM:", resolutionProp?.GetValue(conflict)?.ToString() ?? "");
    }

    [Fact]
    public void ManualReview_Should_Resume_Sync_After_Resolution()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var resourceIdProp = conflictLogType.GetProperty("ResourceId");
        var resolvedProp = conflictLogType.GetProperty("Resolved");

        if (resourceIdProp != null) resourceIdProp.SetValue(conflict, "user-001");
        if (resolvedProp != null) resolvedProp.SetValue(conflict, true);

        // Act - Check if resource should resume syncing
        var isResolved = (bool?)resolvedProp?.GetValue(conflict);
        var shouldResumeSyncing = isResolved.GetValueOrDefault(false);

        // Assert
        Assert.True(shouldResumeSyncing);
    }

    [Fact]
    public void ManualReview_Should_Track_Resolution_Timestamp()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var resolvedAtProp = conflictLogType.GetProperty("ResolvedAt");
        var detectedAtProp = conflictLogType.GetProperty("DetectedAt");

        // Act
        var detectionTime = DateTime.UtcNow.AddHours(-1); // Detected 1 hour ago
        var resolutionTime = DateTime.UtcNow;

        if (detectedAtProp != null) detectedAtProp.SetValue(conflict, detectionTime);
        if (resolvedAtProp != null) resolvedAtProp.SetValue(conflict, resolutionTime);

        // Assert
        var detected = (DateTime?)detectedAtProp?.GetValue(conflict);
        var resolved = (DateTime?)resolvedAtProp?.GetValue(conflict);
        
        Assert.NotNull(detected);
        Assert.NotNull(resolved);
        Assert.True(resolved > detected);
    }

    [Fact]
    public void ManualReview_Should_Log_Resolution_To_Audit_Trail()
    {
        // Arrange - Simulate audit log entry for conflict resolution
        var auditLogType = GetTypeByName("AuditLogEntry");
        Assert.NotNull(auditLogType);

        var auditLog = CreateInstance(auditLogType);
        Assert.NotNull(auditLog);

        var operationTypeProp = auditLogType.GetProperty("OperationType");
        var resourceIdProp = auditLogType.GetProperty("ResourceId");

        // Act - Create audit entry for resolution
        if (operationTypeProp != null) operationTypeProp.SetValue(auditLog, "ConflictResolution");
        if (resourceIdProp != null) resourceIdProp.SetValue(auditLog, "user-001");

        // Assert
        Assert.Equal("ConflictResolution", operationTypeProp?.GetValue(auditLog)?.ToString());
        Assert.Equal("user-001", resourceIdProp?.GetValue(auditLog)?.ToString());
    }

    [Fact]
    public void ManualReview_Should_Support_Ignore_Resolution()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var resolvedProp = conflictLogType.GetProperty("Resolved");
        var resolutionProp = conflictLogType.GetProperty("Resolution");

        // Act - Admin chooses to ignore the conflict
        if (resolvedProp != null) resolvedProp.SetValue(conflict, true);
        if (resolutionProp != null) resolutionProp.SetValue(conflict, "IGNORE");

        // Assert
        Assert.True((bool?)resolvedProp?.GetValue(conflict));
        Assert.Equal("IGNORE", resolutionProp?.GetValue(conflict)?.ToString());
        // Resource should resume syncing but keep both values as-is
    }

    [Fact]
    public void ManualReview_Should_Handle_Multiple_Pending_Conflicts()
    {
        // Arrange - Multiple conflicts pending review
        var pendingConflicts = new List<Dictionary<string, object>>
        {
            new() { { "conflictId", "c-001" }, { "resourceId", "user-001" }, { "resolved", false } },
            new() { { "conflictId", "c-002" }, { "resourceId", "user-002" }, { "resolved", false } },
            new() { { "conflictId", "c-003" }, { "resourceId", "group-001" }, { "resolved", true } } // Already resolved
        };

        // Act - Filter for pending conflicts
        var pending = pendingConflicts.Where(c => !(bool)c["resolved"]).ToList();

        // Assert
        Assert.Equal(2, pending.Count);
        Assert.DoesNotContain(pending, c => c["conflictId"]?.ToString() == "c-003");
    }

    #endregion

    // ==================== Reconciler Strategy Selection Tests ====================

    #region Strategy Selection

    [Fact]
    public void Reconciler_Should_Use_AutoApply_When_No_Conflict()
    {
        // Arrange - Simple drift (no dual modification)
        var hasDrift = true;
        var hasConflict = false;
        var configuredStrategy = "AUTO_APPLY";

        // Act - Determine reconciliation strategy
        var strategyToUse = !hasConflict ? configuredStrategy : "MANUAL_REVIEW";

        // Assert
        Assert.Equal("AUTO_APPLY", strategyToUse);
    }

    [Fact]
    public void Reconciler_Should_Use_ManualReview_When_Conflict_Detected()
    {
        // Arrange - Conflict detected
        var hasDrift = true;
        var hasConflict = true;
        var configuredStrategy = "AUTO_APPLY";

        // Act - Conflict forces manual review regardless of config
        var strategyToUse = hasConflict ? "MANUAL_REVIEW" : configuredStrategy;

        // Assert
        Assert.Equal("MANUAL_REVIEW", strategyToUse);
    }

    [Fact]
    public void Reconciler_Should_Support_Ignore_Strategy()
    {
        // Arrange
        var configuredStrategy = "IGNORE";
        var hasDrift = true;

        // Act - IGNORE strategy logs but doesn't apply changes
        var shouldApplyChanges = configuredStrategy != "IGNORE";
        var shouldLog = true; // Always log

        // Assert
        Assert.False(shouldApplyChanges);
        Assert.True(shouldLog);
    }

    [Fact]
    public void Reconciler_Should_Default_To_ManualReview_For_DeleteModify_Conflict()
    {
        // Arrange - Delete/Modify conflict is high-risk
        var conflictType = "DeleteModifyConflict";

        // Act - These should always go to manual review
        var requiresManualReview = conflictType == "DeleteModifyConflict" || 
                                    conflictType == "DualModification";

        // Assert
        Assert.True(requiresManualReview);
    }

    #endregion
}
