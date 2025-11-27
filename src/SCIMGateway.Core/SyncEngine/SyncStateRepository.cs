// ==========================================================================
// T120: SyncStateRepository - Cosmos DB Sync State Repository
// ==========================================================================
// Implements sync state CRUD operations using Azure Cosmos DB
// ==========================================================================

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Data;
using SCIMGateway.Core.Models;
using System.Net;

namespace SCIMGateway.Core.SyncEngine;

/// <summary>
/// Cosmos DB implementation of sync state repository.
/// </summary>
public class SyncStateRepository : ISyncStateRepository
{
    private readonly ICosmosDbClient _cosmosClient;
    private readonly ILogger<SyncStateRepository> _logger;

    public SyncStateRepository(
        ICosmosDbClient cosmosClient,
        ILogger<SyncStateRepository> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SyncState?> GetAsync(string tenantId, string providerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentException.ThrowIfNullOrEmpty(providerId);

        var container = _cosmosClient.GetSyncStateContainer();
        var query = $"SELECT * FROM c WHERE c.tenantId = @tenantId AND c.providerId = @providerId";
        
        var results = await _cosmosClient.QueryItemsAsync<SyncState>(container, 
            $"SELECT * FROM c WHERE c.tenantId = '{tenantId}' AND c.providerId = '{providerId}'", 
            tenantId);
        
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<SyncState?> GetByIdAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetSyncStateContainer();
        return await _cosmosClient.ReadItemAsync<SyncState>(container, id, tenantId);
    }

    /// <inheritdoc />
    public async Task<SyncState> CreateAsync(SyncState syncState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(syncState);
        ArgumentException.ThrowIfNullOrEmpty(syncState.TenantId);

        // Generate ID if not provided
        if (string.IsNullOrEmpty(syncState.Id))
        {
            syncState.Id = $"{syncState.TenantId}_{syncState.ProviderId}";
        }

        syncState.CreatedAt = DateTime.UtcNow;
        syncState.ModifiedAt = DateTime.UtcNow;

        var container = _cosmosClient.GetSyncStateContainer();
        
        try
        {
            var created = await _cosmosClient.CreateItemAsync(container, syncState, syncState.TenantId);
            _logger.LogInformation("Created sync state for tenant {TenantId}, provider {ProviderId}", 
                syncState.TenantId, syncState.ProviderId);
            return created;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Sync state already exists for tenant {TenantId}, provider {ProviderId}", 
                syncState.TenantId, syncState.ProviderId);
            throw new InvalidOperationException($"Sync state already exists for provider {syncState.ProviderId}");
        }
    }

    /// <inheritdoc />
    public async Task<SyncState> UpdateAsync(SyncState syncState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(syncState);
        ArgumentException.ThrowIfNullOrEmpty(syncState.Id);
        ArgumentException.ThrowIfNullOrEmpty(syncState.TenantId);

        syncState.ModifiedAt = DateTime.UtcNow;

        var container = _cosmosClient.GetSyncStateContainer();
        var updated = await _cosmosClient.UpsertItemAsync(container, syncState, syncState.TenantId);
        
        _logger.LogDebug("Updated sync state {Id} for tenant {TenantId}", syncState.Id, syncState.TenantId);
        return updated;
    }

