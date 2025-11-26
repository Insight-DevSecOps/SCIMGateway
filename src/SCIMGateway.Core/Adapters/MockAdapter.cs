// ==========================================================================
// T076: MockAdapter - In-Memory Test Adapter
// ==========================================================================
// Implements IAdapter with in-memory storage for testing purposes
// ==========================================================================

using System.Collections.Concurrent;
using SCIMGateway.Core.Configuration;
using SCIMGateway.Core.Errors;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Mock adapter implementation for testing.
/// Uses in-memory storage with no external dependencies.
/// </summary>
public class MockAdapter : IAdapter
{
    private readonly ConcurrentDictionary<string, ScimUser> _users = new();
    private readonly ConcurrentDictionary<string, ScimGroup> _groups = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _groupMembers = new();
    private int _userIdCounter;
    private int _groupIdCounter;

    /// <inheritdoc />
    public string AdapterId { get; }

    /// <inheritdoc />
    public string ProviderName => "Mock";

    /// <inheritdoc />
    public AdapterConfiguration Configuration { get; }

    /// <inheritdoc />
    public AdapterHealthStatus HealthStatus { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockAdapter"/> class.
    /// </summary>
    public MockAdapter()
    {
        AdapterId = "mock-adapter";
        Configuration = new AdapterConfiguration
        {
            ProviderId = AdapterId,
            Enabled = true
        };
        HealthStatus = AdapterHealthStatus.Healthy();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockAdapter"/> class.
    /// </summary>
    /// <param name="adapterId">Custom adapter ID.</param>
    public MockAdapter(string adapterId)
    {
        AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
        Configuration = new AdapterConfiguration
        {
            ProviderId = AdapterId,
            Enabled = true
        };
        HealthStatus = AdapterHealthStatus.Healthy();
    }

    // ==================== User Operations ====================

    /// <inheritdoc />
    public Task<ScimUser> CreateUserAsync(ScimUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Check for duplicate userName
        if (_users.Values.Any(u => string.Equals(u.UserName, user.UserName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new AdapterException(
                $"User with userName '{user.UserName}' already exists",
                ProviderName,
                ScimErrorType.Uniqueness,
                409);
        }

        var id = $"mock-user-{Interlocked.Increment(ref _userIdCounter)}";
        var now = DateTime.UtcNow;

        var createdUser = new ScimUser
        {
            Id = id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Name = user.Name,
            Emails = user.Emails,
            PhoneNumbers = user.PhoneNumbers,
            Active = user.Active,
            ExternalId = user.ExternalId,
            Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = now,
                LastModified = now,
                Version = "W/\"1\""
            }
        };

        _users[id] = createdUser;
        return Task.FromResult(createdUser);
    }

    /// <inheritdoc />
    public Task<ScimUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    /// <inheritdoc />
    public Task<ScimUser> UpdateUserAsync(string userId, ScimUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_users.TryGetValue(userId, out var existingUser))
        {
            throw new AdapterException(
                $"User with ID '{userId}' not found",
                ProviderName,
                ScimErrorType.ResourceNotFound,
                404);
        }

        // Check for userName conflict with other users
        if (!string.IsNullOrEmpty(user.UserName) &&
            _users.Values.Any(u => u.Id != userId && 
                string.Equals(u.UserName, user.UserName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new AdapterException(
                $"User with userName '{user.UserName}' already exists",
                ProviderName,
                ScimErrorType.Uniqueness,
                409);
        }

        var updatedUser = new ScimUser
        {
            Id = userId,
            UserName = user.UserName ?? existingUser.UserName,
            DisplayName = user.DisplayName ?? existingUser.DisplayName,
            Name = user.Name ?? existingUser.Name,
            Emails = user.Emails ?? existingUser.Emails,
            PhoneNumbers = user.PhoneNumbers ?? existingUser.PhoneNumbers,
            Active = user.Active,
            ExternalId = user.ExternalId ?? existingUser.ExternalId,
            Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = existingUser.Meta?.Created ?? DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = IncrementVersion(existingUser.Meta?.Version)
            }
        };

        _users[userId] = updatedUser;
        return Task.FromResult(updatedUser);
    }

    /// <inheritdoc />
    public Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_users.TryRemove(userId, out _))
        {
            throw new AdapterException(
                $"User with ID '{userId}' not found",
                ProviderName,
                ScimErrorType.ResourceNotFound,
                404);
        }

        // Remove user from all groups
        foreach (var groupMembers in _groupMembers.Values)
        {
            groupMembers.Remove(userId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<PagedResult<ScimUser>> ListUsersAsync(QueryFilter filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<ScimUser> users = _users.Values;

        // Apply filter if specified
        if (!string.IsNullOrEmpty(filter.Filter))
        {
            users = ApplyUserFilter(users, filter.Filter);
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(filter.SortBy))
        {
            users = filter.SortOrder == SortOrder.Descending
                ? users.OrderByDescending(GetUserSortKey(filter.SortBy))
                : users.OrderBy(GetUserSortKey(filter.SortBy));
        }

        var result = PagedResult<ScimUser>.FromFilter(users, filter);
        return Task.FromResult(result);
    }

    // ==================== Group Operations ====================

    /// <inheritdoc />
    public Task<ScimGroup> CreateGroupAsync(ScimGroup group, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Check for duplicate displayName
        if (_groups.Values.Any(g => string.Equals(g.DisplayName, group.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new AdapterException(
                $"Group with displayName '{group.DisplayName}' already exists",
                ProviderName,
                ScimErrorType.Uniqueness,
                409);
        }

        var id = $"mock-group-{Interlocked.Increment(ref _groupIdCounter)}";
        var now = DateTime.UtcNow;

        var createdGroup = new ScimGroup
        {
            Id = id,
            DisplayName = group.DisplayName,
            ExternalId = group.ExternalId,
            Members = group.Members ?? [],
            Meta = new ScimMeta
            {
                ResourceType = "Group",
                Created = now,
                LastModified = now,
                Version = "W/\"1\""
            }
        };

        _groups[id] = createdGroup;
        _groupMembers[id] = new HashSet<string>(
            group.Members?.Select(m => m.Value ?? string.Empty).Where(v => !string.IsNullOrEmpty(v)) ?? []);

        return Task.FromResult(createdGroup);
    }

    /// <inheritdoc />
    public Task<ScimGroup?> GetGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _groups.TryGetValue(groupId, out var group);
        return Task.FromResult(group);
    }

    /// <inheritdoc />
    public Task<ScimGroup> UpdateGroupAsync(string groupId, ScimGroup group, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_groups.TryGetValue(groupId, out var existingGroup))
        {
            throw new AdapterException(
                $"Group with ID '{groupId}' not found",
                ProviderName,
                ScimErrorType.ResourceNotFound,
                404);
        }

        var updatedGroup = new ScimGroup
        {
            Id = groupId,
            DisplayName = group.DisplayName ?? existingGroup.DisplayName,
            ExternalId = group.ExternalId ?? existingGroup.ExternalId,
            Members = group.Members ?? existingGroup.Members,
            Meta = new ScimMeta
            {
                ResourceType = "Group",
                Created = existingGroup.Meta?.Created ?? DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = IncrementVersion(existingGroup.Meta?.Version)
            }
        };

        _groups[groupId] = updatedGroup;

        // Update member tracking
        _groupMembers[groupId] = new HashSet<string>(
            updatedGroup.Members?.Select(m => m.Value ?? string.Empty).Where(v => !string.IsNullOrEmpty(v)) ?? []);

        return Task.FromResult(updatedGroup);
    }

    /// <inheritdoc />
    public Task DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_groups.TryRemove(groupId, out _))
        {
            throw new AdapterException(
                $"Group with ID '{groupId}' not found",
                ProviderName,
                ScimErrorType.ResourceNotFound,
                404);
        }

        _groupMembers.TryRemove(groupId, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<PagedResult<ScimGroup>> ListGroupsAsync(QueryFilter filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<ScimGroup> groups = _groups.Values;

        // Apply filter if specified
        if (!string.IsNullOrEmpty(filter.Filter))
        {
            groups = ApplyGroupFilter(groups, filter.Filter);
        }

        var result = PagedResult<ScimGroup>.FromFilter(groups, filter);
        return Task.FromResult(result);
    }

    // ==================== Membership Operations ====================

    /// <inheritdoc />
    public Task AddUserToGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_groups.ContainsKey(groupId))
        {
            throw new AdapterException(
                $"Group with ID '{groupId}' not found",
                ProviderName,
                ScimErrorType.ResourceNotFound,
                404);
        }

        var members = _groupMembers.GetOrAdd(groupId, _ => new HashSet<string>());
        members.Add(userId);

        // Update the group's member list
        if (_groups.TryGetValue(groupId, out var group))
        {
            var memberList = group.Members?.ToList() ?? [];
            if (!memberList.Any(m => m.Value == userId))
            {
                memberList.Add(new ScimGroupMember { Value = userId });
                group.Members = memberList;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveUserFromGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_groups.ContainsKey(groupId))
        {
            throw new AdapterException(
                $"Group with ID '{groupId}' not found",
                ProviderName,
                ScimErrorType.ResourceNotFound,
                404);
        }

        if (_groupMembers.TryGetValue(groupId, out var members))
        {
            members.Remove(userId);
        }

        // Update the group's member list
        if (_groups.TryGetValue(groupId, out var group))
        {
            var memberList = group.Members?.ToList() ?? [];
            memberList.RemoveAll(m => m.Value == userId);
            group.Members = memberList;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetGroupMembersAsync(string groupId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_groups.ContainsKey(groupId))
        {
            throw new AdapterException(
                $"Group with ID '{groupId}' not found",
                ProviderName,
                ScimErrorType.ResourceNotFound,
                404);
        }

        _groupMembers.TryGetValue(groupId, out var members);
        return Task.FromResult<IEnumerable<string>>(members?.ToList() ?? []);
    }

    // ==================== Transformation Operations ====================

    /// <inheritdoc />
    public Task<EntitlementMapping> MapGroupToEntitlementAsync(ScimGroup group, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Mock implementation: simple 1:1 mapping
        var mapping = new EntitlementMapping
        {
            ProviderId = ProviderName,
            ProviderEntitlementId = group.Id,
            Name = group.DisplayName,
            Type = EntitlementType.GROUP,
            MappedGroups = [group.Id],
            Enabled = true
        };

        return Task.FromResult(mapping);
    }

    /// <inheritdoc />
    public Task<ScimGroup> MapEntitlementToGroupAsync(EntitlementMapping entitlement, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Mock implementation: create a group from entitlement
        var group = new ScimGroup
        {
            Id = entitlement.ProviderEntitlementId,
            DisplayName = entitlement.Name,
            Meta = new ScimMeta
            {
                ResourceType = "Group",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            }
        };

        return Task.FromResult(group);
    }

    // ==================== Health & Diagnostics ====================

    /// <inheritdoc />
    public Task<AdapterHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        HealthStatus = new AdapterHealthStatus
        {
            Status = HealthStatusLevel.Healthy,
            LastChecked = DateTime.UtcNow,
            IsConnected = true,
            IsAuthenticated = true,
            ResponseTimeMs = 1 // Mock always fast
        };

        return Task.FromResult(HealthStatus);
    }

    /// <inheritdoc />
    public AdapterCapabilities GetCapabilities()
    {
        return new AdapterCapabilities
        {
            SupportsUsers = true,
            SupportsGroups = true,
            SupportsFiltering = true,
            SupportsSorting = true,
            SupportsPagination = true,
            SupportsPatch = true,
            SupportsBulk = false,
            SupportsEnterpriseExtension = false,
            MaxPageSize = 1000,
            SupportedFilterOperators = ["eq", "ne", "co", "sw", "ew"],
            SupportedAuthMethods = ["None"]
        };
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Clears all data from the mock adapter.
    /// </summary>
    public void Clear()
    {
        _users.Clear();
        _groups.Clear();
        _groupMembers.Clear();
        _userIdCounter = 0;
        _groupIdCounter = 0;
    }

    /// <summary>
    /// Gets the count of users in the mock adapter.
    /// </summary>
    public int UserCount => _users.Count;

    /// <summary>
    /// Gets the count of groups in the mock adapter.
    /// </summary>
    public int GroupCount => _groups.Count;

    private static string IncrementVersion(string? version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return "W/\"1\"";
        }

        // Extract version number from ETag format W/"N"
        var match = System.Text.RegularExpressions.Regex.Match(version, @"W/""(\d+)""");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var versionNum))
        {
            return $"W/\"{versionNum + 1}\"";
        }

        return "W/\"1\"";
    }

    private static IEnumerable<ScimUser> ApplyUserFilter(IEnumerable<ScimUser> users, string filter)
    {
        // Simple filter implementation for common cases
        if (filter.StartsWith("userName eq ", StringComparison.OrdinalIgnoreCase))
        {
            var value = ExtractFilterValue(filter);
            return users.Where(u => string.Equals(u.UserName, value, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.StartsWith("userName sw ", StringComparison.OrdinalIgnoreCase))
        {
            var value = ExtractFilterValue(filter);
            return users.Where(u => u.UserName?.StartsWith(value, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (filter.StartsWith("userName co ", StringComparison.OrdinalIgnoreCase))
        {
            var value = ExtractFilterValue(filter);
            return users.Where(u => u.UserName?.Contains(value, StringComparison.OrdinalIgnoreCase) == true);
        }

        return users;
    }

    private static IEnumerable<ScimGroup> ApplyGroupFilter(IEnumerable<ScimGroup> groups, string filter)
    {
        if (filter.StartsWith("displayName eq ", StringComparison.OrdinalIgnoreCase))
        {
            var value = ExtractFilterValue(filter);
            return groups.Where(g => string.Equals(g.DisplayName, value, StringComparison.OrdinalIgnoreCase));
        }

        return groups;
    }

    private static string ExtractFilterValue(string filter)
    {
        var parts = filter.Split(' ', 3);
        if (parts.Length >= 3)
        {
            return parts[2].Trim('"', '\'');
        }
        return string.Empty;
    }

    private static Func<ScimUser, object> GetUserSortKey(string sortBy)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "username" => u => u.UserName ?? string.Empty,
            "displayname" => u => u.DisplayName ?? string.Empty,
            "name.familyname" => u => u.Name?.FamilyName ?? string.Empty,
            "name.givenname" => u => u.Name?.GivenName ?? string.Empty,
            "meta.created" => u => u.Meta?.Created ?? DateTime.MinValue,
            "meta.lastmodified" => u => u.Meta?.LastModified ?? DateTime.MinValue,
            _ => u => u.UserName ?? string.Empty
        };
    }
}
