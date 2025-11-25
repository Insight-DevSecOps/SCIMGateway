// ==========================================================================
// T029: ConnectionPool - HTTP Connection Pool Management
// ==========================================================================
// Manages HTTP client pooling for adapter communications
// ==========================================================================

using System.Collections.Concurrent;
using System.Diagnostics;

namespace SCIMGateway.Core.Http;

/// <summary>
/// Interface for HTTP connection pool.
/// </summary>
public interface IConnectionPool : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets an HTTP client for an adapter.
    /// </summary>
    Task<HttpClient> GetClientAsync(string adapterId);

    /// <summary>
    /// Releases a client back to the pool.
    /// </summary>
    void ReleaseClient(string adapterId, HttpClient client);

    /// <summary>
    /// Returns a client to the pool (alias for ReleaseClient).
    /// </summary>
    void ReturnClient(string adapterId, HttpClient client);

    /// <summary>
    /// Gets pool statistics.
    /// </summary>
    ConnectionPoolStatistics GetStatistics();

    /// <summary>
    /// Clears all pooled connections.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Clears all pooled connections (synchronous).
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks if the pool is healthy.
    /// </summary>
    bool IsHealthy { get; }

    /// <summary>
    /// Checks pool health asynchronously.
    /// </summary>
    Task<bool> CheckHealthAsync();
}

/// <summary>
/// Connection pool configuration options.
/// </summary>
public class ConnectionPoolOptions
{
    /// <summary>
    /// Maximum connections per adapter.
    /// </summary>
    public int MaxConnectionsPerAdapter { get; set; } = 10;

    /// <summary>
    /// Maximum connections per host (alias).
    /// </summary>
    public int MaxConnectionsPerHost => MaxConnectionsPerAdapter;

    /// <summary>
    /// How long idle connections are kept alive.
    /// </summary>
    public TimeSpan ConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Alias for ConnectionIdleTimeout.
    /// </summary>
    public TimeSpan IdleTimeout => ConnectionIdleTimeout;

    /// <summary>
    /// Maximum lifetime of a connection before recycling.
    /// </summary>
    public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Alias for ConnectionLifetime.
    /// </summary>
    public TimeSpan PooledConnectionLifetime => ConnectionLifetime;

    /// <summary>
    /// Timeout for establishing new connections.
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Timeout for individual requests.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Alias for RequestTimeout.
    /// </summary>
    public TimeSpan Timeout => RequestTimeout;

    /// <summary>
    /// Per-adapter connection options.
    /// </summary>
    public Dictionary<string, AdapterConnectionOptions> AdapterOptions { get; set; } = [];
}

/// <summary>
/// Per-adapter connection options.
/// </summary>
public class AdapterConnectionOptions
{
    /// <summary>
    /// Adapter endpoint base URI.
    /// </summary>
    public Uri? BaseUri { get; set; }

    /// <summary>
    /// Alias for BaseUri.
    /// </summary>
    public Uri? BaseUrl => BaseUri;

    /// <summary>
    /// Maximum connections for this adapter.
    /// </summary>
    public int MaxConnections { get; set; } = 10;

    /// <summary>
    /// Custom timeout for this adapter.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}

/// <summary>
/// Connection pool statistics for monitoring.
/// </summary>
public class ConnectionPoolStatistics
{
    /// <summary>
    /// Currently in-use connections.
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Available pooled connections.
    /// </summary>
    public int IdleConnections { get; set; }

    /// <summary>
    /// Total requests served.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Requests served from pool.
    /// </summary>
    public long PoolHits { get; set; }

    /// <summary>
    /// Percentage of requests served from pool.
    /// </summary>
    public double PoolHitRate => TotalRequests > 0 ? (double)PoolHits / TotalRequests * 100 : 0;

    /// <summary>
    /// Alias for PoolHitRate.
    /// </summary>
    public double HitRate => PoolHitRate;

    /// <summary>
    /// Total connections created.
    /// </summary>
    public long TotalConnectionsCreated { get; set; }

    /// <summary>
    /// Connections recycled due to lifetime.
    /// </summary>
    public long ConnectionsRecycled { get; set; }

