# Adapter Interface Contract

**Version**: 1.0.0  
**Phase**: Phase 1 Design  
**Status**: Final Design  
**Date**: 2025-11-22

---

## 1. Overview

The Adapter Interface defines a standardized contract for integrating with SaaS provider APIs. All provider-specific implementations (Salesforce, Workday, ServiceNow) must implement this interface to ensure consistency, testability, and maintainability.

### Design Principles

1. **Provider Agnostic**: Interface methods use SCIM types, not provider-specific types
2. **Async First**: All I/O operations are asynchronous
3. **Error Translation**: Adapters translate provider errors to SCIM error responses
4. **Testable**: Interface designed for unit testing and mocking
5. **Observable**: Built-in hooks for logging, metrics, and tracing

---

## 2. Core Interface Definition

```csharp
namespace SCIMGateway.Adapters
{
    /// <summary>
    /// Adapter interface for SaaS provider integration.
    /// All provider-specific adapters must implement this interface.
    /// </summary>
    public interface IAdapter
    {
        /// <summary>
        /// Unique identifier for this adapter instance.
        /// Format: "{providerName}-{environment}" (e.g., "salesforce-prod")
        /// </summary>
        string AdapterId { get; }
        
        /// <summary>
        /// Provider name (Salesforce, Workday, ServiceNow, etc.)
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Adapter configuration (credentials, endpoints, settings)
        /// </summary>
        AdapterConfiguration Configuration { get; }
        
        /// <summary>
        /// Adapter health status
        /// </summary>
        AdapterHealthStatus HealthStatus { get; }
        
        // ==================== User Operations ====================
        
        /// <summary>
        /// Create a new user in the provider system.
        /// </summary>
        /// <param name="user">SCIM User object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created user with provider-assigned ID</returns>
        /// <exception cref="AdapterException">Provider-specific error</exception>
        Task<ScimUser> CreateUserAsync(
            ScimUser user, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieve a user by ID.
        /// </summary>
        /// <param name="userId">Provider user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>SCIM User object or null if not found</returns>
        Task<ScimUser?> GetUserAsync(
            string userId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <param name="userId">Provider user ID</param>
        /// <param name="user">Updated SCIM User object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated user with new version</returns>
        Task<ScimUser> UpdateUserAsync(
            string userId, 
            ScimUser user, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="userId">Provider user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task DeleteUserAsync(
            string userId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// List users with filtering and pagination.
        /// </summary>
        /// <param name="filter">SCIM filter query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paged result of users</returns>
        Task<PagedResult<ScimUser>> ListUsersAsync(
            QueryFilter filter, 
            CancellationToken cancellationToken = default);
        
        // ==================== Group Operations ====================
        
        /// <summary>
        /// Create a new group in the provider system.
        /// </summary>
        Task<ScimGroup> CreateGroupAsync(
            ScimGroup group, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Retrieve a group by ID.
        /// </summary>
        Task<ScimGroup?> GetGroupAsync(
            string groupId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Update an existing group.
        /// </summary>
        Task<ScimGroup> UpdateGroupAsync(
            string groupId, 
            ScimGroup group, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Delete a group.
        /// </summary>
        Task DeleteGroupAsync(
            string groupId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// List groups with filtering and pagination.
        /// </summary>
        Task<PagedResult<ScimGroup>> ListGroupsAsync(
            QueryFilter filter, 
            CancellationToken cancellationToken = default);
        
        // ==================== Membership Operations ====================
        
        /// <summary>
        /// Add a user to a group/role/entitlement.
        /// </summary>
        /// <param name="groupId">Provider group/role ID</param>
        /// <param name="userId">Provider user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task AddUserToGroupAsync(
            string groupId, 
            string userId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Remove a user from a group/role/entitlement.
        /// </summary>
        Task RemoveUserFromGroupAsync(
            string groupId, 
            string userId, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get all members of a group/role.
        /// </summary>
        /// <returns>List of user IDs</returns>
        Task<IEnumerable<string>> GetGroupMembersAsync(
            string groupId, 
            CancellationToken cancellationToken = default);
        
        // ==================== Transformation Operations ====================
        
        /// <summary>
        /// Map a SCIM group to provider-specific entitlement(s).
        /// </summary>
        /// <param name="group">SCIM Group</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Provider entitlement mapping</returns>
        Task<EntitlementMapping> MapGroupToEntitlementAsync(
            ScimGroup group, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Reverse map: provider entitlement to SCIM group.
        /// </summary>
        Task<ScimGroup> MapEntitlementToGroupAsync(
            EntitlementMapping entitlement, 
            CancellationToken cancellationToken = default);
        
        // ==================== Health & Diagnostics ====================
        
        /// <summary>
        /// Check adapter health status (connectivity, auth, rate limits).
        /// </summary>
        Task<AdapterHealthStatus> CheckHealthAsync(
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get adapter capabilities and supported features.
        /// </summary>
        AdapterCapabilities GetCapabilities();
    }
}
```

