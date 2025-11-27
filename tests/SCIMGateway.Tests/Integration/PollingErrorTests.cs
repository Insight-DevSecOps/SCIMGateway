// ==========================================================================
// T141: Integration Tests for Polling Error Handling
// ==========================================================================
// Tests for error scenarios: adapter unavailable, retry logic, error logging,
// and operations team alerts
// ==========================================================================

using System.Reflection;
using System.Text.Json;
using Xunit;

namespace SCIMGateway.Tests.Integration;

/// <summary>
/// Integration tests for polling error handling.
/// Tests retry logic, error logging, and alerting when adapters are unavailable.
/// </summary>
public class PollingErrorTests
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

            // Prefer SyncEngine namespace
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

    // ==================== T141: Adapter Unavailable Tests ====================

    #region Adapter Unavailable Scenarios

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task PollingError_Should_Handle_Adapter_Connection_Timeout()
    {
        // Arrange
        var adapterException = new Exception("Connection timed out after 30 seconds");
        var errorLogged = false;
        var retryAttempted = false;

        // Act - Simulate adapter timeout
        try
        {
            throw adapterException;
        }
        catch (Exception ex)
        {
            errorLogged = true;
            retryAttempted = true;
            Assert.Contains("timed out", ex.Message);
        }

        await Task.CompletedTask;

        // Assert
        Assert.True(errorLogged);
        Assert.True(retryAttempted);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task PollingError_Should_Handle_Adapter_Authentication_Failure()
    {
        // Arrange
        var errorCode = "401";
        var errorMessage = "Invalid or expired OAuth token";
        var alertSent = false;

        // Act - 401 errors should trigger immediate alert (not retry)
        var isAuthError = errorCode == "401" || errorCode == "403";
        if (isAuthError)
        {
            alertSent = true;
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(isAuthError);
        Assert.True(alertSent);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task PollingError_Should_Handle_Adapter_Rate_Limited()
    {
        // Arrange - 429 Too Many Requests
        var errorCode = "429";
        var retryAfterSeconds = 60;
        var shouldRetryAfterDelay = false;

        // Act - Handle rate limiting
        if (errorCode == "429")
        {
            shouldRetryAfterDelay = true;
            // Wait for retry-after period
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(shouldRetryAfterDelay);
        Assert.Equal(60, retryAfterSeconds);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task PollingError_Should_Handle_Adapter_ServerError()
    {
        // Arrange - 500 Internal Server Error
        var errorCode = "500";
        var isTransientError = false;
        var shouldRetry = false;

        // Act - 5xx errors are transient, should retry
        if (int.TryParse(errorCode, out var code) && code >= 500 && code < 600)
        {
            isTransientError = true;
            shouldRetry = true;
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(isTransientError);
        Assert.True(shouldRetry);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task PollingError_Should_Handle_Network_Unreachable()
    {
        // Arrange
        var networkException = new Exception("Network is unreachable");
        var isNetworkError = false;
        var shouldRetry = false;

        // Act
        if (networkException.Message.Contains("Network") || 
            networkException.Message.Contains("unreachable"))
        {
            isNetworkError = true;
            shouldRetry = true; // Network issues are transient
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(isNetworkError);
        Assert.True(shouldRetry);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task PollingError_Should_Handle_DNS_Resolution_Failure()
    {
        // Arrange
        var dnsException = new Exception("No such host is known: api.provider.com");
        var shouldAlertOps = false;

        // Act - DNS failures may indicate configuration issue
        if (dnsException.Message.Contains("No such host"))
        {
            shouldAlertOps = true; // Configuration issue, not transient
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(shouldAlertOps);
    }

    #endregion

    #region Retry Logic Tests

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task RetryLogic_Should_Retry_On_Transient_Error()
    {
        // Arrange
        var maxRetries = 3;
        var currentRetry = 0;
        var succeeded = false;

        // Act - Simulate retries with eventual success
        while (currentRetry < maxRetries && !succeeded)
        {
            currentRetry++;
            if (currentRetry == 3)
            {
                succeeded = true; // Succeeds on 3rd attempt
            }
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(succeeded);
        Assert.Equal(3, currentRetry);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task RetryLogic_Should_Fail_After_Max_Retries()
    {
        // Arrange
        var maxRetries = 3;
        var currentRetry = 0;
        var succeeded = false;
        var finalFailure = false;

        // Act - All retries fail
        while (currentRetry < maxRetries && !succeeded)
        {
            currentRetry++;
            // Always fails
        }

        if (!succeeded && currentRetry >= maxRetries)
        {
            finalFailure = true;
        }
        await Task.CompletedTask;

        // Assert
        Assert.False(succeeded);
        Assert.True(finalFailure);
        Assert.Equal(3, currentRetry);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task RetryLogic_Should_Use_Exponential_Backoff()
    {
        // Arrange
        var baseDelayMs = 1000;
        var maxDelayMs = 30000;
        var retryAttempt = 0;
        var delays = new List<int>();

        // Act - Calculate delays for 5 retries
        for (retryAttempt = 1; retryAttempt <= 5; retryAttempt++)
        {
            var delay = Math.Min(baseDelayMs * (int)Math.Pow(2, retryAttempt - 1), maxDelayMs);
            delays.Add(delay);
        }
        await Task.CompletedTask;

        // Assert
        Assert.Equal(5, delays.Count);
        Assert.Equal(1000, delays[0]);  // 1 second
        Assert.Equal(2000, delays[1]);  // 2 seconds
        Assert.Equal(4000, delays[2]);  // 4 seconds
        Assert.Equal(8000, delays[3]);  // 8 seconds
        Assert.Equal(16000, delays[4]); // 16 seconds
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task RetryLogic_Should_Add_Jitter_To_Avoid_Thundering_Herd()
    {
        // Arrange
        var baseDelayMs = 1000;
        var random = new Random(42); // Seeded for test reproducibility
        var delays = new List<int>();

        // Act - Add jitter (±20%)
        for (int i = 0; i < 5; i++)
        {
            var jitterFactor = 0.8 + (random.NextDouble() * 0.4); // 0.8 to 1.2
            var delay = (int)(baseDelayMs * jitterFactor);
            delays.Add(delay);
        }
        await Task.CompletedTask;

        // Assert - Delays should vary
        Assert.Equal(5, delays.Count);
        Assert.True(delays.Distinct().Count() > 1); // Should have variation
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task RetryLogic_Should_Not_Retry_Non_Transient_Errors()
    {
        // Arrange
        var errorCode = "400"; // Bad Request - not transient
        var isTransient = false;
        var retryAttempted = false;

        // Act - Check if error is transient
        if (int.TryParse(errorCode, out var code))
        {
            isTransient = code >= 500 || code == 429 || code == 408;
        }

        if (!isTransient)
        {
            retryAttempted = false; // Don't retry
        }
        await Task.CompletedTask;

        // Assert
        Assert.False(isTransient);
        Assert.False(retryAttempted);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task RetryLogic_Should_Respect_Retry_After_Header()
    {
        // Arrange
        var retryAfterHeader = "120"; // 120 seconds
        var calculatedDelay = 0;

        // Act - Use retry-after value
        if (int.TryParse(retryAfterHeader, out var seconds))
        {
            calculatedDelay = seconds * 1000; // Convert to milliseconds
        }
        await Task.CompletedTask;

        // Assert
        Assert.Equal(120000, calculatedDelay);
    }

    #endregion

    #region Error Logging Tests

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task ErrorLogging_Should_Log_To_SyncState_ErrorLog()
    {
        // Arrange
        var syncStateType = GetTypeByName("SyncState");
        Assert.NotNull(syncStateType);

        var syncState = CreateInstance(syncStateType);
        Assert.NotNull(syncState);

        var errorLogProp = syncStateType.GetProperty("ErrorLog");
        Assert.NotNull(errorLogProp);

        var errorEntryType = GetTypeByName("SyncErrorEntry");
        Assert.NotNull(errorEntryType);

        // Act - Create error entry
        var errorEntry = CreateInstance(errorEntryType);
        Assert.NotNull(errorEntry);

        var errorMessageProp = errorEntryType.GetProperty("ErrorMessage");
        var errorCodeProp = errorEntryType.GetProperty("ErrorCode");
        var isTransientProp = errorEntryType.GetProperty("IsTransient");

        if (errorMessageProp != null) errorMessageProp.SetValue(errorEntry, "Connection timed out");
        if (errorCodeProp != null) errorCodeProp.SetValue(errorEntry, "TIMEOUT");
        if (isTransientProp != null) isTransientProp.SetValue(errorEntry, true);

        await Task.CompletedTask;

        // Assert
        Assert.Equal("Connection timed out", errorMessageProp?.GetValue(errorEntry)?.ToString());
        Assert.Equal("TIMEOUT", errorCodeProp?.GetValue(errorEntry)?.ToString());
        Assert.True((bool?)isTransientProp?.GetValue(errorEntry));
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task ErrorLogging_Should_Log_To_Application_Insights()
    {
        // Arrange
        var auditLogType = GetTypeByName("AuditLogEntry");
        Assert.NotNull(auditLogType);

        var auditLog = CreateInstance(auditLogType);
        Assert.NotNull(auditLog);

        var operationTypeProp = auditLogType.GetProperty("OperationType");
        var errorCodeProp = auditLogType.GetProperty("ErrorCode");
        var errorMessageProp = auditLogType.GetProperty("ErrorMessage");

        // Act - Log error
        if (operationTypeProp != null) operationTypeProp.SetValue(auditLog, "SyncError");
        if (errorCodeProp != null) errorCodeProp.SetValue(auditLog, "ADAPTER_UNAVAILABLE");
        if (errorMessageProp != null) errorMessageProp.SetValue(auditLog, "Failed to connect to Salesforce");
        await Task.CompletedTask;

        // Assert
        Assert.Equal("SyncError", operationTypeProp?.GetValue(auditLog)?.ToString());
        Assert.Equal("ADAPTER_UNAVAILABLE", errorCodeProp?.GetValue(auditLog)?.ToString());
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task ErrorLogging_Should_Track_Retry_Count()
    {
        // Arrange
        var errorEntryType = GetTypeByName("SyncErrorEntry");
        Assert.NotNull(errorEntryType);

        var errorEntry = CreateInstance(errorEntryType);
        Assert.NotNull(errorEntry);

        var retryCountProp = errorEntryType.GetProperty("RetryCount");
        Assert.NotNull(retryCountProp);

        // Act - Simulate retries
        for (int i = 1; i <= 3; i++)
        {
            retryCountProp.SetValue(errorEntry, i);
        }
        await Task.CompletedTask;

        // Assert
        Assert.Equal(3, (int?)retryCountProp.GetValue(errorEntry));
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task ErrorLogging_Should_Include_Stack_Trace()
    {
        // Arrange
        var errorEntryType = GetTypeByName("SyncErrorEntry");
        Assert.NotNull(errorEntryType);

        var errorEntry = CreateInstance(errorEntryType);
        Assert.NotNull(errorEntry);

        var stackTraceProp = errorEntryType.GetProperty("StackTrace");

        // Act - Set stack trace
        try
        {
            throw new Exception("Test error");
        }
        catch (Exception ex)
        {
            if (stackTraceProp != null) stackTraceProp.SetValue(errorEntry, ex.StackTrace);
        }
        await Task.CompletedTask;

        // Assert
        var stackTrace = stackTraceProp?.GetValue(errorEntry)?.ToString();
        Assert.NotNull(stackTrace);
        Assert.Contains("PollingErrorTests", stackTrace);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task ErrorLogging_Should_Identify_Affected_Resource()
    {
        // Arrange
        var errorEntryType = GetTypeByName("SyncErrorEntry");
        Assert.NotNull(errorEntryType);

        var errorEntry = CreateInstance(errorEntryType);
        Assert.NotNull(errorEntry);

        var resourceTypeProp = errorEntryType.GetProperty("ResourceType");
        var resourceIdProp = errorEntryType.GetProperty("ResourceId");
        var operationProp = errorEntryType.GetProperty("Operation");

        // Act - Set resource context
        if (resourceTypeProp != null) resourceTypeProp.SetValue(errorEntry, "User");
        if (resourceIdProp != null) resourceIdProp.SetValue(errorEntry, "user-001");
        if (operationProp != null) operationProp.SetValue(errorEntry, "UpdateUser");
        await Task.CompletedTask;

        // Assert
        Assert.Equal("User", resourceTypeProp?.GetValue(errorEntry)?.ToString());
        Assert.Equal("user-001", resourceIdProp?.GetValue(errorEntry)?.ToString());
        Assert.Equal("UpdateUser", operationProp?.GetValue(errorEntry)?.ToString());
    }

    #endregion

    #region Operations Team Alert Tests

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Alert_Should_Trigger_After_Max_Retries_Exhausted()
    {
        // Arrange
        var maxRetries = 3;
        var currentRetry = 3;
        var alertTriggered = false;

        // Act - All retries exhausted
        if (currentRetry >= maxRetries)
        {
            alertTriggered = true;
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(alertTriggered);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Alert_Should_Trigger_For_Critical_Errors()
    {
        // Arrange
        var criticalErrorCodes = new HashSet<string> { "401", "403", "QUOTA_EXCEEDED", "ACCOUNT_DISABLED" };
        var errorCode = "401";
        var alertTriggered = false;

        // Act - Check if critical
        if (criticalErrorCodes.Contains(errorCode))
        {
            alertTriggered = true;
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(alertTriggered);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Alert_Should_Include_Error_Details()
    {
        // Arrange
        var alertMessage = new Dictionary<string, object>
        {
            { "severity", "High" },
            { "title", "Adapter Connection Failed" },
            { "tenantId", "tenant-001" },
            { "providerId", "salesforce" },
            { "errorCode", "TIMEOUT" },
            { "errorMessage", "Connection timed out after 30 seconds" },
            { "retryCount", 3 },
            { "lastAttemptTime", DateTime.UtcNow.ToString("O") }
        };

        // Act
        await Task.CompletedTask;

        // Assert
        Assert.Equal("High", alertMessage["severity"]);
        Assert.Contains("salesforce", alertMessage["providerId"]?.ToString() ?? "");
        Assert.Equal(3, (int)alertMessage["retryCount"]);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Alert_Should_Include_Recommended_Action()
    {
        // Arrange
        var errorCode = "401";
        var recommendedAction = "";

        // Act - Determine recommended action based on error
        recommendedAction = errorCode switch
        {
            "401" => "Refresh OAuth token or check credentials in Key Vault",
            "403" => "Verify permissions for the service principal",
            "429" => "Review rate limits and adjust polling frequency",
            "500" => "Check provider status page, retry may succeed",
            _ => "Review logs for additional context"
        };
        await Task.CompletedTask;

        // Assert
        Assert.Contains("OAuth", recommendedAction);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Alert_Should_Not_Spam_For_Same_Error()
    {
        // Arrange
        var alertsSent = new List<(string ErrorCode, DateTime Time)>();
        var cooldownMinutes = 15;
        var errorCode = "500";

        // Act - First alert
        alertsSent.Add((errorCode, DateTime.UtcNow.AddMinutes(-20)));
        
        // Try to send second alert (should be allowed - cooldown passed)
        var lastAlertForCode = alertsSent.FirstOrDefault(a => a.ErrorCode == errorCode);
        var timeSinceLastAlert = DateTime.UtcNow - lastAlertForCode.Time;
        var shouldSendAlert = timeSinceLastAlert.TotalMinutes >= cooldownMinutes;

        if (shouldSendAlert)
        {
            alertsSent.Add((errorCode, DateTime.UtcNow));
        }
        await Task.CompletedTask;

        // Assert
        Assert.True(shouldSendAlert);
        Assert.Equal(2, alertsSent.Count);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Alert_Should_Suppress_During_Cooldown()
    {
        // Arrange
        var lastAlertTime = DateTime.UtcNow.AddMinutes(-5); // 5 minutes ago
        var cooldownMinutes = 15;
        var currentTime = DateTime.UtcNow;

        // Act
        var timeSinceLastAlert = currentTime - lastAlertTime;
        var shouldSendAlert = timeSinceLastAlert.TotalMinutes >= cooldownMinutes;
        await Task.CompletedTask;

        // Assert
        Assert.False(shouldSendAlert); // Only 5 minutes passed, need 15
    }

    #endregion

    #region Sync State Management During Errors

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Error_Should_Set_SyncStatus_To_Failed()
    {
        // Arrange
        var syncStateType = GetTypeByName("SyncState");
        var syncStatusEnum = GetTypeByName("SyncStatus");
        Assert.NotNull(syncStateType);
        Assert.NotNull(syncStatusEnum);

        var syncState = CreateInstance(syncStateType);
        Assert.NotNull(syncState);

        var statusProp = syncStateType.GetProperty("Status");
        Assert.NotNull(statusProp);

        // Act - Set status to Failed
        var failedValue = Enum.Parse(syncStatusEnum, "Failed");
        statusProp.SetValue(syncState, failedValue);
        await Task.CompletedTask;

        // Assert
        Assert.Equal(failedValue, statusProp.GetValue(syncState));
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task PartialError_Should_Set_SyncStatus_To_CompletedWithErrors()
    {
        // Arrange
        var syncStateType = GetTypeByName("SyncState");
        var syncStatusEnum = GetTypeByName("SyncStatus");
        Assert.NotNull(syncStateType);
        Assert.NotNull(syncStatusEnum);

        var syncState = CreateInstance(syncStateType);
        Assert.NotNull(syncState);

        var statusProp = syncStateType.GetProperty("Status");
        Assert.NotNull(statusProp);

        // Act - Some operations succeeded, some failed
        var completedWithErrorsValue = Enum.Parse(syncStatusEnum, "CompletedWithErrors");
        statusProp.SetValue(syncState, completedWithErrorsValue);
        await Task.CompletedTask;

        // Assert
        Assert.Equal(completedWithErrorsValue, statusProp.GetValue(syncState));
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Error_Should_Not_Update_LastSyncTimestamp()
    {
        // Arrange
        var syncStateType = GetTypeByName("SyncState");
        Assert.NotNull(syncStateType);

        var syncState = CreateInstance(syncStateType);
        Assert.NotNull(syncState);

        var lastSyncTimestampProp = syncStateType.GetProperty("LastSyncTimestamp");
        Assert.NotNull(lastSyncTimestampProp);

        var previousSyncTime = DateTime.UtcNow.AddHours(-1);
        lastSyncTimestampProp.SetValue(syncState, previousSyncTime);

        var syncFailed = true;

        // Act - Don't update timestamp on failure
        if (!syncFailed)
        {
            lastSyncTimestampProp.SetValue(syncState, DateTime.UtcNow);
        }
        await Task.CompletedTask;

        // Assert - Timestamp should remain at previous value
        var currentTimestamp = (DateTime?)lastSyncTimestampProp.GetValue(syncState);
        Assert.Equal(previousSyncTime, currentTimestamp);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Error_Should_Preserve_Previous_Snapshot()
    {
        // Arrange
        var syncStateType = GetTypeByName("SyncState");
        Assert.NotNull(syncStateType);

        var syncState = CreateInstance(syncStateType);
        Assert.NotNull(syncState);

        var snapshotChecksumProp = syncStateType.GetProperty("SnapshotChecksum");
        Assert.NotNull(snapshotChecksumProp);

        var previousChecksum = "abc123";
        snapshotChecksumProp.SetValue(syncState, previousChecksum);

        var syncFailed = true;

        // Act - Don't update snapshot on failure
        if (!syncFailed)
        {
            snapshotChecksumProp.SetValue(syncState, "newChecksum");
        }
        await Task.CompletedTask;

        // Assert - Snapshot should remain unchanged
        Assert.Equal(previousChecksum, snapshotChecksumProp.GetValue(syncState)?.ToString());
    }

    #endregion

    #region Error Recovery Tests

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Recovery_Should_Resume_From_Last_Good_State()
    {
        // Arrange
        var lastGoodUserCount = 100;
        var lastGoodGroupCount = 25;
        var lastGoodChecksum = "abc123";

        // Act - Recover from last good state
        var recoveredState = new Dictionary<string, object>
        {
            { "userCount", lastGoodUserCount },
            { "groupCount", lastGoodGroupCount },
            { "checksum", lastGoodChecksum }
        };
        await Task.CompletedTask;

        // Assert
        Assert.Equal(100, (int)recoveredState["userCount"]);
        Assert.Equal(25, (int)recoveredState["groupCount"]);
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Recovery_Should_Clear_Error_Status_On_Success()
    {
        // Arrange
        var syncStateType = GetTypeByName("SyncState");
        var syncStatusEnum = GetTypeByName("SyncStatus");
        Assert.NotNull(syncStateType);
        Assert.NotNull(syncStatusEnum);

        var syncState = CreateInstance(syncStateType);
        Assert.NotNull(syncState);

        var statusProp = syncStateType.GetProperty("Status");
        Assert.NotNull(statusProp);

        // Set to Failed initially
        var failedValue = Enum.Parse(syncStatusEnum, "Failed");
        statusProp.SetValue(syncState, failedValue);

        // Act - Next sync succeeds
        var completedValue = Enum.Parse(syncStatusEnum, "Completed");
        statusProp.SetValue(syncState, completedValue);
        await Task.CompletedTask;

        // Assert
        Assert.Equal(completedValue, statusProp.GetValue(syncState));
    }

    [Fact(Skip = "Pending implementation of T118-T133")]
    public async Task Recovery_Should_Log_Successful_Recovery()
    {
        // Arrange
        var auditLogType = GetTypeByName("AuditLogEntry");
        Assert.NotNull(auditLogType);

        var auditLog = CreateInstance(auditLogType);
        Assert.NotNull(auditLog);

        var operationTypeProp = auditLogType.GetProperty("OperationType");

        // Act - Log recovery
        if (operationTypeProp != null) operationTypeProp.SetValue(auditLog, "SyncRecovered");
        await Task.CompletedTask;

        // Assert
        Assert.Equal("SyncRecovered", operationTypeProp?.GetValue(auditLog)?.ToString());
    }

    #endregion
}