    /// <summary>
    /// Statistics per adapter.
    /// </summary>
    public Dictionary<string, AdapterPoolStatistics> AdapterStatistics { get; set; } = [];
}

/// <summary>
/// Per-adapter pool statistics.
/// </summary>
public class AdapterPoolStatistics
{
    /// <summary>
    /// Adapter identifier.
    /// </summary>
    public string AdapterId { get; set; } = string.Empty;

    /// <summary>
    /// Active connections for this adapter.
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Idle connections for this adapter.
    /// </summary>
    public int IdleConnections { get; set; }

    /// <summary>
    /// Total requests for this adapter.
    /// </summary>
    public long TotalRequests { get; set; }
}

/// <summary>
/// Pooled connection wrapper.
/// </summary>
internal class PooledConnection
{
    public HttpClient Client { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastUsedAt { get; set; }
    public bool IsInUse { get; set; }

    public PooledConnection(HttpClient client)
    {
        Client = client;
        CreatedAt = DateTimeOffset.UtcNow;
        LastUsedAt = DateTimeOffset.UtcNow;
        IsInUse = false;
    }

    public bool IsExpired(TimeSpan lifetime) => DateTimeOffset.UtcNow - CreatedAt > lifetime;
    public bool IsIdle(TimeSpan idleTimeout) => !IsInUse && DateTimeOffset.UtcNow - LastUsedAt > idleTimeout;
}

/// <summary>
/// HTTP connection pool implementation.
/// </summary>
public class ConnectionPool : IConnectionPool
{
    private readonly ConnectionPoolOptions _options;
    private readonly ConcurrentDictionary<string, List<PooledConnection>> _pools = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly object _lock = new();
    private long _totalRequests;
    private long _poolHits;
    private long _totalConnectionsCreated;
    private long _connectionsRecycled;
    private bool _disposed;
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// Initializes a new instance of ConnectionPool.
    /// </summary>
    public ConnectionPool(ConnectionPoolOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        // Start cleanup timer
        _cleanupTimer = new Timer(CleanupCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc />
    public bool IsHealthy => !_disposed;

    /// <inheritdoc />
    public async Task<HttpClient> GetClientAsync(string adapterId)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ConnectionPool));

        Interlocked.Increment(ref _totalRequests);

        var semaphore = _semaphores.GetOrAdd(adapterId, _ => 
            new SemaphoreSlim(GetMaxConnections(adapterId)));

        await semaphore.WaitAsync();

        try
        {
            var pool = _pools.GetOrAdd(adapterId, _ => []);

            lock (pool)
            {
                // Try to find an available connection
                var connection = pool.FirstOrDefault(c => !c.IsInUse && !c.IsExpired(_options.ConnectionLifetime));
                if (connection != null)
                {
                    connection.IsInUse = true;
                    connection.LastUsedAt = DateTimeOffset.UtcNow;
                    Interlocked.Increment(ref _poolHits);
                    return connection.Client;
                }

                // Create new connection
                var client = CreateClient(adapterId);
                var newConnection = new PooledConnection(client) { IsInUse = true };
                pool.Add(newConnection);
                Interlocked.Increment(ref _totalConnectionsCreated);
                return client;
            }
        }
        catch
        {
            semaphore.Release();
            throw;
        }
    }

    /// <inheritdoc />
    public void ReleaseClient(string adapterId, HttpClient client)
    {
        ReturnClient(adapterId, client);
    }

