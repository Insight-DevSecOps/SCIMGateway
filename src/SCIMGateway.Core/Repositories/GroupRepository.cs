// ==========================================================================
// T040-T045: GroupRepository - Cosmos DB Group Repository Implementation
// ==========================================================================
// Implements group CRUD operations using Azure Cosmos DB
// ==========================================================================

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Data;
using SCIMGateway.Core.Filtering;
using SCIMGateway.Core.Models;
using System.Net;

namespace SCIMGateway.Core.Repositories;

/// <summary>
/// Cosmos DB implementation of group repository.
/// </summary>
public class GroupRepository : IGroupRepository
{
    private readonly ICosmosDbClient _cosmosClient;
    private readonly IFilterParser _filterParser;
    private readonly ILogger<GroupRepository> _logger;

    public GroupRepository(
        ICosmosDbClient cosmosClient,
        IFilterParser filterParser,
        ILogger<GroupRepository> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _filterParser = filterParser ?? throw new ArgumentNullException(nameof(filterParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ScimGroup> CreateAsync(ScimGroup group, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        // Assign ID if not set
        if (string.IsNullOrEmpty(group.Id))
        {
            group.Id = Guid.NewGuid().ToString();
        }

        // Set tenant ID
        group.TenantId = tenantId;

        // Set metadata
        group.Meta = new ScimMeta
        {
            ResourceType = ScimConstants.ResourceTypes.Group,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            Version = $"W/\"{Guid.NewGuid():N}\""
        };

        // Initialize members list if null
        group.Members ??= [];

        var container = _cosmosClient.GetGroupsContainer();

        try
        {
            var created = await _cosmosClient.CreateItemAsync(container, group, tenantId);
            _logger.LogInformation("Created group {GroupId} for tenant {TenantId}", group.Id, tenantId);
            return created;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Group creation conflict for {GroupId} in tenant {TenantId}", group.Id, tenantId);
            throw new Errors.ScimConflictException($"Group with ID {group.Id} already exists");
        }
    }

    /// <inheritdoc />
    public async Task<ScimGroup?> GetByIdAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetGroupsContainer();

        try
        {
            var group = await _cosmosClient.ReadItemAsync<ScimGroup>(container, id, tenantId);
            return group;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ScimGroup?> GetByDisplayNameAsync(string displayName, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(displayName);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetGroupsContainer();
        var query = $"SELECT * FROM c WHERE c.tenantId = @tenantId AND c.displayName = @displayName";

        // Use query with parameters for security
        var queryDef = new QueryDefinition(query)
            .WithParameter("@tenantId", tenantId)
            .WithParameter("@displayName", displayName);

        var groups = await _cosmosClient.QueryItemsAsync<ScimGroup>(container, queryDef.QueryText, tenantId);
        return groups.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<ScimGroup?> GetByExternalIdAsync(string externalId, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(externalId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetGroupsContainer();
        var query = $"SELECT * FROM c WHERE c.tenantId = '{tenantId}' AND c.externalId = '{externalId}'";

        var groups = await _cosmosClient.QueryItemsAsync<ScimGroup>(container, query, tenantId);
        return groups.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ScimGroup> Groups, int TotalCount)> ListAsync(
        string tenantId,
        string? filter = null,
        int startIndex = 1,
        int count = 100,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetGroupsContainer();

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
            // Default sort by displayName
            queryBuilder.Append(" ORDER BY c.displayName ASC");
        }

        var query = queryBuilder.ToString();

        // Get all groups (we'll paginate in memory for now)
        var allGroups = await _cosmosClient.QueryItemsAsync<ScimGroup>(container, query, tenantId);
        var totalCount = allGroups.Count;

        // Apply pagination (startIndex is 1-based per SCIM spec)
        var pagedGroups = allGroups
            .Skip(startIndex - 1)
            .Take(count)
            .ToList();

        return (pagedGroups, totalCount);
    }

    /// <inheritdoc />
    public async Task<ScimGroup> UpdateAsync(ScimGroup group, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentException.ThrowIfNullOrEmpty(group.Id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        // Set tenant ID
        group.TenantId = tenantId;

        // Update metadata
        if (group.Meta == null)
        {
            group.Meta = new ScimMeta { ResourceType = ScimConstants.ResourceTypes.Group };
        }
        group.Meta.LastModified = DateTime.UtcNow;
        group.Meta.Version = $"W/\"{Guid.NewGuid():N}\"";

        var container = _cosmosClient.GetGroupsContainer();

        try
        {
            var updated = await _cosmosClient.UpsertItemAsync(container, group, tenantId);
            _logger.LogInformation("Updated group {GroupId} for tenant {TenantId}", group.Id, tenantId);
            return updated;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Errors.ScimNotFoundException($"Group with ID {group.Id} not found");
        }
    }

    /// <inheritdoc />
    public async Task<ScimGroup> PatchAsync(string id, string tenantId, IEnumerable<ScimPatchOperation> operations, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        ArgumentNullException.ThrowIfNull(operations);

        // Get existing group
        var group = await GetByIdAsync(id, tenantId, cancellationToken);
        if (group == null)
        {
            throw new Errors.ScimNotFoundException($"Group with ID {id} not found");
        }

        // Apply patch operations
        foreach (var operation in operations)
        {
            ApplyPatchOperation(group, operation);
        }

        // Update the group
        return await UpdateAsync(group, tenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetGroupsContainer();

        try
        {
            await _cosmosClient.DeleteItemAsync(container, id, tenantId);
            _logger.LogInformation("Deleted group {GroupId} from tenant {TenantId}", id, tenantId);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DisplayNameExistsAsync(string displayName, string tenantId, string? excludeGroupId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(displayName);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var container = _cosmosClient.GetGroupsContainer();
        var query = $"SELECT VALUE COUNT(1) FROM c WHERE c.tenantId = '{tenantId}' AND c.displayName = '{displayName}'";

        if (!string.IsNullOrEmpty(excludeGroupId))
        {
            query += $" AND c.id != '{excludeGroupId}'";
        }

        var results = await _cosmosClient.QueryItemsAsync<int>(container, query, tenantId);
        return results.FirstOrDefault() > 0;
    }

    /// <inheritdoc />
    public async Task<ScimGroup> AddMemberAsync(string groupId, ScimGroupMember member, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(groupId);
        ArgumentNullException.ThrowIfNull(member);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var group = await GetByIdAsync(groupId, tenantId, cancellationToken);
        if (group == null)
        {
            throw new Errors.ScimNotFoundException($"Group with ID {groupId} not found");
        }

        // Initialize members if null
        group.Members ??= [];

        // Check if member already exists
        if (group.Members.Any(m => m.Value == member.Value))
        {
            _logger.LogDebug("Member {MemberId} already exists in group {GroupId}", member.Value, groupId);
            return group;
        }

        // Add the member
        group.Members.Add(member);

        return await UpdateAsync(group, tenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ScimGroup> RemoveMemberAsync(string groupId, string memberId, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(groupId);
        ArgumentException.ThrowIfNullOrEmpty(memberId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var group = await GetByIdAsync(groupId, tenantId, cancellationToken);
        if (group == null)
        {
            throw new Errors.ScimNotFoundException($"Group with ID {groupId} not found");
        }

        if (group.Members == null || !group.Members.Any())
        {
            return group;
        }

        // Remove the member
        var memberToRemove = group.Members.FirstOrDefault(m => m.Value == memberId);
        if (memberToRemove != null)
        {
            group.Members.Remove(memberToRemove);
            return await UpdateAsync(group, tenantId, cancellationToken);
        }

        return group;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ScimGroupMember>> GetMembersAsync(string groupId, string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(groupId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        var group = await GetByIdAsync(groupId, tenantId, cancellationToken);
        if (group == null)
        {
            throw new Errors.ScimNotFoundException($"Group with ID {groupId} not found");
        }

        return group.Members ?? [];
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
            "displayname" => "displayName",
            "externalid" => "externalId",
            "members" => "members",
            "members.value" => "members[0].value",
            "members.display" => "members[0].display",
            "members.type" => "members[0].type",
            "meta.created" => "meta.created",
            "meta.lastmodified" => "meta.lastModified",
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

    private void ApplyPatchOperation(ScimGroup group, ScimPatchOperation operation)
    {
        var op = operation.Op.ToLowerInvariant();
        var path = operation.Path?.ToLowerInvariant() ?? string.Empty;

        switch (op)
        {
            case "add":
                ApplyAddOperation(group, path, operation.Value);
                break;
            case "replace":
                ApplyReplaceOperation(group, path, operation.Value);
                break;
            case "remove":
                ApplyRemoveOperation(group, path, operation.Value);
                break;
            default:
                throw new Errors.ScimInvalidSyntaxException($"Invalid patch operation: {operation.Op}");
        }
    }

    private void ApplyAddOperation(ScimGroup group, string path, object? value)
    {
        // Handle members add
        if (path == "members" || path.StartsWith("members["))
        {
            AddMembers(group, value);
            return;
        }

        // Handle other paths
        switch (path)
        {
            case "displayname":
                group.DisplayName = value?.ToString() ?? string.Empty;
                break;
            case "externalid":
                group.ExternalId = value?.ToString();
                break;
            default:
                _logger.LogWarning("Unhandled add path: {Path}", path);
                break;
        }
    }

    private void ApplyReplaceOperation(ScimGroup group, string path, object? value)
    {
        switch (path)
        {
            case "displayname":
                group.DisplayName = value?.ToString() ?? string.Empty;
                break;
            case "externalid":
                group.ExternalId = value?.ToString();
                break;
            case "members":
                // Replace all members
                group.Members = ParseMembers(value);
                break;
            default:
                _logger.LogWarning("Unhandled replace path: {Path}", path);
                break;
        }
    }

    private void ApplyRemoveOperation(ScimGroup group, string path, object? value)
    {
        // Handle members removal with filter
        if (path.StartsWith("members[value eq "))
        {
            // Parse member ID from path like: members[value eq "user-id"]
            var memberId = ExtractMemberIdFromPath(path);
            if (!string.IsNullOrEmpty(memberId) && group.Members != null)
            {
                var member = group.Members.FirstOrDefault(m => m.Value == memberId);
                if (member != null)
                {
                    group.Members.Remove(member);
                }
            }
            return;
        }

        // Handle other removal paths
        switch (path)
        {
            case "externalid":
                group.ExternalId = null;
                break;
            case "members":
                // If value contains specific members to remove
                if (value != null)
                {
                    RemoveMembers(group, value);
                }
                else
                {
                    group.Members?.Clear();
                }
                break;
            default:
                _logger.LogWarning("Unhandled remove path: {Path}", path);
                break;
        }
    }

    private void AddMembers(ScimGroup group, object? value)
    {
        group.Members ??= [];
        var newMembers = ParseMembers(value);
        
        foreach (var member in newMembers)
        {
            if (!group.Members.Any(m => m.Value == member.Value))
            {
                group.Members.Add(member);
            }
        }
    }

    private void RemoveMembers(ScimGroup group, object? value)
    {
        if (group.Members == null) return;

        var membersToRemove = ParseMembers(value);
        foreach (var member in membersToRemove)
        {
            var existing = group.Members.FirstOrDefault(m => m.Value == member.Value);
            if (existing != null)
            {
                group.Members.Remove(existing);
            }
        }
    }

    private static List<ScimGroupMember> ParseMembers(object? value)
    {
        if (value == null) return [];

        // Handle JSON array
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            var members = new List<ScimGroupMember>();
            
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in jsonElement.EnumerateArray())
                {
                    var member = ParseSingleMember(item);
                    if (member != null)
                    {
                        members.Add(member);
                    }
                }
            }
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var member = ParseSingleMember(jsonElement);
                if (member != null)
                {
                    members.Add(member);
                }
            }
            
            return members;
        }

        // Handle list of members
        if (value is IEnumerable<ScimGroupMember> memberList)
        {
            return memberList.ToList();
        }

        return [];
    }

    private static ScimGroupMember? ParseSingleMember(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
            return null;

        var member = new ScimGroupMember();

        if (element.TryGetProperty("value", out var valueElem))
        {
            member.Value = valueElem.GetString();
        }

        if (element.TryGetProperty("display", out var displayElem))
        {
            member.Display = displayElem.GetString();
        }

        if (element.TryGetProperty("type", out var typeElem))
        {
            member.Type = typeElem.GetString();
        }

        if (element.TryGetProperty("$ref", out var refElem))
        {
            member.Ref = refElem.GetString();
        }

        return member;
    }

    private static string? ExtractMemberIdFromPath(string path)
    {
        // Parse path like: members[value eq "user-id"]
        var startQuote = path.IndexOf('"');
        var endQuote = path.LastIndexOf('"');
        
        if (startQuote >= 0 && endQuote > startQuote)
        {
            return path[(startQuote + 1)..endQuote];
        }

        return null;
    }

    #endregion
}
