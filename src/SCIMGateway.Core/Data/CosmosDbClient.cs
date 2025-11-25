// ==========================================================================
// T027: CosmosDbClient - Azure Cosmos DB Integration
// ==========================================================================
// Provides Cosmos DB operations with managed identity authentication
// ==========================================================================

using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace SCIMGateway.Core.Data;

/// <summary>
/// Interface for Cosmos DB client operations.
/// </summary>
public interface ICosmosDbClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the users container.
    /// </summary>
    Container GetUsersContainer();

    /// <summary>
    /// Gets the groups container.
    /// </summary>
    Container GetGroupsContainer();

    /// <summary>
    /// Gets the sync-state container.
    /// </summary>
    Container GetSyncStateContainer();

    /// <summary>
    /// Gets the transformation-rules container.
    /// </summary>
    Container GetTransformationRulesContainer();

    /// <summary>
    /// Gets the audit-logs container.
    /// </summary>
    Container GetAuditLogsContainer();

    /// <summary>
    /// Creates an item in a container.
    /// </summary>
    Task<T> CreateItemAsync<T>(Container container, T item, string partitionKey);

    /// <summary>
    /// Reads an item from a container.
    /// </summary>
    Task<T?> ReadItemAsync<T>(Container container, string id, string partitionKey);

    /// <summary>
    /// Upserts an item in a container.
    /// </summary>
    Task<T> UpsertItemAsync<T>(Container container, T item, string partitionKey);

    /// <summary>
    /// Deletes an item from a container.
    /// </summary>
    Task DeleteItemAsync(Container container, string id, string partitionKey);

    /// <summary>
    /// Queries items in a container.
    /// </summary>
    Task<List<T>> QueryItemsAsync<T>(Container container, string query, string? partitionKey = null);

    /// <summary>
    /// Checks database health.
    /// </summary>
    Task<bool> CheckHealthAsync();
}

/// <summary>
/// Cosmos DB configuration options.
/// </summary>
public class CosmosDbOptions
{
    /// <summary>
    /// Cosmos DB account endpoint.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Database name.
    /// </summary>
    public string DatabaseName { get; set; } = "scimgateway";

    /// <summary>
    /// Use managed identity authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Account key (only used if not using managed identity).
    /// </summary>
    public string? AccountKey { get; set; }

    /// <summary>
    /// Connection string (alternative to endpoint/key).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Application region.
    /// </summary>
    public string? ApplicationRegion { get; set; }

    /// <summary>
    /// Maximum retry attempts.
    /// </summary>
    public int MaxRetryAttemptsOnRateLimitedRequests { get; set; } = 9;

    /// <summary>
    /// Maximum retry wait time.
    /// </summary>
    public TimeSpan MaxRetryWaitTimeOnRateLimitedRequests { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Constants for Cosmos DB container names.
/// </summary>
public static class CosmosContainerNames
{
    /// <summary>Users container name.</summary>
    public const string Users = "users";

    /// <summary>Groups container name.</summary>
    public const string Groups = "groups";

    /// <summary>Sync state container name.</summary>
    public const string SyncState = "sync-state";

    /// <summary>Transformation rules container name.</summary>
    public const string TransformationRules = "transformation-rules";

    /// <summary>Audit logs container name.</summary>
    public const string AuditLogs = "audit-logs";
}

/// <summary>
/// Azure Cosmos DB client implementation.
/// </summary>
public class CosmosDbClient : ICosmosDbClient
{
    private readonly CosmosClient _cosmosClient;
    private readonly Database _database;
    private readonly CosmosDbOptions _options;
    private bool _disposed;

    // Container cache
    private Container? _usersContainer;
    private Container? _groupsContainer;
    private Container? _syncStateContainer;
    private Container? _transformationRulesContainer;
    private Container? _auditLogsContainer;

    /// <summary>
    /// Initializes a new instance of CosmosDbClient.
    /// </summary>
    public CosmosDbClient(CosmosDbOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var clientOptions = new CosmosClientOptions
        {
            MaxRetryAttemptsOnRateLimitedRequests = options.MaxRetryAttemptsOnRateLimitedRequests,
            MaxRetryWaitTimeOnRateLimitedRequests = options.MaxRetryWaitTimeOnRateLimitedRequests,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        if (!string.IsNullOrEmpty(options.ApplicationRegion))
        {
            clientOptions.ApplicationRegion = options.ApplicationRegion;
        }

        if (!string.IsNullOrEmpty(options.ConnectionString))
        {
            _cosmosClient = new CosmosClient(options.ConnectionString, clientOptions);
        }
        else if (options.UseManagedIdentity)
        {
            var credential = new DefaultAzureCredential();
            _cosmosClient = new CosmosClient(options.Endpoint, credential, clientOptions);
        }
        else
        {
            _cosmosClient = new CosmosClient(options.Endpoint, options.AccountKey, clientOptions);
        }

        _database = _cosmosClient.GetDatabase(options.DatabaseName);
    }

    /// <inheritdoc />
    public Container GetUsersContainer()
    {
        return _usersContainer ??= _database.GetContainer(CosmosContainerNames.Users);
    }

    /// <inheritdoc />
    public Container GetGroupsContainer()
    {
        return _groupsContainer ??= _database.GetContainer(CosmosContainerNames.Groups);
    }

    /// <inheritdoc />
    public Container GetSyncStateContainer()
    {
        return _syncStateContainer ??= _database.GetContainer(CosmosContainerNames.SyncState);
    }

    /// <inheritdoc />
    public Container GetTransformationRulesContainer()
    {
        return _transformationRulesContainer ??= _database.GetContainer(CosmosContainerNames.TransformationRules);
    }

    /// <inheritdoc />
    public Container GetAuditLogsContainer()
    {
        return _auditLogsContainer ??= _database.GetContainer(CosmosContainerNames.AuditLogs);
    }

    /// <inheritdoc />
    public async Task<T> CreateItemAsync<T>(Container container, T item, string partitionKey)
    {
        var response = await container.CreateItemAsync(
            item,
            new PartitionKey(partitionKey));
        return response.Resource;
    }

    /// <inheritdoc />
    public async Task<T?> ReadItemAsync<T>(Container container, string id, string partitionKey)
    {
        try
        {
            var response = await container.ReadItemAsync<T>(
                id,
                new PartitionKey(partitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    /// <inheritdoc />
    public async Task<T> UpsertItemAsync<T>(Container container, T item, string partitionKey)
    {
        var response = await container.UpsertItemAsync(
            item,
            new PartitionKey(partitionKey));
        return response.Resource;
    }

    /// <inheritdoc />
    public async Task DeleteItemAsync(Container container, string id, string partitionKey)
    {
        await container.DeleteItemAsync<object>(
            id,
            new PartitionKey(partitionKey));
    }

    /// <inheritdoc />
    public async Task<List<T>> QueryItemsAsync<T>(Container container, string query, string? partitionKey = null)
    {
        var queryDefinition = new QueryDefinition(query);
        var queryRequestOptions = new QueryRequestOptions();

        if (!string.IsNullOrEmpty(partitionKey))
        {
            queryRequestOptions.PartitionKey = new PartitionKey(partitionKey);
        }

        var results = new List<T>();
        using var iterator = container.GetItemQueryIterator<T>(queryDefinition, requestOptions: queryRequestOptions);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            await _database.ReadAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _cosmosClient.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Disposes resources asynchronously.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;

        _cosmosClient.Dispose();
        await Task.CompletedTask;

        _disposed = true;
    }
}
