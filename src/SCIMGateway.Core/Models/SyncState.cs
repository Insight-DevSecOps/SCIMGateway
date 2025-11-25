// ==========================================================================
// T021: SyncState Model
// ==========================================================================
// Model for tracking synchronization state between Entra ID and providers
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models;

/// <summary>
/// Synchronization state model for tracking sync operations.
/// </summary>
public class SyncState
{
    /// <summary>
    /// Unique identifier for this sync state record.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tenant identifier for multi-tenant isolation (partition key).
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Provider identifier.
    /// </summary>
    [JsonPropertyName("providerId")]
    [JsonProperty("providerId")]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the last successful sync.
    /// </summary>
    [JsonPropertyName("lastSyncTimestamp")]
    [JsonProperty("lastSyncTimestamp")]
    public DateTime? LastSyncTimestamp { get; set; }

    /// <summary>
    /// Current sync direction.
    /// </summary>
    [JsonPropertyName("syncDirection")]
    [JsonProperty("syncDirection")]
    public SyncDirection SyncDirection { get; set; } = SyncDirection.EntraToSaas;

    /// <summary>
    /// Last known state hash for change detection.
    /// </summary>
    [JsonPropertyName("lastKnownState")]
    [JsonProperty("lastKnownState")]
    public string? LastKnownState { get; set; }

    /// <summary>
    /// Current sync status.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public SyncStatus Status { get; set; } = SyncStatus.Idle;

    /// <summary>
    /// Snapshot checksum for drift detection.
    /// </summary>
    [JsonPropertyName("snapshotChecksum")]
    [JsonProperty("snapshotChecksum")]
    public string? SnapshotChecksum { get; set; }

    /// <summary>
    /// Timestamp of the last snapshot.
    /// </summary>
    [JsonPropertyName("snapshotTimestamp")]
    [JsonProperty("snapshotTimestamp")]
    public DateTime? SnapshotTimestamp { get; set; }

    /// <summary>
    /// Number of users in the last snapshot.
    /// </summary>
    [JsonPropertyName("userCount")]
    [JsonProperty("userCount")]
    public int UserCount { get; set; }

    /// <summary>
    /// Number of groups in the last snapshot.
    /// </summary>
    [JsonPropertyName("groupCount")]
    [JsonProperty("groupCount")]
    public int GroupCount { get; set; }

    /// <summary>
    /// Log of detected drift events.
    /// </summary>
    [JsonPropertyName("driftLog")]
    [JsonProperty("driftLog")]
    public List<DriftLogEntry>? DriftLog { get; set; }

    /// <summary>
    /// Log of detected conflicts.
    /// </summary>
    [JsonPropertyName("conflictLog")]
    [JsonProperty("conflictLog")]
    public List<ConflictLogEntry>? ConflictLog { get; set; }

    /// <summary>
    /// Log of sync errors.
    /// </summary>
    [JsonPropertyName("errorLog")]
    [JsonProperty("errorLog")]
    public List<SyncErrorEntry>? ErrorLog { get; set; }

    /// <summary>
    /// When this sync state record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this sync state record was last modified.
    /// </summary>
    [JsonPropertyName("modifiedAt")]
    [JsonProperty("modifiedAt")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Direction of synchronization.
/// </summary>
public enum SyncDirection
{
    /// <summary>
    /// Push changes from Entra ID to SaaS provider.
    /// </summary>
    EntraToSaas,

    /// <summary>
    /// Pull changes from SaaS provider to Entra ID.
    /// </summary>
    SaasToEntra
}

/// <summary>
/// Status of the sync operation.
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// No sync in progress.
    /// </summary>
    Idle,

    /// <summary>
    /// Sync is currently running.
    /// </summary>
    InProgress,

    /// <summary>
    /// Sync completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Sync completed with errors.
    /// </summary>
    CompletedWithErrors,

    /// <summary>
    /// Sync failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Sync is paused (e.g., for conflict resolution).
    /// </summary>
    Paused
}

/// <summary>
/// Entry in the drift log.
/// </summary>
public class DriftLogEntry
{
    /// <summary>
    /// Unique identifier for this drift entry.
    /// </summary>
    [JsonPropertyName("driftId")]
    [JsonProperty("driftId")]
    public string DriftId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When the drift was detected.
    /// </summary>
    [JsonPropertyName("detectedAt")]
    [JsonProperty("detectedAt")]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of drift.
    /// </summary>
    [JsonPropertyName("driftType")]
    [JsonProperty("driftType")]
    public DriftType DriftType { get; set; }

    /// <summary>
    /// Type of resource (User or Group).
    /// </summary>
    [JsonPropertyName("resourceType")]
    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected resource.
    /// </summary>
    [JsonPropertyName("resourceId")]
    [JsonProperty("resourceId")]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Previous value (for modifications).
    /// </summary>
    [JsonPropertyName("oldValue")]
    [JsonProperty("oldValue")]
    public string? OldValue { get; set; }

    /// <summary>
    /// New value (for modifications).
    /// </summary>
    [JsonPropertyName("newValue")]
    [JsonProperty("newValue")]
    public string? NewValue { get; set; }

    /// <summary>
    /// Whether this drift has been reconciled.
    /// </summary>
    [JsonPropertyName("reconciled")]
    [JsonProperty("reconciled")]
    public bool Reconciled { get; set; }

