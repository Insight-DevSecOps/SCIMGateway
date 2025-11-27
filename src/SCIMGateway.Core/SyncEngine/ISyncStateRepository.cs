// ==========================================================================
// T120: ISyncStateRepository - Sync State Repository Interface
// ==========================================================================
// Repository interface for sync state CRUD operations in Cosmos DB
// ==========================================================================

using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.SyncEngine;

/// <summary>
/// Repository interface for sync state operations.
/// </summary>
public interface ISyncStateRepository
{
    /// <summary>
    /// Gets a sync state by tenant and provider ID.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sync state, or null if not found.</returns>
    Task<SyncState?> GetAsync(string tenantId, string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a sync state by ID.
    /// </summary>
    /// <param name="id">The sync state ID.</param>
    /// <param name="tenantId">The tenant identifier (partition key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The sync state, or null if not found.</returns>
    Task<SyncState?> GetByIdAsync(string id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new sync state.
    /// </summary>
    /// <param name="syncState">The sync state to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created sync state.</returns>
    Task<SyncState> CreateAsync(SyncState syncState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing sync state.
    /// </summary>
    /// <param name="syncState">The sync state to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync state.</returns>
    Task<SyncState> UpdateAsync(SyncState syncState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a sync state (upsert).
    /// </summary>
    /// <param name="syncState">The sync state to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upserted sync state.</returns>
    Task<SyncState> UpsertAsync(SyncState syncState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a sync state.
    /// </summary>
    /// <param name="id">The sync state ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all sync states for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sync states.</returns>
    Task<IEnumerable<SyncState>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a drift log entry to a sync state.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="entry">The drift log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync state.</returns>
    Task<SyncState> AddDriftLogEntryAsync(string tenantId, string providerId, DriftLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a conflict log entry to a sync state.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="entry">The conflict log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync state.</returns>
    Task<SyncState> AddConflictLogEntryAsync(string tenantId, string providerId, ConflictLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an error log entry to a sync state.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="entry">The error log entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync state.</returns>
    Task<SyncState> AddErrorLogEntryAsync(string tenantId, string providerId, SyncErrorEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates sync state status.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync state.</returns>
    Task<SyncState> UpdateStatusAsync(string tenantId, string providerId, SyncStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last known state hash for change detection.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="stateHash">The new state hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync state.</returns>
    Task<SyncState> UpdateLastKnownStateAsync(string tenantId, string providerId, string stateHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a sync completion with snapshot details.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="snapshot">The snapshot details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated sync state.</returns>
    Task<SyncState> RecordSyncCompletionAsync(string tenantId, string providerId, SyncSnapshot snapshot, CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot details captured after a sync operation.
/// </summary>
public class SyncSnapshot
{
    /// <summary>
    /// Checksum of the synced state.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Number of users in the snapshot.
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Number of groups in the snapshot.
    /// </summary>
    public int GroupCount { get; set; }

    /// <summary>
    /// Final sync status.
    /// </summary>
    public SyncStatus Status { get; set; } = SyncStatus.Completed;

    /// <summary>
    /// Timestamp of the snapshot.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