---

## 3. Supporting Types

### 3.1 AdapterConfiguration

```csharp
public class AdapterConfiguration
{
    /// <summary>
    /// Adapter ID (unique instance identifier)
    /// </summary>
    public string AdapterId { get; set; }
    
    /// <summary>
    /// Provider name (Salesforce, Workday, ServiceNow)
    /// </summary>
    public string ProviderName { get; set; }
    
    /// <summary>
    /// Provider API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; }
    
    /// <summary>
    /// Azure Key Vault path for credentials
    /// </summary>
    public string CredentialKeyVaultPath { get; set; }
    
    /// <summary>
    /// Group mapping strategy (ROLE_ASSIGNMENT, ORG_HIERARCHY, DIRECT_GROUP, etc.)
    /// </summary>
    public string GroupMappingStrategy { get; set; }
    
    /// <summary>
    /// Transformation rules for group → entitlement mapping
    /// </summary>
    public List<TransformationRule> TransformationRules { get; set; }
    
    /// <summary>
    /// Rate limiting settings
    /// </summary>
    public RateLimitConfiguration RateLimiting { get; set; }
    
    /// <summary>
    /// Timeout settings
    /// </summary>
    public TimeoutConfiguration Timeouts { get; set; }
    
    /// <summary>
    /// Retry policy settings
    /// </summary>
    public RetryPolicyConfiguration RetryPolicy { get; set; }
    
    /// <summary>
    /// Provider-specific custom settings (JSON object)
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; }
}
```

### 3.2 QueryFilter

```csharp
public class QueryFilter
{
    /// <summary>
    /// SCIM filter expression (e.g., "userName eq 'john@example.com'")
    /// </summary>
    public string Filter { get; set; }
    
    /// <summary>
    /// Attributes to return (comma-separated)
    /// </summary>
    public string Attributes { get; set; }
    
    /// <summary>
    /// Attributes to exclude (comma-separated)
    /// </summary>
    public string ExcludedAttributes { get; set; }
    
    /// <summary>
    /// Sort by attribute name
    /// </summary>
    public string SortBy { get; set; }
    
    /// <summary>
    /// Sort order (ascending, descending)
    /// </summary>
    public SortOrder SortOrder { get; set; }
    
    /// <summary>
    /// Page start index (1-based)
    /// </summary>
    public int StartIndex { get; set; } = 1;
    
    /// <summary>
    /// Page size (max results per page)
    /// </summary>
    public int Count { get; set; } = 100;
}

public enum SortOrder
{
    Ascending,
    Descending
}
```

### 3.3 PagedResult

```csharp
public class PagedResult<T>
{
    /// <summary>
    /// List of resources
    /// </summary>
    public List<T> Resources { get; set; }
    
    /// <summary>
    /// Total results available (across all pages)
    /// </summary>
    public int TotalResults { get; set; }
    
    /// <summary>
    /// Start index of this page
    /// </summary>
    public int StartIndex { get; set; }
    
    /// <summary>
    /// Number of results in this page
    /// </summary>
    public int ItemsPerPage { get; set; }
}
```

### 3.4 EntitlementMapping

```csharp
public class EntitlementMapping
{
    /// <summary>
    /// Provider ID (Salesforce, Workday, ServiceNow)
    /// </summary>
    public string ProviderId { get; set; }
    
    /// <summary>
    /// Provider-specific entitlement ID (role ID, org ID, group ID)
    /// </summary>
    public string ProviderEntitlementId { get; set; }
    
    /// <summary>
    /// Entitlement name
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Entitlement type (ROLE, ORG_HIERARCHY, GROUP, PERMISSION_SET, etc.)
    /// </summary>
    public EntitlementType Type { get; set; }
    
    /// <summary>
    /// List of SCIM groups mapped to this entitlement
    /// </summary>
    public List<string> MappedGroups { get; set; }
    
    /// <summary>
    /// Priority (for conflict resolution)
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// Enabled status
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// Additional metadata (provider-specific)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; }
}

public enum EntitlementType
{
    ROLE,
    PERMISSION_SET,
    ORG_HIERARCHY_LEVEL,
    GROUP,
    DEPARTMENT,
    CUSTOM
}
```

### 3.5 AdapterHealthStatus

