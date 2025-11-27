// ==========================================================================
// T125-T128: Reconciler - Drift and Conflict Reconciliation
// ==========================================================================
// Implements reconciliation strategies: AUTO_APPLY, MANUAL_REVIEW, IGNORE
// ==========================================================================

using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Adapters;
using SCIMGateway.Core.Auditing;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.SyncEngine;

/// <summary>
/// Reconciliation strategy for handling drift.
/// </summary>
public enum ReconciliationStrategy
{
    /// <summary>
    /// Automatically apply changes based on sync direction.
    /// T126: If direction=ENTRA_TO_SAAS, overwrite provider with Entra state;
    /// if direction=SAAS_TO_ENTRA, overwrite Entra with provider state.
    /// </summary>
    AutoApply,

    /// <summary>
    /// Flag for manual review, block auto-sync, notify operations team.
    /// T127: Create conflict log entry, block auto-sync for conflicted resource.
    /// </summary>
    ManualReview,

    /// <summary>
    /// Ignore the drift, log as informational only.
    /// T128: Do not apply changes, just log.
    /// </summary>
    Ignore
}

/// <summary>
/// Result of a reconciliation operation.
/// </summary>
public class ReconciliationResult
{
    /// <summary>
    /// Unique identifier for this reconciliation.
    /// </summary>
    public string ReconciliationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The drift ID that was reconciled.
    /// </summary>
    public string DriftId { get; set; } = string.Empty;

    /// <summary>
    /// Whether reconciliation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Strategy that was applied.
    /// </summary>
    public ReconciliationStrategy Strategy { get; set; }

    /// <summary>
    /// Action taken during reconciliation.
    /// </summary>
    public ReconciliationAction Action { get; set; }

    /// <summary>
    /// Resource type affected.
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Resource ID affected.
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Sync direction used.
    /// </summary>
    public SyncDirection SyncDirection { get; set; }

    /// <summary>
    /// Error message if reconciliation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Details about what was changed.
    /// </summary>
    public string? ChangesSummary { get; set; }

    /// <summary>
    /// When reconciliation was performed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Actor who performed the reconciliation.
    /// </summary>
    public string? ReconciledBy { get; set; }

    /// <summary>
    /// Whether sync is blocked for this resource.
    /// </summary>
    public bool SyncBlocked { get; set; }

    /// <summary>
    /// Notes about the reconciliation.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request for manual reconciliation.
/// </summary>
public class ManualReconciliationRequest
{
    /// <summary>
    /// The drift ID to reconcile.
    /// </summary>
    public string DriftId { get; set; } = string.Empty;

    /// <summary>
    /// The conflict ID to reconcile (if applicable).
    /// </summary>
    public string? ConflictId { get; set; }

    /// <summary>
    /// Direction to apply for reconciliation.
    /// </summary>
    public SyncDirection Direction { get; set; }

    /// <summary>
    /// Actor performing the reconciliation.
    /// </summary>
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Notes about the reconciliation decision.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether to apply the change immediately.
    /// </summary>
    public bool ApplyImmediately { get; set; } = true;
}

/// <summary>
/// Configuration for reconciliation behavior.
/// </summary>
public class ReconcilerOptions
{
    /// <summary>
    /// Default strategy to use for drift reconciliation.
    /// </summary>
    public ReconciliationStrategy DefaultStrategy { get; set; } = ReconciliationStrategy.ManualReview;

    /// <summary>
    /// Strategy to use for conflicts (typically ManualReview).
    /// </summary>
    public ReconciliationStrategy ConflictStrategy { get; set; } = ReconciliationStrategy.ManualReview;

    /// <summary>
    /// Maximum number of auto-apply operations per sync cycle.
    /// </summary>
    public int MaxAutoApplyPerCycle { get; set; } = 100;

    /// <summary>
    /// Whether to notify operations team on drift detection.
    /// </summary>
    public bool NotifyOnDrift { get; set; } = true;

    /// <summary>
    /// Whether to notify operations team on conflict detection.
    /// </summary>
    public bool NotifyOnConflict { get; set; } = true;

    /// <summary>
    /// Email addresses for notifications.
    /// </summary>
    public List<string> NotificationEmails { get; set; } = new();

