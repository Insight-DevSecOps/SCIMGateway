// ==========================================================================
// T074: AdapterRegistry - Adapter Registration and Routing
// ==========================================================================
// Registers adapters per provider and routes requests to correct adapter
// based on tenantId/providerId configuration
// ==========================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Interface for adapter registry operations.
/// </summary>
public interface IAdapterRegistry
{
    /// <summary>
    /// Registers an adapter for a specific provider.
    /// </summary>
    /// <param name="providerId">The unique provider identifier.</param>
    /// <param name="adapter">The adapter instance.</param>
    void RegisterAdapter(string providerId, IAdapter adapter);

    /// <summary>
    /// Gets an adapter by provider ID.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>The adapter instance.</returns>
    /// <exception cref="AdapterNotFoundException">Thrown when adapter is not found.</exception>
    IAdapter GetAdapter(string providerId);

    /// <summary>
    /// Tries to get an adapter by provider ID.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="adapter">The adapter instance if found.</param>
    /// <returns>True if adapter was found, false otherwise.</returns>
    bool TryGetAdapter(string providerId, out IAdapter? adapter);

    /// <summary>
    /// Gets an adapter for a specific tenant based on their configuration.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>The adapter instance.</returns>
    IAdapter GetAdapterForTenant(string tenantId, string providerId);

    /// <summary>
    /// Unregisters an adapter.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>True if adapter was removed, false if not found.</returns>
    bool UnregisterAdapter(string providerId);

    /// <summary>
    /// Gets all registered adapters.
    /// </summary>
    /// <returns>Collection of all registered adapters.</returns>
    IReadOnlyDictionary<string, IAdapter> GetAllAdapters();

    /// <summary>
    /// Checks if an adapter is registered for the given provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>True if adapter is registered.</returns>
    bool IsAdapterRegistered(string providerId);

    /// <summary>
    /// Refreshes or reloads adapter configuration.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    Task RefreshAdapterAsync(string providerId);
}

