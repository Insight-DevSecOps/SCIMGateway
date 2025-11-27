// ==========================================================================
// T129-T131: PollingService - Scheduled Provider Polling
// ==========================================================================
// Implements scheduled polling for change detection and drift reconciliation
// T129: Scheduled polling via timer trigger, call adapter, invoke ChangeDetector
// T130: Capture sync state snapshot after each sync
// T131: Error handling for polling failures with retry policy
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SCIMGateway.Core.Adapters;
using SCIMGateway.Core.Auditing;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.SyncEngine;

/// <summary>
/// Configuration options for the polling service.
/// </summary>
public class PollingServiceOptions
{
    /// <summary>
    /// Default polling interval in seconds.
    /// </summary>
    public int DefaultPollingIntervalSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Minimum polling interval in seconds.
    /// </summary>
    public int MinPollingIntervalSeconds { get; set; } = 60; // 1 minute

    /// <summary>
    /// Maximum polling interval in seconds.
    /// </summary>
    public int MaxPollingIntervalSeconds { get; set; } = 86400; // 24 hours

    /// <summary>
    /// Maximum retry attempts for failed polls.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in seconds (exponential backoff).
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Maximum delay between retries in seconds.
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Whether to enable automatic polling on startup.
    /// </summary>
    public bool EnableOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to notify operations team on failures.
    /// </summary>
    public bool NotifyOnFailure { get; set; } = true;

    /// <summary>
    /// Number of consecutive failures before alerting.
    /// </summary>
    public int AlertAfterConsecutiveFailures { get; set; } = 3;

    /// <summary>
    /// Default reconciliation strategy for detected drift.
    /// </summary>
    public ReconciliationStrategy DefaultReconciliationStrategy { get; set; } = ReconciliationStrategy.ManualReview;
}

/// <summary>
/// Configuration for a specific tenant/provider polling schedule.
/// </summary>
public class PollingSchedule
{
    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Polling interval in seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Whether polling is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron expression for scheduling (optional, overrides interval).
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Last poll timestamp.
    /// </summary>
    public DateTime? LastPollTime { get; set; }

    /// <summary>
    /// Next scheduled poll timestamp.
    /// </summary>
    public DateTime? NextPollTime { get; set; }

    /// <summary>
    /// Reconciliation strategy for this schedule.
    /// </summary>
    public ReconciliationStrategy ReconciliationStrategy { get; set; } = ReconciliationStrategy.ManualReview;

    /// <summary>
    /// Number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Whether the schedule is currently in backoff due to failures.
    /// </summary>
    public bool InBackoff { get; set; }

    /// <summary>
    /// Backoff end time.
    /// </summary>
    public DateTime? BackoffUntil { get; set; }
}

/// <summary>
/// Result of a polling operation.
/// </summary>
public class PollingResult
{
    /// <summary>
    /// Unique identifier for this poll.
    /// </summary>
    public string PollId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the poll was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// When the poll started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the poll completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long DurationMs => (long)(CompletedAt - StartedAt).TotalMilliseconds;

    /// <summary>
    /// Number of users polled.
    /// </summary>
    public int UsersPolled { get; set; }

    /// <summary>
    /// Number of groups polled.
    /// </summary>
    public int GroupsPolled { get; set; }

    /// <summary>
    /// Change detection result.
    /// </summary>
    public ChangeDetectionResult? ChangeDetectionResult { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Whether this was a retry.
    /// </summary>
    public bool IsRetry { get; set; }

    /// <summary>
    /// Retry attempt number (0 for first attempt).
    /// </summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// Snapshot captured after this poll.
    /// </summary>
    public SyncSnapshot? Snapshot { get; set; }
}

/// <summary>
/// Interface for the polling service.
/// </summary>
public interface IPollingService
{
    /// <summary>
    /// Starts polling for a specific tenant/provider.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="intervalSeconds">Polling interval in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartPollingAsync(string tenantId, string providerId, int? intervalSeconds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops polling for a specific tenant/provider.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    Task StopPollingAsync(string tenantId, string providerId);