    /// <inheritdoc />
    public void ReturnClient(string adapterId, HttpClient client)
    {
        if (_disposed) return;

        if (_pools.TryGetValue(adapterId, out var pool))
        {
            lock (pool)
            {
                var connection = pool.FirstOrDefault(c => c.Client == client);
                if (connection != null)
                {
                    if (connection.IsExpired(_options.ConnectionLifetime))
                    {
                        // Recycle expired connection
                        pool.Remove(connection);
                        connection.Client.Dispose();
                        Interlocked.Increment(ref _connectionsRecycled);
                    }
                    else
                    {
                        connection.IsInUse = false;
                        connection.LastUsedAt = DateTimeOffset.UtcNow;
                    }
                }
            }
        }

        if (_semaphores.TryGetValue(adapterId, out var semaphore))
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc />
    public ConnectionPoolStatistics GetStatistics()
    {
        var stats = new ConnectionPoolStatistics
        {
            TotalRequests = Interlocked.Read(ref _totalRequests),
            PoolHits = Interlocked.Read(ref _poolHits),
            TotalConnectionsCreated = Interlocked.Read(ref _totalConnectionsCreated),
            ConnectionsRecycled = Interlocked.Read(ref _connectionsRecycled)
        };

        foreach (var kvp in _pools)
        {
            var adapterId = kvp.Key;
            var pool = kvp.Value;

            int active, idle;
            lock (pool)
            {
                active = pool.Count(c => c.IsInUse);
                idle = pool.Count(c => !c.IsInUse);
            }

            stats.ActiveConnections += active;
            stats.IdleConnections += idle;

            stats.AdapterStatistics[adapterId] = new AdapterPoolStatistics
            {
                AdapterId = adapterId,
                ActiveConnections = active,
                IdleConnections = idle
            };
        }

        return stats;
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Clear()
    {
        foreach (var kvp in _pools)
        {
            var pool = kvp.Value;
            lock (pool)
            {
                foreach (var connection in pool)
                {
                    connection.Client.Dispose();
                }
                pool.Clear();
            }
        }
        _pools.Clear();
    }

    /// <inheritdoc />
    public Task<bool> CheckHealthAsync()
    {
        return Task.FromResult(IsHealthy);
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
            _cleanupTimer.Dispose();
            Clear();
            
            foreach (var semaphore in _semaphores.Values)
            {
                semaphore.Dispose();
            }
            _semaphores.Clear();
        }

        _disposed = true;
    }

    /// <summary>
    /// Disposes resources asynchronously.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;

        await _cleanupTimer.DisposeAsync();
        Clear();
        
        foreach (var semaphore in _semaphores.Values)
        {
            semaphore.Dispose();
        }
        _semaphores.Clear();

        _disposed = true;
    }

    private HttpClient CreateClient(string adapterId)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = _options.ConnectionLifetime,
            PooledConnectionIdleTimeout = _options.ConnectionIdleTimeout,
            ConnectTimeout = _options.ConnectTimeout,
            MaxConnectionsPerServer = GetMaxConnections(adapterId)
        };

        var client = new HttpClient(handler)
        {
            Timeout = GetTimeout(adapterId)
        };

        // Set base address if configured
        if (_options.AdapterOptions.TryGetValue(adapterId, out var adapterOptions) && adapterOptions.BaseUri != null)
        {
            client.BaseAddress = adapterOptions.BaseUri;
        }

        return client;
    }

    private int GetMaxConnections(string adapterId)
    {
        if (_options.AdapterOptions.TryGetValue(adapterId, out var adapterOptions))
        {
            return adapterOptions.MaxConnections;
        }
        return _options.MaxConnectionsPerAdapter;
    }

    private TimeSpan GetTimeout(string adapterId)
    {
        if (_options.AdapterOptions.TryGetValue(adapterId, out var adapterOptions) && adapterOptions.Timeout.HasValue)
        {
            return adapterOptions.Timeout.Value;
        }
        return _options.RequestTimeout;
    }

    private void CleanupCallback(object? state)
    {
        if (_disposed) return;

        foreach (var kvp in _pools)
        {
            var pool = kvp.Value;
            lock (pool)
            {
                var toRemove = pool.Where(c => 
                    !c.IsInUse && (c.IsExpired(_options.ConnectionLifetime) || c.IsIdle(_options.ConnectionIdleTimeout)))
                    .ToList();

                foreach (var connection in toRemove)
                {
                    pool.Remove(connection);
                    connection.Client.Dispose();
                    Interlocked.Increment(ref _connectionsRecycled);
                }
            }
        }
    }
}

/// <summary>
/// Exception for connection timeout.
/// </summary>
public class ConnectionTimeoutException : TimeoutException
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public ConnectionTimeoutException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with inner exception.
    /// </summary>
    public ConnectionTimeoutException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception for request timeout.
/// </summary>
public class RequestTimeoutException : TaskCanceledException
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public RequestTimeoutException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with inner exception.
    /// </summary>
    public RequestTimeoutException(string message, Exception innerException) : base(message, innerException) { }
}