/// <summary>
/// Exception thrown when an adapter is not found in the registry.
/// </summary>
public class AdapterNotFoundException : Exception
{
    /// <summary>
    /// Gets the provider ID that was not found.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Gets the tenant ID associated with the request, if any.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterNotFoundException"/> class.
    /// </summary>
    public AdapterNotFoundException(string providerId)
        : base($"Adapter not found for provider: {providerId}")
    {
        ProviderId = providerId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterNotFoundException"/> class.
    /// </summary>
    public AdapterNotFoundException(string providerId, string tenantId)
        : base($"Adapter not found for provider '{providerId}' and tenant '{tenantId}'")
    {
        ProviderId = providerId;
        TenantId = tenantId;
    }
}

/// <summary>
/// Exception thrown when an adapter is disabled.
/// </summary>
public class AdapterDisabledException : Exception
{
    /// <summary>
    /// Gets the provider ID that is disabled.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterDisabledException"/> class.
    /// </summary>
    public AdapterDisabledException(string providerId)
        : base($"Adapter is disabled for provider: {providerId}")
    {
        ProviderId = providerId;
    }
}

/// <summary>
/// Registry for managing adapter instances.
/// Thread-safe implementation for concurrent access.
/// </summary>
public class AdapterRegistry : IAdapterRegistry
{
    private readonly ConcurrentDictionary<string, IAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, HashSet<string>> _tenantProviderMapping = new();
    private readonly ConcurrentDictionary<string, bool> _disabledAdapters = new();
    private readonly ILogger<AdapterRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterRegistry"/> class.
    /// </summary>
    public AdapterRegistry(ILogger<AdapterRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterRegistry"/> class.
    /// Parameterless constructor for testing.
    /// </summary>
    public AdapterRegistry()
    {
        _logger = new NullLogger<AdapterRegistry>();
    }

    /// <inheritdoc />
    public void RegisterAdapter(string providerId, IAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(adapter);

        _adapters[providerId] = adapter;
        _disabledAdapters.TryRemove(providerId, out _);

        _logger.LogInformation(
            "Registered adapter for provider {ProviderId}: {AdapterId} ({ProviderName})",
            providerId,
            adapter.AdapterId,
            adapter.ProviderName);
    }

    /// <inheritdoc />
    public IAdapter GetAdapter(string providerId)
    {
        ArgumentNullException.ThrowIfNull(providerId);

        if (_disabledAdapters.ContainsKey(providerId))
        {
            throw new AdapterDisabledException(providerId);
        }

        if (_adapters.TryGetValue(providerId, out var adapter))
        {
            return adapter;
        }

        _logger.LogWarning("Adapter not found for provider: {ProviderId}", providerId);
        throw new AdapterNotFoundException(providerId);
    }

    /// <inheritdoc />
    public bool TryGetAdapter(string providerId, out IAdapter? adapter)
    {
        adapter = null;

        if (string.IsNullOrEmpty(providerId))
        {
            return false;
        }

        if (_disabledAdapters.ContainsKey(providerId))
        {
            return false;
        }

        return _adapters.TryGetValue(providerId, out adapter);
    }

    /// <inheritdoc />
    public IAdapter GetAdapterForTenant(string tenantId, string providerId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(providerId);

        // Check if this tenant has access to this provider
        if (_tenantProviderMapping.TryGetValue(tenantId, out var allowedProviders))
        {
            if (!allowedProviders.Contains(providerId))
            {
                _logger.LogWarning(
                    "Tenant {TenantId} does not have access to provider {ProviderId}",
                    tenantId,
                    providerId);
                throw new AdapterNotFoundException(providerId, tenantId);
            }
        }

        return GetAdapter(providerId);
    }

    /// <inheritdoc />
    public bool UnregisterAdapter(string providerId)
    {
        ArgumentNullException.ThrowIfNull(providerId);

        var removed = _adapters.TryRemove(providerId, out var adapter);
        _disabledAdapters.TryRemove(providerId, out _);

        if (removed)
        {
            _logger.LogInformation(
                "Unregistered adapter for provider {ProviderId}: {AdapterId}",
                providerId,
                adapter?.AdapterId);
        }

        return removed;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IAdapter> GetAllAdapters()
    {
        return _adapters.Where(kvp => !_disabledAdapters.ContainsKey(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <inheritdoc />
    public bool IsAdapterRegistered(string providerId)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            return false;
        }

        return _adapters.ContainsKey(providerId) && !_disabledAdapters.ContainsKey(providerId);
    }

    /// <inheritdoc />
    public async Task RefreshAdapterAsync(string providerId)
    {
        ArgumentNullException.ThrowIfNull(providerId);

        if (_adapters.TryGetValue(providerId, out var adapter))
        {
            _logger.LogInformation("Refreshing adapter for provider {ProviderId}", providerId);

            // Check health after refresh
            var health = await adapter.CheckHealthAsync();
            
            _logger.LogInformation(
                "Adapter {ProviderId} health status after refresh: {Status}",
                providerId,
                health.Status);
        }
    }

    /// <summary>
    /// Configures which providers a tenant can access.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerIds">The allowed provider IDs.</param>
    public void ConfigureTenantProviders(string tenantId, IEnumerable<string> providerIds)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(providerIds);

        _tenantProviderMapping[tenantId] = new HashSet<string>(providerIds, StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Configured tenant {TenantId} with providers: {Providers}",
            tenantId,
            string.Join(", ", providerIds));
    }

    /// <summary>
    /// Disables an adapter temporarily.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    public void DisableAdapter(string providerId)
    {
        ArgumentNullException.ThrowIfNull(providerId);

        _disabledAdapters[providerId] = true;

        _logger.LogWarning("Disabled adapter for provider {ProviderId}", providerId);
    }

    /// <summary>
    /// Enables a previously disabled adapter.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    public void EnableAdapter(string providerId)
    {
        ArgumentNullException.ThrowIfNull(providerId);

        _disabledAdapters.TryRemove(providerId, out _);

        _logger.LogInformation("Enabled adapter for provider {ProviderId}", providerId);
    }

    /// <summary>
    /// Gets the count of registered adapters.
    /// </summary>
    public int Count => _adapters.Count;
}

/// <summary>
/// Null logger implementation for when no logger is provided.
/// </summary>
internal class NullLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