    /// <summary>
    /// Triggers an immediate poll for a specific tenant/provider.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Polling result.</returns>
    Task<PollingResult> TriggerPollAsync(string tenantId, string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the polling schedule for a tenant/provider.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <returns>Polling schedule or null if not configured.</returns>
    PollingSchedule? GetSchedule(string tenantId, string providerId);

    /// <summary>
    /// Updates the polling schedule for a tenant/provider.
    /// </summary>
    /// <param name="schedule">The polling schedule.</param>
    void UpdateSchedule(PollingSchedule schedule);

    /// <summary>
    /// Gets all active polling schedules.
    /// </summary>
    /// <returns>List of active schedules.</returns>
    IEnumerable<PollingSchedule> GetActiveSchedules();

    /// <summary>
    /// Gets the last polling result for a tenant/provider.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <returns>Last polling result or null.</returns>
    PollingResult? GetLastResult(string tenantId, string providerId);

    /// <summary>
    /// Gets recent polling results for a tenant/provider.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="count">Number of results to return.</param>
    /// <returns>Recent polling results.</returns>
    IEnumerable<PollingResult> GetRecentResults(string tenantId, string providerId, int count = 10);
}

/// <summary>
/// Implements scheduled polling for provider state synchronization.
/// </summary>
public class PollingService : IPollingService, IHostedService, IDisposable
{
    private readonly ILogger<PollingService> _logger;
    private readonly IAdapterRegistry _adapterRegistry;
    private readonly IChangeDetector _changeDetector;
    private readonly ISyncStateRepository _syncStateRepository;
    private readonly IReconciler _reconciler;
    private readonly IAuditLogger _auditLogger;
    private readonly PollingServiceOptions _options;

