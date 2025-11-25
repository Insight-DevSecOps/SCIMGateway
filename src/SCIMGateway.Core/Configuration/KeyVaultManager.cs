// ==========================================================================
// T012: KeyVaultManager - Azure Key Vault Integration
// ==========================================================================
// Secure credential management using Azure Key Vault with managed identity
// ==========================================================================

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SCIMGateway.Core.Configuration;

/// <summary>
/// Interface for Azure Key Vault operations.
/// </summary>
public interface IKeyVaultManager
{
    /// <summary>
    /// Retrieves a secret value from Key Vault.
    /// </summary>
    /// <param name="secretName">Name of the secret to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secret value.</returns>
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a connection string from Key Vault.
    /// </summary>
    /// <param name="connectionName">Name of the connection string secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection string value.</returns>
    Task<string> GetConnectionStringAsync(string connectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves adapter credentials from Key Vault.
    /// </summary>
    /// <param name="adapterId">Adapter identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The adapter credentials.</returns>
    Task<AdapterCredentials> GetAdapterCredentialsAsync(string adapterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health of the Key Vault connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Key Vault is accessible.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for configuring KeyVaultManager.
/// </summary>
public class KeyVaultOptions
{
    /// <summary>
    /// URI of the Azure Key Vault (e.g., https://my-vault.vault.azure.net/).
    /// </summary>
    public string VaultUri { get; set; } = string.Empty;

    /// <summary>
    /// Cache duration for secrets.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to use managed identity authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Client ID for service principal authentication (if not using managed identity).
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Tenant ID for service principal authentication.
    /// </summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// Azure Key Vault manager for secure credential retrieval.
/// Uses DefaultAzureCredential for managed identity support in Azure environments.
/// </summary>
public class KeyVaultManager : IKeyVaultManager
{
    private readonly SecretClient _secretClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KeyVaultManager> _logger;
    private readonly KeyVaultOptions _options;
    private const string CacheKeyPrefix = "kv_";

    /// <summary>
    /// Creates a new KeyVaultManager instance.
    /// </summary>
    /// <param name="options">Key Vault configuration options.</param>
    /// <param name="cache">Memory cache for secret caching.</param>
    /// <param name="logger">Logger instance.</param>
    public KeyVaultManager(
        IOptions<KeyVaultOptions> options,
        IMemoryCache cache,
        ILogger<KeyVaultManager> logger)
    {
        _options = options.Value;
        _cache = cache;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.VaultUri))
        {
            throw new ArgumentException("VaultUri is required", nameof(options));
        }

        // Use DefaultAzureCredential which supports managed identity in Azure,
        // and falls back to other auth methods in development
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = _options.ClientId,
            TenantId = _options.TenantId
        });

        _secretClient = new SecretClient(new Uri(_options.VaultUri), credential);
        _logger.LogInformation("KeyVaultManager initialized for vault: {VaultUri}", _options.VaultUri);
    }

    /// <summary>
    /// Creates a new KeyVaultManager instance with a specific SecretClient (for testing).
    /// </summary>
    /// <param name="secretClient">The secret client to use.</param>
    /// <param name="cache">Memory cache for secret caching.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Key Vault configuration options.</param>
    public KeyVaultManager(
        SecretClient secretClient,
        IMemoryCache cache,
        ILogger<KeyVaultManager> logger,
        KeyVaultOptions options)
    {
        _secretClient = secretClient;
        _cache = cache;
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(secretName);

        var cacheKey = $"{CacheKeyPrefix}{secretName}";

        if (_cache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("Retrieved secret {SecretName} from cache", secretName);
            return cachedValue;
        }

        try
        {
            _logger.LogDebug("Fetching secret {SecretName} from Key Vault", secretName);
            var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
            var secretValue = response.Value.Value;

            // Cache the secret
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_options.CacheDuration);
            _cache.Set(cacheKey, secretValue, cacheOptions);

            _logger.LogInformation("Successfully retrieved secret {SecretName} from Key Vault", secretName);
            return secretValue;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault", secretName);
            throw new KeyVaultException($"Failed to retrieve secret '{secretName}'", secretName, ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> GetConnectionStringAsync(string connectionName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        // Connection strings are stored with a naming convention
        var secretName = $"ConnectionString-{connectionName}";
        return await GetSecretAsync(secretName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdapterCredentials> GetAdapterCredentialsAsync(string adapterId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(adapterId);

        var credentials = new AdapterCredentials();

        // Try to get API key
        try
        {
            var apiKeySecretName = $"Adapter-{adapterId}-ApiKey";
            credentials.ApiKey = await GetSecretAsync(apiKeySecretName, cancellationToken);
            credentials.AuthenticationType = AuthenticationType.ApiKey;
        }
        catch (KeyVaultException)
        {
            // API key not found, try OAuth credentials
            try
            {
                var clientIdSecretName = $"Adapter-{adapterId}-ClientId";
                var clientSecretSecretName = $"Adapter-{adapterId}-ClientSecret";

                credentials.ClientId = await GetSecretAsync(clientIdSecretName, cancellationToken);
                credentials.ClientSecret = await GetSecretAsync(clientSecretSecretName, cancellationToken);
                credentials.AuthenticationType = AuthenticationType.OAuth2;
            }
            catch (KeyVaultException ex)
            {
                _logger.LogError(ex, "No credentials found for adapter {AdapterId}", adapterId);
                throw new KeyVaultException($"No credentials found for adapter '{adapterId}'", adapterId, ex);
            }
        }

        return credentials;
    }

    /// <inheritdoc />
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to list secrets (will fail quickly if Key Vault is inaccessible)
            await foreach (var _ in _secretClient.GetPropertiesOfSecretsAsync(cancellationToken).ConfigureAwait(false))
            {
                // Just checking if we can access Key Vault
                return true;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key Vault health check failed");
            return false;
        }
    }
}

/// <summary>
/// Exception thrown when Key Vault operations fail.
/// </summary>
public class KeyVaultException : Exception
{
    /// <summary>
    /// Name of the secret that caused the exception.
    /// </summary>
    public string SecretName { get; }

    /// <summary>
    /// Creates a new KeyVaultException.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="secretName">Name of the secret.</param>
    /// <param name="innerException">Inner exception.</param>
    public KeyVaultException(string message, string secretName, Exception? innerException = null)
        : base(message, innerException)
    {
        SecretName = secretName;
    }
}