    /// <inheritdoc />
    public async Task<SyncState> UpsertAsync(SyncState syncState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(syncState);
        ArgumentException.ThrowIfNullOrEmpty(syncState.TenantId);

        // Generate ID if not provided
        if (string.IsNullOrEmpty(syncState.Id))
        {
            syncState.Id = $"{syncState.TenantId}_{syncState.ProviderId}";
        }

        var existing = await GetByIdAsync(syncState.Id, syncState.TenantId, cancellationToken);
        if (existing == null)
        {
            syncState.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            syncState.CreatedAt = existing.CreatedAt;
        }
        
        syncState.ModifiedAt = DateTime.UtcNow;

        var container = _cosmosClient.GetSyncStateContainer();
        var upserted = await _cosmosClient.UpsertItemAsync(container, syncState, syncState.TenantId);
        
        _logger.LogDebug("Upserted sync state {Id} for tenant {TenantId}", syncState.Id, syncState.TenantId);
        return upserted;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetSyncStateContainer();
        
        try
        {
            await _cosmosClient.DeleteItemAsync(container, id, tenantId);
            _logger.LogInformation("Deleted sync state {Id} for tenant {TenantId}", id, tenantId);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SyncState>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetSyncStateContainer();
        return await _cosmosClient.QueryItemsAsync<SyncState>(container, 
            $"SELECT * FROM c WHERE c.tenantId = '{tenantId}'", 
            tenantId);
    }

    /// <inheritdoc />
    public async Task<SyncState> AddDriftLogEntryAsync(string tenantId, string providerId, DriftLogEntry entry, CancellationToken cancellationToken = default)
    {
        var syncState = await GetOrCreateAsync(tenantId, providerId, cancellationToken);
        
        syncState.DriftLog ??= new List<DriftLogEntry>();
        syncState.DriftLog.Add(entry);
        syncState.ModifiedAt = DateTime.UtcNow;

        return await UpdateAsync(syncState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SyncState> AddConflictLogEntryAsync(string tenantId, string providerId, ConflictLogEntry entry, CancellationToken cancellationToken = default)
    {
        var syncState = await GetOrCreateAsync(tenantId, providerId, cancellationToken);
        
        syncState.ConflictLog ??= new List<ConflictLogEntry>();
        syncState.ConflictLog.Add(entry);
        syncState.ModifiedAt = DateTime.UtcNow;

        return await UpdateAsync(syncState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SyncState> AddErrorLogEntryAsync(string tenantId, string providerId, SyncErrorEntry entry, CancellationToken cancellationToken = default)
    {
        var syncState = await GetOrCreateAsync(tenantId, providerId, cancellationToken);
        
        syncState.ErrorLog ??= new List<SyncErrorEntry>();
        syncState.ErrorLog.Add(entry);
        syncState.ModifiedAt = DateTime.UtcNow;

        return await UpdateAsync(syncState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SyncState> UpdateStatusAsync(string tenantId, string providerId, SyncStatus status, CancellationToken cancellationToken = default)
    {
        var syncState = await GetOrCreateAsync(tenantId, providerId, cancellationToken);
        
        syncState.Status = status;
        syncState.ModifiedAt = DateTime.UtcNow;

        _logger.LogInformation("Updated sync state status to {Status} for tenant {TenantId}, provider {ProviderId}", 
            status, tenantId, providerId);

        return await UpdateAsync(syncState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SyncState> UpdateLastKnownStateAsync(string tenantId, string providerId, string stateHash, CancellationToken cancellationToken = default)
    {
        var syncState = await GetOrCreateAsync(tenantId, providerId, cancellationToken);
        
        syncState.LastKnownState = stateHash;
        syncState.ModifiedAt = DateTime.UtcNow;

        return await UpdateAsync(syncState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SyncState> RecordSyncCompletionAsync(string tenantId, string providerId, SyncSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var syncState = await GetOrCreateAsync(tenantId, providerId, cancellationToken);
        
        syncState.LastSyncTimestamp = snapshot.Timestamp;
        syncState.SnapshotChecksum = snapshot.Checksum;
        syncState.SnapshotTimestamp = snapshot.Timestamp;
        syncState.UserCount = snapshot.UserCount;
        syncState.GroupCount = snapshot.GroupCount;
        syncState.Status = snapshot.Status;
        syncState.ModifiedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Recorded sync completion for tenant {TenantId}, provider {ProviderId}: {UserCount} users, {GroupCount} groups", 
            tenantId, providerId, snapshot.UserCount, snapshot.GroupCount);

        return await UpdateAsync(syncState, cancellationToken);
    }

    /// <summary>
    /// Gets an existing sync state or creates a new one.
    /// </summary>
    private async Task<SyncState> GetOrCreateAsync(string tenantId, string providerId, CancellationToken cancellationToken)
    {
        var syncState = await GetAsync(tenantId, providerId, cancellationToken);
        
        if (syncState == null)
        {
            syncState = new SyncState
            {
                Id = $"{tenantId}_{providerId}",
                TenantId = tenantId,
                ProviderId = providerId,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            var container = _cosmosClient.GetSyncStateContainer();
            syncState = await _cosmosClient.CreateItemAsync(container, syncState, tenantId);
            
            _logger.LogInformation("Created new sync state for tenant {TenantId}, provider {ProviderId}", 
                tenantId, providerId);
        }

        return syncState;
    }
}