    /// <summary>
    /// When the drift was reconciled.
    /// </summary>
    [JsonPropertyName("reconciledAt")]
    [JsonProperty("reconciledAt")]
    public DateTime? ReconciledAt { get; set; }

    /// <summary>
    /// How the drift was reconciled.
    /// </summary>
    [JsonPropertyName("reconciliationAction")]
    [JsonProperty("reconciliationAction")]
    public string? ReconciliationAction { get; set; }
}

/// <summary>
/// Type of drift detected.
/// </summary>
public enum DriftType
{
    /// <summary>
    /// Resource was added in provider but not in Entra.
    /// </summary>
    Added,

    /// <summary>
    /// Resource was modified in provider.
    /// </summary>
    Modified,

    /// <summary>
    /// Resource was deleted in provider but exists in Entra.
    /// </summary>
    Deleted,

    /// <summary>
    /// Resource attributes differ between Entra and provider.
    /// </summary>
    AttributeMismatch,

    /// <summary>
    /// Group membership differs.
    /// </summary>
    MembershipMismatch
}

/// <summary>
/// Entry in the conflict log.
/// </summary>
public class ConflictLogEntry
{
    /// <summary>
    /// Unique identifier for this conflict.
    /// </summary>
    [JsonPropertyName("conflictId")]
    [JsonProperty("conflictId")]
    public string ConflictId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When the conflict was detected.
    /// </summary>
    [JsonPropertyName("detectedAt")]
    [JsonProperty("detectedAt")]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of conflict.
    /// </summary>
    [JsonPropertyName("conflictType")]
    [JsonProperty("conflictType")]
    public ConflictType ConflictType { get; set; }

    /// <summary>
    /// Type of resource.
    /// </summary>
    [JsonPropertyName("resourceType")]
    [JsonProperty("resourceType")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected resource.
    /// </summary>
    [JsonPropertyName("resourceId")]
    [JsonProperty("resourceId")]
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Change from Entra ID side.
    /// </summary>
    [JsonPropertyName("entraChange")]
    [JsonProperty("entraChange")]
    public string? EntraChange { get; set; }

    /// <summary>
    /// Change from provider side.
    /// </summary>
    [JsonPropertyName("providerChange")]
    [JsonProperty("providerChange")]
    public string? ProviderChange { get; set; }

    /// <summary>
    /// Suggested resolution.
    /// </summary>
    [JsonPropertyName("suggestedResolution")]
    [JsonProperty("suggestedResolution")]
    public string? SuggestedResolution { get; set; }

    /// <summary>
    /// How the conflict was resolved.
    /// </summary>
    [JsonPropertyName("resolution")]
    [JsonProperty("resolution")]
    public string? Resolution { get; set; }

    /// <summary>
    /// Whether this conflict has been resolved.
    /// </summary>
    [JsonPropertyName("resolved")]
    [JsonProperty("resolved")]
    public bool Resolved { get; set; }

    /// <summary>
    /// When the conflict was resolved.
    /// </summary>
    [JsonPropertyName("resolvedAt")]
    [JsonProperty("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Who resolved the conflict.
    /// </summary>
    [JsonPropertyName("resolvedBy")]
    [JsonProperty("resolvedBy")]
    public string? ResolvedBy { get; set; }
}

/// <summary>
/// Type of conflict.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Same resource modified in both Entra and provider.
    /// </summary>
    DualModification,

    /// <summary>
    /// Resource deleted in one system but modified in another.
    /// </summary>
    DeleteModifyConflict,

    /// <summary>
    /// Uniqueness constraint violation.
    /// </summary>
    UniquenessViolation,

    /// <summary>
    /// Transformation rule conflict.
    /// </summary>
    TransformationConflict
}

/// <summary>
/// Entry in the sync error log.
/// </summary>
public class SyncErrorEntry
{
    /// <summary>
    /// Unique identifier for this error.
    /// </summary>
    [JsonPropertyName("errorId")]
    [JsonProperty("errorId")]
    public string ErrorId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// When the error occurred.
    /// </summary>
    [JsonPropertyName("occurredAt")]
    [JsonProperty("occurredAt")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of resource being synced.
    /// </summary>
    [JsonPropertyName("resourceType")]
    [JsonProperty("resourceType")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// ID of the affected resource.
    /// </summary>
    [JsonPropertyName("resourceId")]
    [JsonProperty("resourceId")]
    public string? ResourceId { get; set; }

    /// <summary>
    /// Operation being performed.
    /// </summary>
    [JsonPropertyName("operation")]
    [JsonProperty("operation")]
    public string? Operation { get; set; }

    /// <summary>
    /// Error code.
    /// </summary>
    [JsonPropertyName("errorCode")]
    [JsonProperty("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [JsonProperty("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Stack trace (for debugging).
    /// </summary>
    [JsonPropertyName("stackTrace")]
    [JsonProperty("stackTrace")]
    public string? StackTrace { get; set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    [JsonPropertyName("retryCount")]
    [JsonProperty("retryCount")]
    public int RetryCount { get; set; }

    /// <summary>
    /// Whether this error is transient and may resolve on retry.
    /// </summary>
    [JsonPropertyName("isTransient")]
    [JsonProperty("isTransient")]
    public bool IsTransient { get; set; }
}