```csharp
public class AdapterHealthStatus
{
    /// <summary>
    /// Overall health status
    /// </summary>
    public HealthStatusLevel Status { get; set; }
    
    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime LastChecked { get; set; }
    
    /// <summary>
    /// Connectivity status (can reach provider API)
    /// </summary>
    public bool IsConnected { get; set; }
    
    /// <summary>
    /// Authentication status (credentials valid)
    /// </summary>
    public bool IsAuthenticated { get; set; }
    
    /// <summary>
    /// Rate limit status
    /// </summary>
    public RateLimitStatus RateLimitStatus { get; set; }
    
    /// <summary>
    /// API response time (p95 latency in ms)
    /// </summary>
    public double ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Error rate (last 5 minutes)
    /// </summary>
    public double ErrorRate { get; set; }
    
    /// <summary>
    /// Health check details
    /// </summary>
    public List<string> Details { get; set; }
}

public enum HealthStatusLevel
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

public class RateLimitStatus
{
    public int Remaining { get; set; }
    public int Limit { get; set; }
    public DateTime ResetsAt { get; set; }
}
```

### 3.6 AdapterCapabilities

```csharp
public class AdapterCapabilities
{
    /// <summary>
    /// Supports user CRUD operations
    /// </summary>
    public bool SupportsUsers { get; set; } = true;
    
    /// <summary>
    /// Supports group CRUD operations
    /// </summary>
    public bool SupportsGroups { get; set; } = true;
    
    /// <summary>
    /// Supports filtering (SCIM filter expressions)
    /// </summary>
    public bool SupportsFiltering { get; set; } = true;
    
    /// <summary>
    /// Supports sorting
    /// </summary>
    public bool SupportsSorting { get; set; } = true;
    
    /// <summary>
    /// Supports pagination
    /// </summary>
    public bool SupportsPagination { get; set; } = true;
    
    /// <summary>
    /// Supports PATCH operations
    /// </summary>
    public bool SupportsPatch { get; set; } = true;
    
    /// <summary>
    /// Supports bulk operations
    /// </summary>
    public bool SupportsBulk { get; set; } = false;
    
    /// <summary>
    /// Supports enterprise user extension
    /// </summary>
    public bool SupportsEnterpriseExtension { get; set; } = true;
    
    /// <summary>
    /// Maximum page size
    /// </summary>
    public int MaxPageSize { get; set; } = 100;
    
    /// <summary>
    /// Supported filter operators
    /// </summary>
    public List<string> SupportedFilterOperators { get; set; }
    
    /// <summary>
    /// Supported authentication methods
    /// </summary>
    public List<string> SupportedAuthMethods { get; set; }
}
```

---

## 4. Error Handling

### 4.1 AdapterException

```csharp
public class AdapterException : Exception
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; set; }
    
    /// <summary>
    /// HTTP status code (if applicable)
    /// </summary>
    public int? HttpStatusCode { get; set; }
    
    /// <summary>
    /// Provider error code
    /// </summary>
    public string ProviderErrorCode { get; set; }
    
    /// <summary>
    /// SCIM error type (for translation)
    /// </summary>
    public ScimErrorType ScimErrorType { get; set; }
    
    /// <summary>
    /// Is this error retryable?
    /// </summary>
    public bool IsRetryable { get; set; }
    
    /// <summary>
    /// Retry after (seconds)
    /// </summary>
    public int? RetryAfterSeconds { get; set; }
    
    public AdapterException(
        string message, 
        string providerName, 
        Exception innerException = null) 
        : base(message, innerException)
    {
        ProviderName = providerName;
    }
}

public enum ScimErrorType
{
    InvalidSyntax,
    Uniqueness,
    Mutability,
    InvalidFilter,
    NoTarget,
    TooMany,
    ServerUnavailable,
    ResourceNotFound,
    Unauthorized,
    Forbidden
}
```

---

## 5. Adapter Base Class (Optional)

```csharp
public abstract class AdapterBase : IAdapter
{
    protected readonly ILogger _logger;
    protected readonly IHttpClientFactory _httpClientFactory;
    protected readonly ISecretStore _secretStore;
    protected readonly AdapterConfiguration _configuration;
    
    public string AdapterId => _configuration.AdapterId;
    public string ProviderName => _configuration.ProviderName;
    public AdapterConfiguration Configuration => _configuration;
    public AdapterHealthStatus HealthStatus { get; protected set; }
    
    protected AdapterBase(
        AdapterConfiguration configuration,
        ILogger logger,
        IHttpClientFactory httpClientFactory,
        ISecretStore secretStore)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    }
    
    // Abstract methods to implement per provider
    public abstract Task<ScimUser> CreateUserAsync(ScimUser user, CancellationToken cancellationToken = default);
    public abstract Task<ScimUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
    // ... other abstract methods
    
    // Common helper methods
    protected async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Retrieve OAuth token from Key Vault
        var credentials = await _secretStore.GetSecretAsync(_configuration.CredentialKeyVaultPath, cancellationToken);
        // Implement token acquisition logic
        return credentials.AccessToken;
    }
    
    protected ScimErrorResponse TranslateError(Exception ex)
    {
        // Translate provider error to SCIM error
        if (ex is AdapterException adapterEx)
        {
            return new ScimErrorResponse
            {
                Status = adapterEx.HttpStatusCode ?? 500,
                ScimType = adapterEx.ScimErrorType.ToString(),
                Detail = adapterEx.Message
            };
        }
        
        return new ScimErrorResponse
        {
            Status = 500,
            ScimType = "serverUnavailable",
            Detail = "An unexpected error occurred"
        };
    }
}
```