    private readonly ConcurrentDictionary<string, PollingSchedule> _schedules = new();
    private readonly ConcurrentDictionary<string, Timer> _timers = new();
    private readonly ConcurrentDictionary<string, List<PollingResult>> _results = new();
    private readonly ConcurrentDictionary<string, ResourceState> _lastKnownStates = new();
    private readonly SemaphoreSlim _pollLock = new(1, 1);

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollingService"/> class.
    /// </summary>
    public PollingService(
        ILogger<PollingService> logger,
        IAdapterRegistry adapterRegistry,
        IChangeDetector changeDetector,
        ISyncStateRepository syncStateRepository,
        IReconciler reconciler,
        IAuditLogger auditLogger,
        IOptions<PollingServiceOptions>? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapterRegistry = adapterRegistry ?? throw new ArgumentNullException(nameof(adapterRegistry));
        _changeDetector = changeDetector ?? throw new ArgumentNullException(nameof(changeDetector));
        _syncStateRepository = syncStateRepository ?? throw new ArgumentNullException(nameof(syncStateRepository));
        _reconciler = reconciler ?? throw new ArgumentNullException(nameof(reconciler));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _options = options?.Value ?? new PollingServiceOptions();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PollingService starting...");

        if (_options.EnableOnStartup)
        {
            // Load existing schedules from sync state repository
            // In production, this would load from configuration or database
            _logger.LogInformation("Polling service ready. Use StartPollingAsync to enable polling for specific tenants/providers.");
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PollingService stopping...");

        // Stop all timers
        foreach (var timer in _timers.Values)
        {
            await timer.DisposeAsync();
        }
        _timers.Clear();

        _logger.LogInformation("PollingService stopped.");
    }

    /// <inheritdoc />
    public async Task StartPollingAsync(string tenantId, string providerId, int? intervalSeconds = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        var key = GetScheduleKey(tenantId, providerId);
        var interval = intervalSeconds ?? _options.DefaultPollingIntervalSeconds;

        // Validate interval
        interval = Math.Clamp(interval, _options.MinPollingIntervalSeconds, _options.MaxPollingIntervalSeconds);

        var schedule = new PollingSchedule
        {
            TenantId = tenantId,
            ProviderId = providerId,
            IntervalSeconds = interval,
            Enabled = true,
            NextPollTime = DateTime.UtcNow.AddSeconds(interval),
            ReconciliationStrategy = _options.DefaultReconciliationStrategy
        };

        _schedules[key] = schedule;

        // Create and start timer
        var timer = new Timer(
            async _ => await ExecuteScheduledPollAsync(tenantId, providerId),
            null,
            TimeSpan.FromSeconds(interval),
            TimeSpan.FromSeconds(interval));

        _timers[key] = timer;

        _logger.LogInformation(
            "Started polling for tenant {TenantId}, provider {ProviderId} with interval {Interval}s",
            tenantId, providerId, interval);

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopPollingAsync(string tenantId, string providerId)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        var key = GetScheduleKey(tenantId, providerId);

        if (_timers.TryRemove(key, out var timer))
        {
            await timer.DisposeAsync();
        }

        if (_schedules.TryGetValue(key, out var schedule))
        {
            schedule.Enabled = false;
        }

        _logger.LogInformation(
            "Stopped polling for tenant {TenantId}, provider {ProviderId}",
            tenantId, providerId);
    }

    /// <inheritdoc />
    public async Task<PollingResult> TriggerPollAsync(string tenantId, string providerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        return await ExecutePollAsync(tenantId, providerId, isRetry: false, retryAttempt: 0, cancellationToken);
    }

    /// <summary>
    /// Executes a scheduled poll with retry logic.
    /// </summary>
    private async Task ExecuteScheduledPollAsync(string tenantId, string providerId)
    {
        var key = GetScheduleKey(tenantId, providerId);

        if (!_schedules.TryGetValue(key, out var schedule) || !schedule.Enabled)
        {
            return;
        }

        // Check if in backoff
        if (schedule.InBackoff && schedule.BackoffUntil > DateTime.UtcNow)
        {
            _logger.LogDebug(
                "Skipping poll for {TenantId}/{ProviderId} - in backoff until {BackoffUntil}",
                tenantId, providerId, schedule.BackoffUntil);
            return;
        }

        // Execute with retry logic
        var result = await ExecutePollWithRetryAsync(tenantId, providerId);

        // Update schedule
        schedule.LastPollTime = DateTime.UtcNow;
        schedule.NextPollTime = DateTime.UtcNow.AddSeconds(schedule.IntervalSeconds);

        if (result.Success)
        {
            schedule.ConsecutiveFailures = 0;
            schedule.InBackoff = false;
            schedule.BackoffUntil = null;
        }
        else
        {
            schedule.ConsecutiveFailures++;

            // Check if we should enter backoff
            if (schedule.ConsecutiveFailures >= _options.AlertAfterConsecutiveFailures)
            {
                var backoffSeconds = Math.Min(
                    _options.RetryBaseDelaySeconds * (int)Math.Pow(2, schedule.ConsecutiveFailures - 1),
                    _options.MaxRetryDelaySeconds);

                schedule.InBackoff = true;
                schedule.BackoffUntil = DateTime.UtcNow.AddSeconds(backoffSeconds);

                _logger.LogWarning(
                    "Polling for {TenantId}/{ProviderId} entering backoff after {Failures} failures. Backoff until {BackoffUntil}",
                    tenantId, providerId, schedule.ConsecutiveFailures, schedule.BackoffUntil);

                // T131: Alert operations team
                if (_options.NotifyOnFailure)
                {
                    await AlertOperationsTeamAsync(tenantId, providerId, schedule.ConsecutiveFailures, result.ErrorMessage);
                }
            }
        }
    }

    /// <summary>
    /// Executes a poll with retry logic.
    /// </summary>
    private async Task<PollingResult> ExecutePollWithRetryAsync(string tenantId, string providerId)
    {
        var attempt = 0;
        PollingResult? lastResult = null;

        while (attempt <= _options.MaxRetryAttempts)
        {
            var isRetry = attempt > 0;
            lastResult = await ExecutePollAsync(tenantId, providerId, isRetry, attempt, CancellationToken.None);

            if (lastResult.Success)
            {
                return lastResult;
            }

            attempt++;

            if (attempt <= _options.MaxRetryAttempts)
            {
                var delay = Math.Min(
                    _options.RetryBaseDelaySeconds * (int)Math.Pow(2, attempt - 1),
                    _options.MaxRetryDelaySeconds);

                _logger.LogWarning(
                    "Poll failed for {TenantId}/{ProviderId}, attempt {Attempt}. Retrying in {Delay}s. Error: {Error}",
                    tenantId, providerId, attempt, delay, lastResult.ErrorMessage);

                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        return lastResult!;
    }

    /// <summary>
    /// Executes a single poll operation.
    /// T129: Core polling logic - call adapter, invoke ChangeDetector.
    /// T130: Capture sync state snapshot.
    /// T131: Error handling.
    /// </summary>
    private async Task<PollingResult> ExecutePollAsync(
        string tenantId,
        string providerId,
        bool isRetry,
        int retryAttempt,
        CancellationToken cancellationToken)
    {
        var result = new PollingResult
        {
            TenantId = tenantId,
            ProviderId = providerId,
            StartedAt = DateTime.UtcNow,
            IsRetry = isRetry,
            RetryAttempt = retryAttempt
        };

        var key = GetScheduleKey(tenantId, providerId);

        try
        {
            await _pollLock.WaitAsync(cancellationToken);

            _logger.LogInformation(
                "Starting poll for tenant {TenantId}, provider {ProviderId} (attempt {Attempt})",
                tenantId, providerId, retryAttempt + 1);

            // Get the adapter
            if (!_adapterRegistry.TryGetAdapter(providerId, out var adapter) || adapter == null)
            {
                throw new InvalidOperationException($"Adapter not found for provider: {providerId}");
            }

            // Check adapter health first
            var health = await adapter.CheckHealthAsync(cancellationToken);
            if (health.Status != HealthStatusLevel.Healthy)
            {
                throw new InvalidOperationException($"Adapter unhealthy: {health.Status}");
            }

            // T129: Poll users and groups from provider
            var providerUsers = new List<ScimUser>();
            var providerGroups = new List<ScimGroup>();

            // Fetch all users
            var userFilter = new QueryFilter { StartIndex = 1, Count = 100 };
            var usersResult = await adapter.ListUsersAsync(userFilter, cancellationToken);
            providerUsers.AddRange(usersResult.Resources);
            result.UsersPolled = providerUsers.Count;

            // Handle pagination for users
            while (usersResult.Resources.Count == userFilter.Count && 
                   providerUsers.Count < usersResult.TotalResults)
            {
                userFilter.StartIndex = providerUsers.Count + 1;
                usersResult = await adapter.ListUsersAsync(userFilter, cancellationToken);
                providerUsers.AddRange(usersResult.Resources);
            }
            result.UsersPolled = providerUsers.Count;

            // Fetch all groups
            var groupFilter = new QueryFilter { StartIndex = 1, Count = 100 };
            var groupsResult = await adapter.ListGroupsAsync(groupFilter, cancellationToken);
            providerGroups.AddRange(groupsResult.Resources);

            // Handle pagination for groups
            while (groupsResult.Resources.Count == groupFilter.Count && 
                   providerGroups.Count < groupsResult.TotalResults)
            {
                groupFilter.StartIndex = providerGroups.Count + 1;
                groupsResult = await adapter.ListGroupsAsync(groupFilter, cancellationToken);
                providerGroups.AddRange(groupsResult.Resources);
            }
            result.GroupsPolled = providerGroups.Count;

            // Create current provider state
            var currentProviderState = new ResourceState
            {
                Users = providerUsers,
                Groups = providerGroups,
                Timestamp = DateTime.UtcNow,
                Source = "Provider"
            };

            // Get last known state
            var lastKnownState = _lastKnownStates.GetValueOrDefault(key);

            // Get sync state from repository
            var syncState = await _syncStateRepository.GetAsync(tenantId, providerId, cancellationToken);
            if (syncState == null)
            {
                syncState = new SyncState
                {
                    Id = $"{tenantId}-{providerId}",
                    TenantId = tenantId,
                    ProviderId = providerId,
                    SyncDirection = SyncDirection.EntraToSaas,
                    Status = SyncStatus.InProgress
                };
                await _syncStateRepository.CreateAsync(syncState, cancellationToken);
            }

            // T129: Invoke ChangeDetector
            var context = new ChangeDetectionContext
            {
                TenantId = tenantId,
                ProviderId = providerId,
                PreviousProviderState = lastKnownState,
                CurrentProviderState = currentProviderState,
                LastKnownStateHash = syncState.LastKnownState,
                SyncDirection = syncState.SyncDirection
            };

            var detectionResult = await _changeDetector.DetectChangesAsync(context, cancellationToken);
            result.ChangeDetectionResult = detectionResult;

            // Update last known state
            currentProviderState.StateHash = detectionResult.NewStateHash;
            _lastKnownStates[key] = currentProviderState;

            // T130: Capture sync state snapshot
            var snapshot = new SyncSnapshot
            {
                Checksum = detectionResult.NewStateHash ?? string.Empty,
                UserCount = providerUsers.Count,
                GroupCount = providerGroups.Count,
                Status = SyncStatus.Completed,
                Timestamp = DateTime.UtcNow
            };
            result.Snapshot = snapshot;

            await _syncStateRepository.RecordSyncCompletionAsync(tenantId, providerId, snapshot, cancellationToken);

            // Process drift reports with reconciler
            if (detectionResult.HasChanges)
            {
                var schedule = _schedules.GetValueOrDefault(key);
                var strategy = schedule?.ReconciliationStrategy ?? _options.DefaultReconciliationStrategy;

                foreach (var drift in detectionResult.DriftReports)
                {
                    await _reconciler.ReconcileDriftAsync(drift, syncState, strategy, cancellationToken);
                }

                _logger.LogInformation(
                    "Detected {Changes} changes for {TenantId}/{ProviderId}: {Summary}",
                    detectionResult.TotalChanges, tenantId, providerId, detectionResult.Summary);
            }

            result.Success = true;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Poll completed for {TenantId}/{ProviderId}: {Users} users, {Groups} groups, {Changes} changes in {Duration}ms",
                tenantId, providerId, result.UsersPolled, result.GroupsPolled,
                detectionResult.TotalChanges, result.DurationMs);
        }
        catch (Exception ex)
        {
            // T131: Error handling
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.ErrorCode = ex.GetType().Name;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogError(ex,
                "Poll failed for {TenantId}/{ProviderId}: {Error}",
                tenantId, providerId, ex.Message);

            // Log error to sync state
            var errorEntry = new SyncErrorEntry
            {
                Operation = "Poll",
                ErrorCode = ex.GetType().Name,
                ErrorMessage = ex.Message,
                IsTransient = IsTransientError(ex),
                RetryCount = retryAttempt
            };

            try
            {
                await _syncStateRepository.AddErrorLogEntryAsync(tenantId, providerId, errorEntry, cancellationToken);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to log error to sync state");
            }
        }
        finally
        {
            _pollLock.Release();

            // Store result
            StoreResult(key, result);
        }

        return result;
    }

    /// <summary>
    /// T131: Alerts the operations team about polling failures.
    /// </summary>
    private Task AlertOperationsTeamAsync(string tenantId, string providerId, int consecutiveFailures, string? errorMessage)
    {
        // In production, this would send emails, call webhooks, or push to a monitoring system
        _logger.LogCritical(
            "ALERT: Polling for {TenantId}/{ProviderId} has failed {Failures} consecutive times. " +
            "Last error: {Error}. Operations team should investigate.",
            tenantId, providerId, consecutiveFailures, errorMessage);

        // Could integrate with:
        // - Azure Monitor / Application Insights alerts
        // - PagerDuty / OpsGenie
        // - Slack / Teams webhooks
        // - Email notifications

        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines if an error is transient and may be resolved on retry.
    /// </summary>
    private static bool IsTransientError(Exception ex)
    {
        // Check for common transient errors
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is TimeoutException ||
               (ex is InvalidOperationException && ex.Message.Contains("unhealthy"));
    }

    /// <summary>
    /// Stores a polling result in the history.
    /// </summary>
    private void StoreResult(string key, PollingResult result)
    {
        var results = _results.GetOrAdd(key, _ => new List<PollingResult>());

        lock (results)
        {
            results.Insert(0, result);

            // Keep only the last 100 results
            if (results.Count > 100)
            {
                results.RemoveRange(100, results.Count - 100);
            }
        }
    }

    /// <inheritdoc />
    public PollingSchedule? GetSchedule(string tenantId, string providerId)
    {
        var key = GetScheduleKey(tenantId, providerId);
        return _schedules.GetValueOrDefault(key);
    }

    /// <inheritdoc />
    public void UpdateSchedule(PollingSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var key = GetScheduleKey(schedule.TenantId, schedule.ProviderId);
        _schedules[key] = schedule;

        // Update timer if exists
        if (_timers.TryGetValue(key, out var existingTimer))
        {
            existingTimer.Change(
                TimeSpan.FromSeconds(schedule.IntervalSeconds),
                TimeSpan.FromSeconds(schedule.IntervalSeconds));
        }
    }

    /// <inheritdoc />
    public IEnumerable<PollingSchedule> GetActiveSchedules()
    {
        return _schedules.Values.Where(s => s.Enabled).ToList();
    }

    /// <inheritdoc />
    public PollingResult? GetLastResult(string tenantId, string providerId)
    {
        var key = GetScheduleKey(tenantId, providerId);

        if (_results.TryGetValue(key, out var results))
        {
            lock (results)
            {
                return results.FirstOrDefault();
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IEnumerable<PollingResult> GetRecentResults(string tenantId, string providerId, int count = 10)
    {
        var key = GetScheduleKey(tenantId, providerId);

        if (_results.TryGetValue(key, out var results))
        {
            lock (results)
            {
                return results.Take(count).ToList();
            }
        }

        return Enumerable.Empty<PollingResult>();
    }

    /// <summary>
    /// Gets the key for a tenant/provider schedule.
    /// </summary>
    private static string GetScheduleKey(string tenantId, string providerId)
    {
        return $"{tenantId}:{providerId}";
    }

    /// <summary>
    /// Disposes the polling service.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the polling service.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Dispose();
                }
                _timers.Clear();
                _pollLock.Dispose();
            }

            _disposed = true;
        }
    }
}