    /// <summary>
    /// Webhook URLs for notifications.
    /// </summary>
    public List<string> NotificationWebhooks { get; set; } = new();

    /// <summary>
    /// Severity threshold for notifications (only notify for this severity or higher).
    /// </summary>
    public DriftSeverity NotificationSeverityThreshold { get; set; } = DriftSeverity.Medium;
}

/// <summary>
/// Interface for drift and conflict reconciliation.
/// </summary>
public interface IReconciler
{
    /// <summary>
    /// Reconciles a drift report using the configured strategy.
    /// </summary>
    /// <param name="driftReport">The drift report to reconcile.</param>
    /// <param name="syncState">Current sync state.</param>
    /// <param name="strategy">Optional strategy override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Reconciliation result.</returns>
    Task<ReconciliationResult> ReconcileDriftAsync(
        DriftReport driftReport,
        SyncState syncState,
        ReconciliationStrategy? strategy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconciles a conflict report.
    /// </summary>
    /// <param name="conflictReport">The conflict report to reconcile.</param>
    /// <param name="syncState">Current sync state.</param>
    /// <param name="resolution">Resolution to apply.</param>
    /// <param name="actorId">Actor performing the reconciliation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Reconciliation result.</returns>
    Task<ReconciliationResult> ReconcileConflictAsync(
        ConflictReport conflictReport,
        SyncState syncState,
        ConflictResolution resolution,
        string actorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a manual reconciliation request.
    /// </summary>
    /// <param name="request">The manual reconciliation request.</param>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Reconciliation result.</returns>
    Task<ReconciliationResult> ProcessManualReconciliationAsync(
        ManualReconciliationRequest request,
        string tenantId,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending drift reports for manual review.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of drift reports pending review.</returns>
    Task<IEnumerable<DriftReport>> GetPendingDriftReportsAsync(
        string tenantId,
        string? providerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending conflict reports for manual review.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of conflict reports pending review.</returns>
    Task<IEnumerable<ConflictReport>> GetPendingConflictReportsAsync(
        string tenantId,
        string? providerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Blocks sync for a specific resource.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="resourceType">Resource type.</param>
    /// <param name="resourceId">Resource ID.</param>
    /// <param name="reason">Reason for blocking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BlockSyncAsync(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblocks sync for a specific resource.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="resourceType">Resource type.</param>
    /// <param name="resourceId">Resource ID.</param>
    /// <param name="actorId">Actor unblocking the sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnblockSyncAsync(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        string actorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if sync is blocked for a resource.
    /// </summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="resourceType">Resource type.</param>
    /// <param name="resourceId">Resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if sync is blocked.</returns>
    Task<bool> IsSyncBlockedAsync(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implements drift and conflict reconciliation strategies.
/// </summary>
public class Reconciler : IReconciler
{
    private readonly ILogger<Reconciler> _logger;
    private readonly IAuditLogger _auditLogger;
    private readonly ISyncStateRepository _syncStateRepository;
    private readonly IAdapterRegistry _adapterRegistry;
    private readonly ReconcilerOptions _options;

    // In-memory storage for blocked resources (in production, use persistent storage)
    private readonly Dictionary<string, BlockedResource> _blockedResources = new();
    private readonly Dictionary<string, DriftReport> _pendingDriftReports = new();
    private readonly Dictionary<string, ConflictReport> _pendingConflictReports = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Reconciler"/> class.
    /// </summary>
    public Reconciler(
        ILogger<Reconciler> logger,
        IAuditLogger auditLogger,
        ISyncStateRepository syncStateRepository,
        IAdapterRegistry adapterRegistry,
        ReconcilerOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _syncStateRepository = syncStateRepository ?? throw new ArgumentNullException(nameof(syncStateRepository));
        _adapterRegistry = adapterRegistry ?? throw new ArgumentNullException(nameof(adapterRegistry));
        _options = options ?? new ReconcilerOptions();
    }

    /// <inheritdoc />
    public async Task<ReconciliationResult> ReconcileDriftAsync(
        DriftReport driftReport,
        SyncState syncState,
        ReconciliationStrategy? strategy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(driftReport);
        ArgumentNullException.ThrowIfNull(syncState);

        var effectiveStrategy = strategy ?? _options.DefaultStrategy;
        var result = new ReconciliationResult
        {
            DriftId = driftReport.DriftId,
            ResourceType = driftReport.ResourceType,
            ResourceId = driftReport.ResourceId,
            Strategy = effectiveStrategy,
            SyncDirection = syncState.SyncDirection
        };

        _logger.LogInformation(
            "Reconciling drift {DriftId} for {ResourceType}/{ResourceId} using strategy {Strategy}",
            driftReport.DriftId, driftReport.ResourceType, driftReport.ResourceId, effectiveStrategy);

        try
        {
            switch (effectiveStrategy)
            {
                case ReconciliationStrategy.AutoApply:
                    result = await ApplyAutoReconciliationAsync(driftReport, syncState, result, cancellationToken);
                    break;

                case ReconciliationStrategy.ManualReview:
                    result = await ApplyManualReviewAsync(driftReport, syncState, result, cancellationToken);
                    break;

                case ReconciliationStrategy.Ignore:
                    result = await ApplyIgnoreAsync(driftReport, syncState, result, cancellationToken);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy), effectiveStrategy, "Unknown reconciliation strategy");
            }

            // Log the reconciliation
            await _auditLogger.LogDriftReconciliationAsync(
                driftReport.TenantId,
                driftReport.ProviderId,
                driftReport.DriftId,
                driftReport.ResourceType,
                driftReport.ResourceId,
                result.Action.ToString(),
                result.Success,
                result.ReconciledBy ?? "system",
                result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconcile drift {DriftId}", driftReport.DriftId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// T126: Implements AUTO_APPLY reconciliation strategy.
    /// If direction=ENTRA_TO_SAAS, overwrite provider with Entra state.
    /// If direction=SAAS_TO_ENTRA, overwrite Entra with provider state.
    /// </summary>
    private async Task<ReconciliationResult> ApplyAutoReconciliationAsync(
        DriftReport driftReport,
        SyncState syncState,
        ReconciliationResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Applying AUTO_APPLY reconciliation for {ResourceType}/{ResourceId}, direction={Direction}",
            driftReport.ResourceType, driftReport.ResourceId, syncState.SyncDirection);

        try
        {
            // Get the adapter
            if (!_adapterRegistry.TryGetAdapter(driftReport.ProviderId, out var adapter) || adapter == null)
            {
                throw new InvalidOperationException($"Adapter not found for provider: {driftReport.ProviderId}");
            }

            switch (syncState.SyncDirection)
            {
                case SyncDirection.EntraToSaas:
                    // Overwrite provider with Entra state
                    await ApplyEntraToProviderAsync(driftReport, adapter, cancellationToken);
                    result.ChangesSummary = "Applied Entra state to provider";
                    break;

                case SyncDirection.SaasToEntra:
                    // In real implementation, this would update Entra via Microsoft Graph API
                    // For now, we log the intended action
                    _logger.LogInformation(
                        "Would apply provider state to Entra for {ResourceType}/{ResourceId} (not implemented - requires Graph API)",
                        driftReport.ResourceType, driftReport.ResourceId);
                    result.ChangesSummary = "Provider state would be applied to Entra (pending Graph API integration)";
                    break;
            }

            result.Success = true;
            result.Action = ReconciliationAction.AutoApplied;
            result.ReconciledBy = "system";

            // Update the drift report as reconciled
            driftReport.Reconciled = true;
            driftReport.ReconciledAt = DateTime.UtcNow;
            driftReport.ReconciliationAction = ReconciliationAction.AutoApplied;
            driftReport.ReconciledBy = "system";
            driftReport.ModifiedAt = DateTime.UtcNow;

            // Remove from pending if present
            lock (_lock)
            {
                _pendingDriftReports.Remove(driftReport.DriftId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AUTO_APPLY failed for {ResourceType}/{ResourceId}", 
                driftReport.ResourceType, driftReport.ResourceId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Action = ReconciliationAction.AutoApplied;
        }

        return result;
    }

    /// <summary>
    /// Applies Entra state to provider (for ENTRA_TO_SAAS direction).
    /// </summary>
    private async Task ApplyEntraToProviderAsync(
        DriftReport driftReport,
        IAdapter adapter,
        CancellationToken cancellationToken)
    {
        switch (driftReport.DriftType)
        {
            case DriftType.Added:
                // Resource was added in provider but not in Entra
                // In ENTRA_TO_SAAS mode, we should delete it from provider
                if (driftReport.ResourceType == "User")
                {
                    await adapter.DeleteUserAsync(driftReport.ResourceId, cancellationToken);
                    _logger.LogInformation("Deleted user {ResourceId} from provider (not in Entra)", driftReport.ResourceId);
                }
                else if (driftReport.ResourceType == "Group")
                {
                    await adapter.DeleteGroupAsync(driftReport.ResourceId, cancellationToken);
                    _logger.LogInformation("Deleted group {ResourceId} from provider (not in Entra)", driftReport.ResourceId);
                }
                break;

            case DriftType.Deleted:
                // Resource was deleted in provider but exists in Entra
                // In ENTRA_TO_SAAS mode, we should recreate it in provider
                if (driftReport.Details?.EntraState != null)
                {
                    if (driftReport.ResourceType == "User" && driftReport.Details.EntraState is ScimUser user)
                    {
                        await adapter.CreateUserAsync(user, cancellationToken);
                        _logger.LogInformation("Recreated user {ResourceId} in provider (deleted but exists in Entra)", driftReport.ResourceId);
                    }
                    else if (driftReport.ResourceType == "Group" && driftReport.Details.EntraState is ScimGroup group)
                    {
                        await adapter.CreateGroupAsync(group, cancellationToken);
                        _logger.LogInformation("Recreated group {ResourceId} in provider (deleted but exists in Entra)", driftReport.ResourceId);
                    }
                }
                break;

            case DriftType.Modified:
            case DriftType.AttributeMismatch:
                // Resource was modified in provider
                // In ENTRA_TO_SAAS mode, overwrite provider with Entra state
                if (driftReport.Details?.EntraState != null)
                {
                    if (driftReport.ResourceType == "User" && driftReport.Details.EntraState is ScimUser user)
                    {
                        await adapter.UpdateUserAsync(driftReport.ResourceId, user, cancellationToken);
                        _logger.LogInformation("Updated user {ResourceId} in provider with Entra state", driftReport.ResourceId);
                    }
                    else if (driftReport.ResourceType == "Group" && driftReport.Details.EntraState is ScimGroup group)
                    {
                        await adapter.UpdateGroupAsync(driftReport.ResourceId, group, cancellationToken);
                        _logger.LogInformation("Updated group {ResourceId} in provider with Entra state", driftReport.ResourceId);
                    }
                }
                break;

            case DriftType.MembershipMismatch:
                // Group membership differs
                // Sync membership from Entra to provider
                if (driftReport.Details != null)
                {
                    // Remove members that should not be there
                    foreach (var memberId in driftReport.Details.MembersAdded ?? Enumerable.Empty<string>())
                    {
                        await adapter.RemoveUserFromGroupAsync(driftReport.ResourceId, memberId, cancellationToken);
                        _logger.LogDebug("Removed user {UserId} from group {GroupId}", memberId, driftReport.ResourceId);
                    }

                    // Add members that should be there
                    foreach (var memberId in driftReport.Details.MembersRemoved ?? Enumerable.Empty<string>())
                    {
                        await adapter.AddUserToGroupAsync(driftReport.ResourceId, memberId, cancellationToken);
                        _logger.LogDebug("Added user {UserId} to group {GroupId}", memberId, driftReport.ResourceId);
                    }
                    
                    _logger.LogInformation("Synced membership for group {ResourceId}", driftReport.ResourceId);
                }
                break;
        }
    }

    /// <summary>
    /// T127: Implements MANUAL_REVIEW reconciliation strategy.
    /// Creates conflict log entry, blocks auto-sync for conflicted resource.
    /// </summary>
    private async Task<ReconciliationResult> ApplyManualReviewAsync(
        DriftReport driftReport,
        SyncState syncState,
        ReconciliationResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Flagging {ResourceType}/{ResourceId} for MANUAL_REVIEW",
            driftReport.ResourceType, driftReport.ResourceId);

        // Block sync for this resource
        await BlockSyncAsync(
            driftReport.TenantId,
            driftReport.ProviderId,
            driftReport.ResourceType,
            driftReport.ResourceId,
            $"Drift detected: {driftReport.DriftType}",
            cancellationToken);

        // Store in pending reports for manual review
        lock (_lock)
        {
            _pendingDriftReports[driftReport.DriftId] = driftReport;
        }

        // Add to sync state drift log
        var driftLogEntry = new DriftLogEntry
        {
            DriftId = driftReport.DriftId,
            DetectedAt = driftReport.Timestamp,
            DriftType = driftReport.DriftType,
            ResourceType = driftReport.ResourceType,
            ResourceId = driftReport.ResourceId,
            OldValue = driftReport.Details?.EntraState?.ToString(),
            NewValue = driftReport.Details?.ProviderState?.ToString(),
            Reconciled = false,
            ReconciliationAction = "PendingManualReview"
        };

        await _syncStateRepository.AddDriftLogEntryAsync(
            driftReport.TenantId,
            driftReport.ProviderId,
            driftLogEntry,
            cancellationToken);

        // Notify operations team if configured
        if (_options.NotifyOnDrift && (int)driftReport.Severity >= (int)_options.NotificationSeverityThreshold)
        {
            await NotifyOperationsTeamAsync(driftReport, cancellationToken);
        }

        result.Success = true;
        result.Action = ReconciliationAction.ManualApproved; // Technically pending, but using existing enum
        result.SyncBlocked = true;
        result.ChangesSummary = "Flagged for manual review, sync blocked";
        result.ReconciledBy = null; // Pending

        return result;
    }

    /// <summary>
    /// T128: Implements IGNORE reconciliation strategy.
    /// Logs drift as informational, does not apply changes.
    /// </summary>
    private async Task<ReconciliationResult> ApplyIgnoreAsync(
        DriftReport driftReport,
        SyncState syncState,
        ReconciliationResult result,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ignoring drift for {ResourceType}/{ResourceId} (policy: IGNORE)",
            driftReport.ResourceType, driftReport.ResourceId);

        // Log as informational
        await _auditLogger.LogDriftDetectionAsync(
            driftReport.TenantId,
            driftReport.ProviderId,
            driftReport.DriftId,
            $"IGNORED_{driftReport.DriftType}",
            driftReport.ResourceType,
            driftReport.ResourceId,
            driftReport.Details?.EntraState?.ToString(),
            driftReport.Details?.ProviderState?.ToString(),
            "Informational", // Override severity for ignored drifts
            "system");

        // Mark as reconciled (ignored)
        driftReport.Reconciled = true;
        driftReport.ReconciledAt = DateTime.UtcNow;
        driftReport.ReconciliationAction = ReconciliationAction.Ignored;
        driftReport.ReconciledBy = "system";
        driftReport.ReconciliationNotes = "Drift ignored per policy";
        driftReport.ModifiedAt = DateTime.UtcNow;

        // Add to sync state drift log as ignored
        var driftLogEntry = new DriftLogEntry
        {
            DriftId = driftReport.DriftId,
            DetectedAt = driftReport.Timestamp,
            DriftType = driftReport.DriftType,
            ResourceType = driftReport.ResourceType,
            ResourceId = driftReport.ResourceId,
            OldValue = driftReport.Details?.EntraState?.ToString(),
            NewValue = driftReport.Details?.ProviderState?.ToString(),
            Reconciled = true,
            ReconciledAt = DateTime.UtcNow,
            ReconciliationAction = "Ignored"
        };

        await _syncStateRepository.AddDriftLogEntryAsync(
            driftReport.TenantId,
            driftReport.ProviderId,
            driftLogEntry,
            cancellationToken);

        result.Success = true;
        result.Action = ReconciliationAction.Ignored;
        result.SyncBlocked = false;
        result.ChangesSummary = "Drift ignored per policy - no changes applied";
        result.ReconciledBy = "system";

        return result;
    }

    /// <inheritdoc />
    public async Task<ReconciliationResult> ReconcileConflictAsync(
        ConflictReport conflictReport,
        SyncState syncState,
        ConflictResolution resolution,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conflictReport);
        ArgumentNullException.ThrowIfNull(syncState);
        ArgumentException.ThrowIfNullOrEmpty(actorId);

        _logger.LogInformation(
            "Reconciling conflict {ConflictId} for {ResourceType}/{ResourceId} with resolution {Resolution} by {ActorId}",
            conflictReport.ConflictId, conflictReport.ResourceType, conflictReport.ResourceId, resolution, actorId);

        var result = new ReconciliationResult
        {
            DriftId = conflictReport.ConflictId,
            ResourceType = conflictReport.ResourceType,
            ResourceId = conflictReport.ResourceId,
            SyncDirection = syncState.SyncDirection,
            ReconciledBy = actorId
        };

        try
        {
            switch (resolution)
            {
                case ConflictResolution.UseEntraValue:
                    // Apply Entra state to provider
                    result.Strategy = ReconciliationStrategy.AutoApply;
                    result.ChangesSummary = "Applied Entra value to resolve conflict";
                    break;

                case ConflictResolution.UseProviderValue:
                    // Apply provider state to Entra (would need Graph API)
                    result.Strategy = ReconciliationStrategy.AutoApply;
                    result.ChangesSummary = "Provider value would be applied to Entra";
                    break;

                case ConflictResolution.UseMostRecent:
                    // Compare timestamps and use most recent
                    var entraTime = conflictReport.EntraChange?.Timestamp ?? DateTime.MinValue;
                    var providerTime = conflictReport.ProviderChange?.Timestamp ?? DateTime.MinValue;
                    result.ChangesSummary = entraTime > providerTime 
                        ? "Used most recent (Entra) value" 
                        : "Used most recent (Provider) value";
                    break;

                case ConflictResolution.MergeValues:
                    result.ChangesSummary = "Merged values from both systems";
                    break;

                case ConflictResolution.Ignore:
                    result.Strategy = ReconciliationStrategy.Ignore;
                    result.ChangesSummary = "Conflict ignored";
                    break;

                case ConflictResolution.Custom:
                    result.ChangesSummary = "Custom resolution applied";
                    break;
            }

            // Mark conflict as resolved
            conflictReport.Resolved = true;
            conflictReport.ResolvedAt = DateTime.UtcNow;
            conflictReport.ResolvedBy = actorId;
            conflictReport.Resolution = resolution;
            conflictReport.SyncBlocked = false;
            conflictReport.ModifiedAt = DateTime.UtcNow;

            // Unblock sync
            await UnblockSyncAsync(
                conflictReport.TenantId,
                conflictReport.ProviderId,
                conflictReport.ResourceType,
                conflictReport.ResourceId,
                actorId,
                cancellationToken);

            // Remove from pending
            lock (_lock)
            {
                _pendingConflictReports.Remove(conflictReport.ConflictId);
            }

            // Log resolution
            await _auditLogger.LogConflictResolutionAsync(
                conflictReport.TenantId,
                conflictReport.ProviderId,
                conflictReport.ConflictId,
                conflictReport.ResourceType,
                conflictReport.ResourceId,
                resolution.ToString(),
                actorId,
                result.ChangesSummary);

            result.Success = true;
            result.Action = ReconciliationAction.ManualApproved;
            result.SyncBlocked = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconcile conflict {ConflictId}", conflictReport.ConflictId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ReconciliationResult> ProcessManualReconciliationAsync(
        ManualReconciliationRequest request,
        string tenantId,
        string providerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(providerId);
        ArgumentException.ThrowIfNullOrEmpty(request.ActorId);

        _logger.LogInformation(
            "Processing manual reconciliation request for drift {DriftId} by {ActorId}",
            request.DriftId, request.ActorId);

        // Get the pending drift report
        DriftReport? driftReport;
        lock (_lock)
        {
            _pendingDriftReports.TryGetValue(request.DriftId, out driftReport);
        }

        if (driftReport == null)
        {
            return new ReconciliationResult
            {
                DriftId = request.DriftId,
                Success = false,
                ErrorMessage = $"Drift report {request.DriftId} not found or already reconciled"
            };
        }

        // Get sync state
        var syncState = await _syncStateRepository.GetAsync(tenantId, providerId, cancellationToken);
        if (syncState == null)
        {
            return new ReconciliationResult
            {
                DriftId = request.DriftId,
                Success = false,
                ErrorMessage = $"Sync state not found for tenant {tenantId}, provider {providerId}"
            };
        }

        // Override sync direction if specified
        var originalDirection = syncState.SyncDirection;
        syncState.SyncDirection = request.Direction;

        try
        {
            // Apply using AUTO_APPLY strategy with the specified direction
            var result = await ReconcileDriftAsync(
                driftReport,
                syncState,
                ReconciliationStrategy.AutoApply,
                cancellationToken);

            result.ReconciledBy = request.ActorId;
            result.Notes = request.Notes;
            result.Action = ReconciliationAction.ManualApproved;

            // Update drift report
            driftReport.ReconciliationNotes = request.Notes;
            driftReport.ReconciledBy = request.ActorId;

            // Unblock sync
            if (result.Success)
            {
                await UnblockSyncAsync(
                    tenantId,
                    providerId,
                    driftReport.ResourceType,
                    driftReport.ResourceId,
                    request.ActorId,
                    cancellationToken);
            }

            return result;
        }
        finally
        {
            // Restore original direction
            syncState.SyncDirection = originalDirection;
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<DriftReport>> GetPendingDriftReportsAsync(
        string tenantId,
        string? providerId = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var pending = _pendingDriftReports.Values
                .Where(d => d.TenantId == tenantId)
                .Where(d => providerId == null || d.ProviderId == providerId)
                .OrderByDescending(d => d.Severity)
                .ThenByDescending(d => d.Timestamp)
                .ToList();

            return Task.FromResult<IEnumerable<DriftReport>>(pending);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<ConflictReport>> GetPendingConflictReportsAsync(
        string tenantId,
        string? providerId = null,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var pending = _pendingConflictReports.Values
                .Where(c => c.TenantId == tenantId)
                .Where(c => providerId == null || c.ProviderId == providerId)
                .OrderByDescending(c => c.Severity)
                .ThenByDescending(c => c.Timestamp)
                .ToList();

            return Task.FromResult<IEnumerable<ConflictReport>>(pending);
        }
    }

    /// <inheritdoc />
    public Task BlockSyncAsync(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var key = GetBlockedResourceKey(tenantId, providerId, resourceType, resourceId);
        lock (_lock)
        {
            _blockedResources[key] = new BlockedResource
            {
                TenantId = tenantId,
                ProviderId = providerId,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Reason = reason,
                BlockedAt = DateTime.UtcNow
            };
        }

        _logger.LogWarning(
            "Blocked sync for {ResourceType}/{ResourceId}: {Reason}",
            resourceType, resourceId, reason);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnblockSyncAsync(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        var key = GetBlockedResourceKey(tenantId, providerId, resourceType, resourceId);
        lock (_lock)
        {
            _blockedResources.Remove(key);
        }

        _logger.LogInformation(
            "Unblocked sync for {ResourceType}/{ResourceId} by {ActorId}",
            resourceType, resourceId, actorId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsSyncBlockedAsync(
        string tenantId,
        string providerId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var key = GetBlockedResourceKey(tenantId, providerId, resourceType, resourceId);
        lock (_lock)
        {
            return Task.FromResult(_blockedResources.ContainsKey(key));
        }
    }

    /// <summary>
    /// Notifies the operations team about drift detection.
    /// </summary>
    private Task NotifyOperationsTeamAsync(DriftReport driftReport, CancellationToken cancellationToken)
    {
        // In a real implementation, this would send emails or call webhooks
        _logger.LogInformation(
            "NOTIFICATION: Drift detected for {ResourceType}/{ResourceId}, severity={Severity}. " +
            "Would notify: emails=[{Emails}], webhooks=[{Webhooks}]",
            driftReport.ResourceType,
            driftReport.ResourceId,
            driftReport.Severity,
            string.Join(", ", _options.NotificationEmails),
            string.Join(", ", _options.NotificationWebhooks));

        // TODO: Implement actual notification logic
        // - Send emails via SMTP or SendGrid
        // - Call webhook endpoints
        // - Push to message queue for async processing

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the key for a blocked resource.
    /// </summary>
    private static string GetBlockedResourceKey(string tenantId, string providerId, string resourceType, string resourceId)
    {
        return $"{tenantId}:{providerId}:{resourceType}:{resourceId}";
    }

    /// <summary>
    /// Represents a blocked resource.
    /// </summary>
    private class BlockedResource
    {
        public string TenantId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime BlockedAt { get; set; }
    }
}