---

## 6. Usage Examples

### 6.1 Salesforce Adapter Implementation

```csharp
public class SalesforceAdapter : AdapterBase
{
    public SalesforceAdapter(
        AdapterConfiguration configuration,
        ILogger<SalesforceAdapter> logger,
        IHttpClientFactory httpClientFactory,
        ISecretStore secretStore)
        : base(configuration, logger, httpClientFactory, secretStore)
    {
    }
    
    public override async Task<ScimUser> CreateUserAsync(
        ScimUser user, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating user {UserName} in Salesforce", user.UserName);
            
            // Get access token
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            
            // Map SCIM user to Salesforce user
            var salesforceUser = MapScimToSalesforceUser(user);
            
            // Call Salesforce API
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await httpClient.PostAsJsonAsync(
                $"{_configuration.ApiBaseUrl}/services/data/v60.0/sobjects/User",
                salesforceUser,
                cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<SalesforceUserResponse>(cancellationToken);
            
            // Map back to SCIM
            user.Id = result.Id;
            user.Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Location = $"{_configuration.ApiBaseUrl}/Users/{result.Id}",
                Version = "W/\"1\""
            };
            
            _logger.LogInformation("User {UserName} created with ID {UserId}", user.UserName, user.Id);
            
            return user;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to create user {UserName} in Salesforce", user.UserName);
            throw new AdapterException(
                $"Salesforce API error: {ex.Message}",
                ProviderName,
                ex)
            {
                HttpStatusCode = (int)(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError),
                IsRetryable = ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
            };
        }
    }
    
    // ... other methods
}
```

---

## 7. Testing Strategy

### 7.1 Unit Tests

```csharp
[Fact]
public async Task CreateUserAsync_ValidUser_ReturnsCreatedUser()
{
    // Arrange
    var mockHttpFactory = new Mock<IHttpClientFactory>();
    var mockSecretStore = new Mock<ISecretStore>();
    var mockLogger = new Mock<ILogger<SalesforceAdapter>>();
    
    var adapter = new SalesforceAdapter(
        TestConfiguration.Salesforce,
        mockLogger.Object,
        mockHttpFactory.Object,
        mockSecretStore.Object);
    
    var user = new ScimUser
    {
        UserName = "test@example.com",
        Name = new ScimName { FamilyName = "Test", GivenName = "User" }
    };
    
    // Act
    var result = await adapter.CreateUserAsync(user);
    
    // Assert
    Assert.NotNull(result.Id);
    Assert.Equal("test@example.com", result.UserName);
}
```

### 7.2 Integration Tests

```csharp
[Fact]
public async Task CreateUserAsync_RealSalesforceAPI_Success()
{
    // Requires Salesforce sandbox environment
    var adapter = IntegrationTestFactory.CreateSalesforceAdapter();
    
    var user = new ScimUser
    {
        UserName = $"test-{Guid.NewGuid()}@example.com",
        Name = new ScimName { FamilyName = "Integration", GivenName = "Test" }
    };
    
    var result = await adapter.CreateUserAsync(user);
    
    Assert.NotNull(result.Id);
    
    // Cleanup
    await adapter.DeleteUserAsync(result.Id);
}
```

---

## 8. Configuration Examples

### 8.1 Salesforce Adapter Configuration

```json
{
  "adapterId": "salesforce-prod",
  "providerName": "Salesforce",
  "apiBaseUrl": "https://company.salesforce.com",
  "credentialKeyVaultPath": "scim-adapters-salesforce-credentials",
  "groupMappingStrategy": "ROLE_ASSIGNMENT",
  "transformationRules": [
    {
      "sourcePattern": "^Sales-(.*)$",
      "targetType": "SALESFORCE_ROLE",
      "targetMapping": "Sales_${1}_Rep"
    }
  ],
  "rateLimiting": {
    "maxRequestsPerSecond": 100,
    "bucketCapacity": 200
  },
  "timeouts": {
    "connectionTimeoutMs": 5000,
    "requestTimeoutMs": 30000
  },
  "retryPolicy": {
    "maxRetries": 3,
    "backoffStrategy": "EXPONENTIAL_WITH_JITTER",
    "retryableStatusCodes": [429, 503]
  }
}
```

---

**Next**: See `scim-user-endpoints.md` for SCIM endpoint specifications.

**Version**: 1.0.0 | **Status**: Final Design | **Date**: 2025-11-22
