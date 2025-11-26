// ==========================================================================
// T034-T038: UserRepository - Cosmos DB User Repository Implementation
// ==========================================================================
// Implements user CRUD operations using Azure Cosmos DB
// ==========================================================================

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Data;
using SCIMGateway.Core.Filtering;
using SCIMGateway.Core.Models;
using System.Net;

namespace SCIMGateway.Core.Repositories;

/// <summary>
/// Cosmos DB implementation of user repository.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ICosmosDbClient _cosmosClient;
    private readonly IFilterParser _filterParser;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        ICosmosDbClient cosmosClient,
        IFilterParser filterParser,
        ILogger<UserRepository> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _filterParser = filterParser ?? throw new ArgumentNullException(nameof(filterParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ScimUser> CreateAsync(ScimUser user, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        // Assign ID if not set
        if (string.IsNullOrEmpty(user.Id))
        {
            user.Id = Guid.NewGuid().ToString();
        }

        // Set tenant ID
        user.TenantId = tenantId;

        // Set metadata
        user.Meta = new ScimMeta
        {
            ResourceType = ScimConstants.ResourceTypes.User,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Version = $"W/\"{Guid.NewGuid():N}\""
        };

        var container = _cosmosClient.GetUsersContainer();
        
        try
        {
            var created = await _cosmosClient.CreateItemAsync(container, user, tenantId);
            _logger.LogInformation("Created user {UserId} for tenant {TenantId}", user.Id, tenantId);
            return created;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning("User creation conflict for {UserId} in tenant {TenantId}", user.Id, tenantId);
            throw new Errors.ScimConflictException($"User with ID {user.Id} already exists");
        }
    }

    /// <inheritdoc />
    public async Task<ScimUser?> GetByIdAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetUsersContainer();
        
        try
        {
            var user = await _cosmosClient.ReadItemAsync<ScimUser>(container, id, tenantId);
            
            // Clear internal fields before returning
            if (user != null)
            {
                user.Password = null; // Never return password
            }
            
            return user;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ScimUser?> GetByUserNameAsync(string userName, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userName);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetUsersContainer();
        var query = $"SELECT * FROM c WHERE c.tenantId = @tenantId AND c.userName = @userName";
        
        // Use query with parameters for security
        var queryDef = new QueryDefinition(query)
            .WithParameter("@tenantId", tenantId)
            .WithParameter("@userName", userName);

        var users = await _cosmosClient.QueryItemsAsync<ScimUser>(container, queryDef.QueryText, tenantId);
        var user = users.FirstOrDefault();
        
        if (user != null)
        {
            user.Password = null;
        }
        
        return user;
    }

    /// <inheritdoc />
    public async Task<ScimUser?> GetByExternalIdAsync(string externalId, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(externalId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetUsersContainer();
        var query = $"SELECT * FROM c WHERE c.tenantId = '{tenantId}' AND c.externalId = '{externalId}'";
        
        var users = await _cosmosClient.QueryItemsAsync<ScimUser>(container, query, tenantId);
        var user = users.FirstOrDefault();
        
        if (user != null)
        {
            user.Password = null;
        }
        
        return user;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ScimUser> Users, int TotalCount)> ListAsync(
        string tenantId,
        string? filter = null,
        int startIndex = 1,
        int count = 100,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetUsersContainer();
        
        // Build query
        var queryBuilder = new System.Text.StringBuilder("SELECT * FROM c WHERE c.tenantId = @tenantId");
        
        // Apply SCIM filter if provided
        if (!string.IsNullOrEmpty(filter))
        {
            try
            {
                var filterExpression = _filterParser.Parse(filter);
                var cosmosFilter = ConvertFilterToCosmosQuery(filterExpression);
                if (!string.IsNullOrEmpty(cosmosFilter))
                {
                    queryBuilder.Append(" AND ").Append(cosmosFilter);
                }
            }
            catch (FilterParseException ex)
            {
                _logger.LogWarning("Invalid filter expression: {Filter}. Error: {Error}", filter, ex.Message);
                throw new Errors.ScimInvalidFilterException(ex.Message);
            }
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            var sortAttribute = MapScimAttributeToCosmosPath(sortBy);
            var order = string.Equals(sortOrder, "descending", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            queryBuilder.Append($" ORDER BY c.{sortAttribute} {order}");
        }
        else
        {
            // Default sort by userName
            queryBuilder.Append(" ORDER BY c.userName ASC");
        }

        var query = queryBuilder.ToString();
        
        // Get total count first
        var countQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.tenantId = '{tenantId}'";
        if (!string.IsNullOrEmpty(filter))
        {
            // Reuse filter for count
            countQuery = query.Replace("SELECT *", "SELECT VALUE COUNT(1)");
            if (countQuery.Contains("ORDER BY"))
            {
                countQuery = countQuery[..countQuery.IndexOf("ORDER BY")].Trim();
            }
        }

        var allUsers = await _cosmosClient.QueryItemsAsync<ScimUser>(container, query, tenantId);
        var totalCount = allUsers.Count;

        // Apply pagination (startIndex is 1-based per SCIM spec)
        var pagedUsers = allUsers
            .Skip(startIndex - 1)
            .Take(count)
            .ToList();

        // Clear passwords
        foreach (var user in pagedUsers)
        {
            user.Password = null;
        }

        return (pagedUsers, totalCount);
    }

    /// <inheritdoc />
    public async Task<ScimUser> UpdateAsync(ScimUser user, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrEmpty(user.Id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        // Set tenant ID
        user.TenantId = tenantId;

        // Update metadata
        if (user.Meta == null)
        {
            user.Meta = new ScimMeta { ResourceType = ScimConstants.ResourceTypes.User };
        }
        user.Meta.LastModified = DateTime.UtcNow;
        user.Meta.Version = $"W/\"{Guid.NewGuid():N}\"";

        var container = _cosmosClient.GetUsersContainer();
        
        try
        {
            var updated = await _cosmosClient.UpsertItemAsync(container, user, tenantId);
            _logger.LogInformation("Updated user {UserId} for tenant {TenantId}", user.Id, tenantId);
            
            updated.Password = null;
            return updated;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Errors.ScimNotFoundException($"User with ID {user.Id} not found");
        }
    }

    /// <inheritdoc />
    public async Task<ScimUser> PatchAsync(string id, string tenantId, IEnumerable<ScimPatchOperation> operations, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentNullException.ThrowIfNull(operations);

        // Get existing user
        var user = await GetByIdAsync(id, tenantId, cancellationToken);
        if (user == null)
        {
            throw new Errors.ScimNotFoundException($"User with ID {id} not found");
        }

        // Apply patch operations
        foreach (var operation in operations)
        {
            ApplyPatchOperation(user, operation);
        }

        // Update the user
        return await UpdateAsync(user, tenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetUsersContainer();
        
        try
        {
            await _cosmosClient.DeleteItemAsync(container, id, tenantId);
            _logger.LogInformation("Deleted user {UserId} from tenant {TenantId}", id, tenantId);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UserNameExistsAsync(string userName, string tenantId, string? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userName);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetUsersContainer();
        var query = $"SELECT VALUE COUNT(1) FROM c WHERE c.tenantId = '{tenantId}' AND c.userName = '{userName}'";
        
        if (!string.IsNullOrEmpty(excludeUserId))
        {
            query += $" AND c.id != '{excludeUserId}'";
        }

        var results = await _cosmosClient.QueryItemsAsync<int>(container, query, tenantId);
        return results.FirstOrDefault() > 0;
    }

    #region Private Methods

    private static string ConvertFilterToCosmosQuery(FilterExpression expression)
    {
        return expression switch
        {
            ComparisonExpression comp => ConvertComparisonToCosmosQuery(comp),
            LogicalExpression logical => ConvertLogicalToCosmosQuery(logical),
            NotExpression not => $"NOT ({ConvertFilterToCosmosQuery(not.Expression)})",
            PresenceExpression presence => $"IS_DEFINED(c.{MapScimAttributeToCosmosPath(presence.AttributePath)})",
            _ => string.Empty
        };
    }

    private static string ConvertComparisonToCosmosQuery(ComparisonExpression comp)
    {
        var path = $"c.{MapScimAttributeToCosmosPath(comp.AttributePath)}";
        var value = FormatValueForCosmos(comp.Value);

        return comp.Operator switch
        {
            FilterOperator.Equal => $"{path} = {value}",
            FilterOperator.NotEqual => $"{path} != {value}",
            FilterOperator.Contains => $"CONTAINS({path}, {value})",
            FilterOperator.StartsWith => $"STARTSWITH({path}, {value})",
            FilterOperator.EndsWith => $"ENDSWITH({path}, {value})",
            FilterOperator.GreaterThan => $"{path} > {value}",
            FilterOperator.GreaterThanOrEqual => $"{path} >= {value}",
            FilterOperator.LessThan => $"{path} < {value}",
            FilterOperator.LessThanOrEqual => $"{path} <= {value}",
            _ => string.Empty
        };
    }

    private static string ConvertLogicalToCosmosQuery(LogicalExpression logical)
    {
        var left = ConvertFilterToCosmosQuery(logical.Left);
        var right = ConvertFilterToCosmosQuery(logical.Right);
        var op = logical.Operator == LogicalOperator.And ? "AND" : "OR";
        return $"({left}) {op} ({right})";
    }

    private static string MapScimAttributeToCosmosPath(string scimAttribute)
    {
        // Map SCIM attribute paths to Cosmos DB paths
        return scimAttribute.ToLowerInvariant() switch
        {
            "username" => "userName",
            "displayname" => "displayName",
            "name.familyname" => "name.familyName",
            "name.givenname" => "name.givenName",
            "name.formatted" => "name.formatted",
            "emails.value" => "emails[0].value",
            "emails.primary" => "emails[0].primary",
            "active" => "active",
            "externalid" => "externalId",
            "meta.created" => "meta.created",
            "meta.lastmodified" => "meta.lastModified",
            "title" => "title",
            "usertype" => "userType",
            _ => scimAttribute
        };
    }

    private static string FormatValueForCosmos(object? value)
    {
        return value switch
        {
            null => "null",
            bool b => b.ToString().ToLowerInvariant(),
            string s => $"'{s.Replace("'", "\\'")}'",
            _ => value.ToString() ?? "null"
        };
    }

    private void ApplyPatchOperation(ScimUser user, ScimPatchOperation operation)
    {
        var op = operation.Op.ToLowerInvariant();
        var path = operation.Path?.ToLowerInvariant() ?? string.Empty;

        switch (op)
        {
            case "add":
            case "replace":
                ApplyAddOrReplace(user, path, operation.Value);
                break;
            case "remove":
                ApplyRemove(user, path);
                break;
            default:
                throw new Errors.ScimInvalidSyntaxException($"Invalid patch operation: {operation.Op}");
        }
    }

    private void ApplyAddOrReplace(ScimUser user, string path, object? value)
    {
        // Handle common paths
        switch (path)
        {
            case "active":
                user.Active = Convert.ToBoolean(value);
                break;
            case "username":
                user.UserName = value?.ToString() ?? string.Empty;
                break;
            case "displayname":
                user.DisplayName = value?.ToString();
                break;
            case "title":
                user.Title = value?.ToString();
                break;
            case "name.givenname":
                user.Name ??= new ScimName();
                user.Name.GivenName = value?.ToString();
                break;
            case "name.familyname":
                user.Name ??= new ScimName();
                user.Name.FamilyName = value?.ToString();
                break;
            case "externalid":
                user.ExternalId = value?.ToString();
                break;
            // Add more paths as needed
            default:
                _logger.LogWarning("Unhandled patch path: {Path}", path);
                break;
        }
    }

    private void ApplyRemove(ScimUser user, string path)
    {
        // Handle remove operations
        switch (path)
        {
            case "displayname":
                user.DisplayName = null;
                break;
            case "title":
                user.Title = null;
                break;
            case "externalid":
                user.ExternalId = null;
                break;
            case "name.givenname":
                if (user.Name != null) user.Name.GivenName = null;
                break;
            case "name.familyname":
                if (user.Name != null) user.Name.FamilyName = null;
                break;
            // Add more paths as needed
            default:
                _logger.LogWarning("Unhandled remove path: {Path}", path);
                break;
        }
    }

    #endregion
}
