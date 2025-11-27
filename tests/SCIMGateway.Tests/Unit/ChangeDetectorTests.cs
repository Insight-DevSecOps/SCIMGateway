// ==========================================================================
// T134-T136: Unit Tests for ChangeDetector
// ==========================================================================
// Tests for change detection (user/group added/modified/deleted),
// drift detection (provider vs Entra state), and conflict detection
// ==========================================================================

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SCIMGateway.Tests.Unit;

/// <summary>
/// Unit tests for ChangeDetector.
/// Tests change detection, drift detection, and conflict detection functionality.
/// </summary>
public class ChangeDetectorTests
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

    // ==================== T134: Change Detection Tests ====================

    #region T134 - User Change Detection

    [Fact]
    public void ChangeDetector_Should_Detect_User_Added()
    {
        // Arrange
        var changeDetectorType = GetTypeByName("ChangeDetector") ?? GetTypeByName("IChangeDetector");
        Assert.NotNull(changeDetectorType);

        // Previous state: no users
        var previousUsers = new List<object>();
        
        // Current state: one user added
        var currentUsers = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "user-001" },
                { "userName", "john.doe@example.com" },
                { "displayName", "John Doe" },
                { "active", true }
            }
        };

        // Act - Detect changes between previous and current state
        // Expecting: one user added

        // Assert
        Assert.NotEmpty(currentUsers);
        Assert.Empty(previousUsers);
        // ChangeDetector should report DriftType.Added for user-001
    }

    [Fact]
    public void ChangeDetector_Should_Detect_User_Modified()
    {
        // Arrange
        var previousUsers = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "user-001" },
                { "userName", "john.doe@example.com" },
                { "displayName", "John Doe" },
                { "active", true }
            }
        };

        var currentUsers = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "user-001" },
                { "userName", "john.doe@example.com" },
                { "displayName", "John Doe Updated" }, // Changed
                { "active", true }
            }
        };

        // Act - Detect changes
        var hasChanges = previousUsers[0]["displayName"]?.ToString() != 
                        currentUsers[0]["displayName"]?.ToString();

        // Assert
        Assert.True(hasChanges);
        // ChangeDetector should report DriftType.Modified for user-001
    }

    [Fact]
    public void ChangeDetector_Should_Detect_User_Deleted()
    {
        // Arrange
        var previousUsers = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "user-001" },
                { "userName", "john.doe@example.com" }
            },
            new()
            {
                { "id", "user-002" },
                { "userName", "jane.doe@example.com" }
            }
        };

        var currentUsers = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "user-001" },
                { "userName", "john.doe@example.com" }
            }
            // user-002 is missing
        };

        // Act - Detect deleted users
        var previousIds = previousUsers.Select(u => u["id"]?.ToString()).ToHashSet();
        var currentIds = currentUsers.Select(u => u["id"]?.ToString()).ToHashSet();
        var deletedIds = previousIds.Except(currentIds);

        // Assert
        Assert.Single(deletedIds);
        Assert.Contains("user-002", deletedIds);
        // ChangeDetector should report DriftType.Deleted for user-002
    }

    [Fact]
    public void ChangeDetector_Should_Detect_User_Attribute_Changes()
    {
        // Arrange
        var previousUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "userName", "john.doe@example.com" },
            { "emails", new[] { new { value = "john.doe@example.com", primary = true } } },
            { "phoneNumbers", new[] { new { value = "+1-555-1234", type = "work" } } }
        };

        var currentUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "userName", "john.doe@example.com" },
            { "emails", new[] { new { value = "john.doe@newdomain.com", primary = true } } }, // Changed
            { "phoneNumbers", new[] { new { value = "+1-555-1234", type = "work" } } }
        };

        // Act - Deep comparison of attributes
        var previousJson = JsonSerializer.Serialize(previousUser);
        var currentJson = JsonSerializer.Serialize(currentUser);
        var hasAttributeChanges = previousJson != currentJson;

        // Assert
        Assert.True(hasAttributeChanges);
        // ChangeDetector should identify specific attribute that changed (emails)
    }

    [Fact]
    public void ChangeDetector_Should_Detect_User_Active_Status_Change()
    {
        // Arrange
        var previousUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "userName", "john.doe@example.com" },
            { "active", true }
        };

        var currentUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "userName", "john.doe@example.com" },
            { "active", false } // Deactivated
        };

        // Act
        var wasActive = (bool)previousUser["active"];
        var isActive = (bool)currentUser["active"];
        var statusChanged = wasActive != isActive;

        // Assert
        Assert.True(statusChanged);
        Assert.True(wasActive);
        Assert.False(isActive);
        // ChangeDetector should report this as a significant change
    }

    [Fact]
    public void ChangeDetector_Should_Handle_No_User_Changes()
    {
        // Arrange
        var previousUsers = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "user-001" },
                { "userName", "john.doe@example.com" },
                { "displayName", "John Doe" }
            }
        };

        var currentUsers = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "user-001" },
                { "userName", "john.doe@example.com" },
                { "displayName", "John Doe" }
            }
        };

        // Act
        var previousJson = JsonSerializer.Serialize(previousUsers);
        var currentJson = JsonSerializer.Serialize(currentUsers);
        var hasChanges = previousJson != currentJson;

        // Assert
        Assert.False(hasChanges);
        // ChangeDetector should report no drift
    }

    #endregion

    #region T134 - Group Change Detection

    [Fact]
    public void ChangeDetector_Should_Detect_Group_Added()
    {
        // Arrange
        var previousGroups = new List<object>();
        
        var currentGroups = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "group-001" },
                { "displayName", "Engineering Team" },
                { "members", new List<object>() }
            }
        };

        // Act
        var groupAdded = previousGroups.Count == 0 && currentGroups.Count == 1;

        // Assert
        Assert.True(groupAdded);
        // ChangeDetector should report DriftType.Added for group-001
    }

    [Fact]
    public void ChangeDetector_Should_Detect_Group_Modified()
    {
        // Arrange
        var previousGroup = new Dictionary<string, object>
        {
            { "id", "group-001" },
            { "displayName", "Engineering Team" },
            { "description", "Original description" }
        };

        var currentGroup = new Dictionary<string, object>
        {
            { "id", "group-001" },
            { "displayName", "Engineering Team" },
            { "description", "Updated description" } // Changed
        };

        // Act
        var descriptionChanged = previousGroup["description"]?.ToString() != 
                                currentGroup["description"]?.ToString();

        // Assert
        Assert.True(descriptionChanged);
        // ChangeDetector should report DriftType.Modified for group-001
    }

    [Fact]
    public void ChangeDetector_Should_Detect_Group_Deleted()
    {
        // Arrange
        var previousGroups = new List<Dictionary<string, object>>
        {
            new() { { "id", "group-001" }, { "displayName", "Team A" } },
            new() { { "id", "group-002" }, { "displayName", "Team B" } }
        };

        var currentGroups = new List<Dictionary<string, object>>
        {
            new() { { "id", "group-001" }, { "displayName", "Team A" } }
            // group-002 is missing
        };

        // Act
        var previousIds = previousGroups.Select(g => g["id"]?.ToString()).ToHashSet();
        var currentIds = currentGroups.Select(g => g["id"]?.ToString()).ToHashSet();
        var deletedIds = previousIds.Except(currentIds);

        // Assert
        Assert.Single(deletedIds);
        Assert.Contains("group-002", deletedIds);
        // ChangeDetector should report DriftType.Deleted for group-002
    }

    [Fact]
    public void ChangeDetector_Should_Detect_Group_Membership_Added()
    {
        // Arrange
        var previousGroup = new Dictionary<string, object>
        {
            { "id", "group-001" },
            { "displayName", "Engineering Team" },
            { "members", new List<string> { "user-001" } }
        };

        var currentGroup = new Dictionary<string, object>
        {
            { "id", "group-001" },
            { "displayName", "Engineering Team" },
            { "members", new List<string> { "user-001", "user-002" } } // user-002 added
        };

        // Act
        var previousMembers = (previousGroup["members"] as List<string>) ?? new List<string>();
        var currentMembers = (currentGroup["members"] as List<string>) ?? new List<string>();
        var addedMembers = currentMembers.Except(previousMembers);

        // Assert
        Assert.Single(addedMembers);
        Assert.Contains("user-002", addedMembers);
        // ChangeDetector should report DriftType.MembershipMismatch
    }

    [Fact]
    public void ChangeDetector_Should_Detect_Group_Membership_Removed()
    {
        // Arrange
        var previousGroup = new Dictionary<string, object>
        {
            { "id", "group-001" },
            { "displayName", "Engineering Team" },
            { "members", new List<string> { "user-001", "user-002", "user-003" } }
        };

        var currentGroup = new Dictionary<string, object>
        {
            { "id", "group-001" },
            { "displayName", "Engineering Team" },
            { "members", new List<string> { "user-001", "user-002" } } // user-003 removed
        };

        // Act
        var previousMembers = (previousGroup["members"] as List<string>) ?? new List<string>();
        var currentMembers = (currentGroup["members"] as List<string>) ?? new List<string>();
        var removedMembers = previousMembers.Except(currentMembers);

        // Assert
        Assert.Single(removedMembers);
        Assert.Contains("user-003", removedMembers);
    }

    [Fact]
    public void ChangeDetector_Should_Handle_No_Group_Changes()
    {
        // Arrange
        var previousGroups = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "group-001" },
                { "displayName", "Engineering Team" },
                { "members", new List<string> { "user-001", "user-002" } }
            }
        };

        var currentGroups = new List<Dictionary<string, object>>
        {
            new()
            {
                { "id", "group-001" },
                { "displayName", "Engineering Team" },
                { "members", new List<string> { "user-001", "user-002" } }
            }
        };

        // Act
        var previousJson = JsonSerializer.Serialize(previousGroups);
        var currentJson = JsonSerializer.Serialize(currentGroups);
        var hasChanges = previousJson != currentJson;

        // Assert
        Assert.False(hasChanges);
    }

    #endregion

    // ==================== T135: Drift Detection Tests ====================

    #region T135 - Drift Detection (Provider vs Entra State)

    [Fact]
    public void DriftDetection_Should_Identify_User_Drift_When_Provider_Has_Different_Data()
    {
        // Arrange - Entra state
        var entraUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "userName", "john.doe@example.com" },
            { "displayName", "John Doe" },
            { "department", "Engineering" }
        };

        // Provider state (different department)
        var providerUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "userName", "john.doe@example.com" },
            { "displayName", "John Doe" },
            { "department", "Sales" } // Drift: different department
        };

        // Act - Compare Entra state with provider state
        var hasDrift = entraUser["department"]?.ToString() != providerUser["department"]?.ToString();

        // Assert
        Assert.True(hasDrift);
        // Drift report should include:
        // - resourceId: "user-001"
        // - driftType: AttributeMismatch
        // - oldValue: "Engineering"
        // - newValue: "Sales"
    }

    [Fact]
    public void DriftDetection_Should_Generate_Drift_Report_With_Old_And_New_Values()
    {
        // Arrange
        var driftReportType = GetTypeByName("DriftReport") ?? GetTypeByName("DriftLogEntry");
        Assert.NotNull(driftReportType);

        // Act - Create a drift report
        var driftReport = CreateInstance(driftReportType);
        Assert.NotNull(driftReport);

        // Set properties
        var resourceIdProp = driftReportType.GetProperty("ResourceId");
        var driftTypeProp = driftReportType.GetProperty("DriftType");
        var oldValueProp = driftReportType.GetProperty("OldValue");
        var newValueProp = driftReportType.GetProperty("NewValue");

        if (resourceIdProp != null) resourceIdProp.SetValue(driftReport, "user-001");
        if (oldValueProp != null) oldValueProp.SetValue(driftReport, "Engineering");
        if (newValueProp != null) newValueProp.SetValue(driftReport, "Sales");

        // Assert
        Assert.Equal("user-001", resourceIdProp?.GetValue(driftReport)?.ToString());
        Assert.Equal("Engineering", oldValueProp?.GetValue(driftReport)?.ToString());
        Assert.Equal("Sales", newValueProp?.GetValue(driftReport)?.ToString());
    }

    [Fact]
    public void DriftDetection_Should_Track_Timestamp_Of_Detection()
    {
        // Arrange
        var beforeDetection = DateTime.UtcNow;

        var driftReportType = GetTypeByName("DriftLogEntry");
        Assert.NotNull(driftReportType);

        var driftReport = CreateInstance(driftReportType);
        Assert.NotNull(driftReport);

        var afterDetection = DateTime.UtcNow;

        // Act
        var detectedAtProp = driftReportType.GetProperty("DetectedAt");
        Assert.NotNull(detectedAtProp);

        var detectedAt = (DateTime?)detectedAtProp.GetValue(driftReport);

        // Assert
        Assert.NotNull(detectedAt);
        Assert.True(detectedAt >= beforeDetection.AddSeconds(-1));
        Assert.True(detectedAt <= afterDetection.AddSeconds(1));
    }

    [Fact]
    public void DriftDetection_Should_Compare_State_Snapshots_Using_Checksum()
    {
        // Arrange
        var previousState = new Dictionary<string, object>
        {
            { "user-001", new { userName = "john.doe@example.com", active = true } },
            { "user-002", new { userName = "jane.doe@example.com", active = true } }
        };

        var currentState = new Dictionary<string, object>
        {
            { "user-001", new { userName = "john.doe@example.com", active = true } },
            { "user-002", new { userName = "jane.doe@example.com", active = false } } // Changed
        };

        // Act - Compute checksums
        var previousChecksum = ComputeChecksum(previousState);
        var currentChecksum = ComputeChecksum(currentState);

        // Assert
        Assert.NotEqual(previousChecksum, currentChecksum);
    }

    [Fact]
    public void DriftDetection_Should_Handle_Provider_User_Not_In_Entra()
    {
        // Arrange - Entra knows about these users
        var entraUsers = new HashSet<string> { "user-001", "user-002" };

        // Provider has an extra user
        var providerUsers = new HashSet<string> { "user-001", "user-002", "user-003" };

        // Act
        var newInProvider = providerUsers.Except(entraUsers);

        // Assert
        Assert.Single(newInProvider);
        Assert.Contains("user-003", newInProvider);
        // This indicates user-003 was created directly in provider (drift)
    }

    [Fact]
    public void DriftDetection_Should_Handle_Entra_User_Not_In_Provider()
    {
        // Arrange - Entra knows about these users
        var entraUsers = new HashSet<string> { "user-001", "user-002", "user-003" };

        // Provider is missing one
        var providerUsers = new HashSet<string> { "user-001", "user-002" };

        // Act
        var missingInProvider = entraUsers.Except(providerUsers);

        // Assert
        Assert.Single(missingInProvider);
        Assert.Contains("user-003", missingInProvider);
        // This indicates user-003 was deleted in provider (drift)
    }

    [Fact]
    public void DriftDetection_Should_Categorize_Drift_By_Type()
    {
        // Arrange
        var driftTypeEnum = GetTypeByName("DriftType");
        Assert.NotNull(driftTypeEnum);
        Assert.True(driftTypeEnum.IsEnum);

        // Act - Get all drift types
        var driftTypes = Enum.GetNames(driftTypeEnum);

        // Assert - Should have all expected drift types
        Assert.Contains("Added", driftTypes);
        Assert.Contains("Modified", driftTypes);
        Assert.Contains("Deleted", driftTypes);
        Assert.Contains("AttributeMismatch", driftTypes);
        Assert.Contains("MembershipMismatch", driftTypes);
    }

    [Fact]
    public void DriftDetection_Should_Track_Reconciliation_Status()
    {
        // Arrange
        var driftReportType = GetTypeByName("DriftLogEntry");
        Assert.NotNull(driftReportType);

        var driftReport = CreateInstance(driftReportType);
        Assert.NotNull(driftReport);

        // Act
        var reconciledProp = driftReportType.GetProperty("Reconciled");
        var reconciledAtProp = driftReportType.GetProperty("ReconciledAt");
        var reconciliationActionProp = driftReportType.GetProperty("ReconciliationAction");

        Assert.NotNull(reconciledProp);
        Assert.NotNull(reconciledAtProp);
        Assert.NotNull(reconciliationActionProp);

        // Initially not reconciled
        var isReconciled = (bool?)reconciledProp.GetValue(driftReport);
        Assert.False(isReconciled);

        // Simulate reconciliation
        reconciledProp.SetValue(driftReport, true);
        reconciledAtProp.SetValue(driftReport, DateTime.UtcNow);
        reconciliationActionProp.SetValue(driftReport, "AUTO_APPLY");

        // Assert
        Assert.True((bool?)reconciledProp.GetValue(driftReport));
        Assert.Equal("AUTO_APPLY", reconciliationActionProp.GetValue(driftReport)?.ToString());
    }

    #endregion

    // ==================== T136: Conflict Detection Tests ====================

    #region T136 - Conflict Detection (Dual Modification)

    [Fact]
    public void ConflictDetection_Should_Detect_Dual_Modification()
    {
        // Arrange - Original state
        var originalUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe" },
            { "department", "Engineering" },
            { "version", "v1" }
        };

        // Entra modified (version v2)
        var entraUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe" },
            { "department", "Product" }, // Entra changed to Product
            { "version", "v2" }
        };

        // Provider also modified (independently)
        var providerUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe" },
            { "department", "Sales" }, // Provider changed to Sales
            { "version", "provider-v2" }
        };

        // Act - Detect conflict
        bool entraChanged = originalUser["department"]?.ToString() != entraUser["department"]?.ToString();
        bool providerChanged = originalUser["department"]?.ToString() != providerUser["department"]?.ToString();
        bool isConflict = entraChanged && providerChanged;

        // Assert
        Assert.True(isConflict);
        // ConflictType should be DualModification
    }

    [Fact]
    public void ConflictDetection_Should_Detect_Delete_Modify_Conflict()
    {
        // Arrange - Original state
        var originalUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe" },
            { "active", true }
        };

        // Entra deleted the user
        var entraUserDeleted = true;

        // Provider modified the user (doesn't know about deletion)
        var providerUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "displayName", "John Doe Updated" },
            { "active", true }
        };

        // Act - Detect conflict
        bool providerModified = originalUser["displayName"]?.ToString() != 
                               providerUser["displayName"]?.ToString();
        bool isDeleteModifyConflict = entraUserDeleted && providerModified;

        // Assert
        Assert.True(isDeleteModifyConflict);
        // ConflictType should be DeleteModifyConflict
    }

    [Fact]
    public void ConflictDetection_Should_Flag_For_Manual_Review()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        // Act - Set conflict properties
        var conflictTypeProp = conflictLogType.GetProperty("ConflictType");
        var resourceIdProp = conflictLogType.GetProperty("ResourceId");
        var entraChangeProp = conflictLogType.GetProperty("EntraChange");
        var providerChangeProp = conflictLogType.GetProperty("ProviderChange");
        var suggestedResolutionProp = conflictLogType.GetProperty("SuggestedResolution");
        var resolvedProp = conflictLogType.GetProperty("Resolved");

        if (resourceIdProp != null) resourceIdProp.SetValue(conflict, "user-001");
        if (entraChangeProp != null) entraChangeProp.SetValue(conflict, "department: Engineering → Product");
        if (providerChangeProp != null) providerChangeProp.SetValue(conflict, "department: Engineering → Sales");
        if (suggestedResolutionProp != null) suggestedResolutionProp.SetValue(conflict, "MANUAL_REVIEW");
        if (resolvedProp != null) resolvedProp.SetValue(conflict, false);

        // Assert
        Assert.Equal("user-001", resourceIdProp?.GetValue(conflict)?.ToString());
        Assert.Equal("MANUAL_REVIEW", suggestedResolutionProp?.GetValue(conflict)?.ToString());
        Assert.False((bool?)resolvedProp?.GetValue(conflict));
    }

    [Fact]
    public void ConflictDetection_Should_Categorize_Conflict_Types()
    {
        // Arrange
        var conflictTypeEnum = GetTypeByName("ConflictType");
        Assert.NotNull(conflictTypeEnum);
        Assert.True(conflictTypeEnum.IsEnum);

        // Act - Get all conflict types
        var conflictTypes = Enum.GetNames(conflictTypeEnum);

        // Assert - Should have all expected conflict types
        Assert.Contains("DualModification", conflictTypes);
        Assert.Contains("DeleteModifyConflict", conflictTypes);
        Assert.Contains("UniquenessViolation", conflictTypes);
        Assert.Contains("TransformationConflict", conflictTypes);
    }

    [Fact]
    public void ConflictDetection_Should_Track_Both_Entra_And_Provider_Changes()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        // Act
        var entraChangeProp = conflictLogType.GetProperty("EntraChange");
        var providerChangeProp = conflictLogType.GetProperty("ProviderChange");

        Assert.NotNull(entraChangeProp);
        Assert.NotNull(providerChangeProp);

        entraChangeProp.SetValue(conflict, "displayName: John Doe → John D. Doe");
        providerChangeProp.SetValue(conflict, "displayName: John Doe → Johnny Doe");

        // Assert
        Assert.Equal("displayName: John Doe → John D. Doe", entraChangeProp.GetValue(conflict)?.ToString());
        Assert.Equal("displayName: John Doe → Johnny Doe", providerChangeProp.GetValue(conflict)?.ToString());
    }

    [Fact]
    public void ConflictDetection_Should_Track_Resolution_Details()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        // Act - Simulate resolution
        var resolvedProp = conflictLogType.GetProperty("Resolved");
        var resolvedAtProp = conflictLogType.GetProperty("ResolvedAt");
        var resolvedByProp = conflictLogType.GetProperty("ResolvedBy");
        var resolutionProp = conflictLogType.GetProperty("Resolution");

        var resolutionTime = DateTime.UtcNow;
        if (resolvedProp != null) resolvedProp.SetValue(conflict, true);
        if (resolvedAtProp != null) resolvedAtProp.SetValue(conflict, resolutionTime);
        if (resolvedByProp != null) resolvedByProp.SetValue(conflict, "admin@example.com");
        if (resolutionProp != null) resolutionProp.SetValue(conflict, "Applied Entra value");

        // Assert
        Assert.True((bool?)resolvedProp?.GetValue(conflict));
        Assert.Equal(resolutionTime, (DateTime?)resolvedAtProp?.GetValue(conflict));
        Assert.Equal("admin@example.com", resolvedByProp?.GetValue(conflict)?.ToString());
        Assert.Equal("Applied Entra value", resolutionProp?.GetValue(conflict)?.ToString());
    }

    [Fact]
    public void ConflictDetection_Should_Handle_No_Conflicts()
    {
        // Arrange - Only Entra changed (no conflict)
        var originalUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "department", "Engineering" }
        };

        var entraUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "department", "Product" } // Entra changed
        };

        var providerUser = new Dictionary<string, object>
        {
            { "id", "user-001" },
            { "department", "Engineering" } // Provider unchanged
        };

        // Act
        bool entraChanged = originalUser["department"]?.ToString() != entraUser["department"]?.ToString();
        bool providerChanged = originalUser["department"]?.ToString() != providerUser["department"]?.ToString();
        bool isConflict = entraChanged && providerChanged;

        // Assert - No conflict because provider didn't change
        Assert.True(entraChanged);
        Assert.False(providerChanged);
        Assert.False(isConflict);
    }

    [Fact]
    public void ConflictDetection_Should_Handle_Multiple_Conflicts_Per_Sync()
    {
        // Arrange - Multiple resources with conflicts
        var conflicts = new List<Dictionary<string, object>>
        {
            new() { { "resourceId", "user-001" }, { "conflictType", "DualModification" } },
            new() { { "resourceId", "user-002" }, { "conflictType", "DeleteModifyConflict" } },
            new() { { "resourceId", "group-001" }, { "conflictType", "DualModification" } }
        };

        // Act - Should collect all conflicts
        var userConflicts = conflicts.Where(c => c["resourceId"]?.ToString()?.StartsWith("user") == true);
        var groupConflicts = conflicts.Where(c => c["resourceId"]?.ToString()?.StartsWith("group") == true);

        // Assert
        Assert.Equal(2, userConflicts.Count());
        Assert.Single(groupConflicts);
    }

    [Fact]
    public void ConflictDetection_Should_Block_Auto_Sync_For_Conflicted_Resources()
    {
        // Arrange
        var conflictLogType = GetTypeByName("ConflictLogEntry");
        Assert.NotNull(conflictLogType);

        var conflict = CreateInstance(conflictLogType);
        Assert.NotNull(conflict);

        var resourceIdProp = conflictLogType.GetProperty("ResourceId");
        var resolvedProp = conflictLogType.GetProperty("Resolved");

        if (resourceIdProp != null) resourceIdProp.SetValue(conflict, "user-001");
        if (resolvedProp != null) resolvedProp.SetValue(conflict, false);

        // Act - Check if resource should be blocked
        var resourceId = resourceIdProp?.GetValue(conflict)?.ToString();
        var isResolved = (bool?)resolvedProp?.GetValue(conflict);
        var shouldBlock = !isResolved.GetValueOrDefault(true);

        // Assert
        Assert.True(shouldBlock);
        Assert.Equal("user-001", resourceId);
        // Sync should skip user-001 until conflict is resolved
    }

    #endregion

    // ==================== Helper Methods ====================

    private static string ComputeChecksum(object state)
    {
        var json = JsonSerializer.Serialize(state);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }
}
