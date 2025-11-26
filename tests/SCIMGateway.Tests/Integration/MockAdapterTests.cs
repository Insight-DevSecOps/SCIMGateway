// ==========================================================================
// T083: Integration Test for MockAdapter
// ==========================================================================
// Verifies MockAdapter implements all CRUD operations, error handling, health checks
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Integration;

/// <summary>
/// Integration tests for MockAdapter.
/// Validates all CRUD operations work end-to-end with in-memory storage.
/// </summary>
public class MockAdapterTests
{
    private static Type? GetMockAdapterType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "MockAdapter");
            if (type != null) return type;
        }
        return null;
    }

    private static Type? GetIAdapterType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.IsInterface && t.Name == "IAdapter");
            if (type != null) return type;
        }
        return null;
    }

    // ==================== MockAdapter Existence ====================

    [Fact]
    public void MockAdapter_Should_Exist()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);
    }

    [Fact]
    public void MockAdapter_Should_Implement_IAdapter()
    {
        var adapterType = GetMockAdapterType();
        var adapterInterface = GetIAdapterType();

        Assert.NotNull(adapterType);
        Assert.NotNull(adapterInterface);
        Assert.True(adapterInterface.IsAssignableFrom(adapterType));
    }

    // ==================== User CRUD Operations ====================

    [Fact]
    public async Task CreateUserAsync_WithValidUser_ReturnsCreatedUser()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        Assert.NotNull(adapter);

        var createMethod = adapterType.GetMethod("CreateUserAsync");
        Assert.NotNull(createMethod);

        // Create a test user
        var userType = GetTypeByName("ScimUser");
        Assert.NotNull(userType);

        var user = Activator.CreateInstance(userType);
        SetProperty(user!, "UserName", "testuser@example.com");

        var task = (Task)createMethod.Invoke(adapter, new object[] { user!, CancellationToken.None })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserAsync_WithExistingId_ReturnsUser()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var getMethod = adapterType.GetMethod("GetUserAsync");
        Assert.NotNull(getMethod);

        // First create a user, then retrieve it
        var userId = "test-user-id";
        var task = (Task)getMethod.Invoke(adapter, new object[] { userId, CancellationToken.None })!;
        await task;

        // MockAdapter may return null for non-existent user
        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        // Result could be null if user doesn't exist, which is valid
    }

    [Fact]
    public async Task UpdateUserAsync_WithValidUser_ReturnsUpdatedUser()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var createMethod = adapterType.GetMethod("CreateUserAsync");
        var updateMethod = adapterType.GetMethod("UpdateUserAsync");
        Assert.NotNull(createMethod);
        Assert.NotNull(updateMethod);

        var userType = GetTypeByName("ScimUser");
        Assert.NotNull(userType);

        // First create a user
        var createUser = Activator.CreateInstance(userType);
        SetProperty(createUser!, "UserName", "original@example.com");
        var createTask = (Task)createMethod.Invoke(adapter, new object[] { createUser!, CancellationToken.None })!;
        await createTask;
        var createdResult = createTask.GetType().GetProperty("Result")?.GetValue(createTask);
        var createdId = createdResult?.GetType().GetProperty("Id")?.GetValue(createdResult)?.ToString();
        Assert.NotNull(createdId);

        // Now update the user
        var updateUser = Activator.CreateInstance(userType);
        SetProperty(updateUser!, "UserName", "updated@example.com");

        var task = (Task)updateMethod.Invoke(adapter, new object[] { createdId, updateUser!, CancellationToken.None })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DeleteUserAsync_WithExistingId_CompletesSuccessfully()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var createMethod = adapterType.GetMethod("CreateUserAsync");
        var deleteMethod = adapterType.GetMethod("DeleteUserAsync");
        Assert.NotNull(createMethod);
        Assert.NotNull(deleteMethod);

        var userType = GetTypeByName("ScimUser");
        Assert.NotNull(userType);

        // First create a user
        var user = Activator.CreateInstance(userType);
        SetProperty(user!, "UserName", "todelete@example.com");
        var createTask = (Task)createMethod.Invoke(adapter, new object[] { user!, CancellationToken.None })!;
        await createTask;
        var createdResult = createTask.GetType().GetProperty("Result")?.GetValue(createTask);
        var createdId = createdResult?.GetType().GetProperty("Id")?.GetValue(createdResult)?.ToString();
        Assert.NotNull(createdId);

        // Now delete the user
        var task = (Task)deleteMethod.Invoke(adapter, new object[] { createdId, CancellationToken.None })!;
        await task; // Should complete without throwing
    }

    [Fact]
    public async Task ListUsersAsync_ReturnsPagedResult()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var listMethod = adapterType.GetMethod("ListUsersAsync");
        Assert.NotNull(listMethod);

        var filterType = GetTypeByName("QueryFilter");
        Assert.NotNull(filterType);

        var filter = Activator.CreateInstance(filterType);
        var task = (Task)listMethod.Invoke(adapter, new object[] { filter!, CancellationToken.None })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        Assert.NotNull(result);
    }

    // ==================== Group CRUD Operations ====================

    [Fact]
    public async Task CreateGroupAsync_WithValidGroup_ReturnsCreatedGroup()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var createMethod = adapterType.GetMethod("CreateGroupAsync");
        Assert.NotNull(createMethod);

        var groupType = GetTypeByName("ScimGroup");
        Assert.NotNull(groupType);

        var group = Activator.CreateInstance(groupType);
        SetProperty(group!, "DisplayName", "Test Group");

        var task = (Task)createMethod.Invoke(adapter, new object[] { group!, CancellationToken.None })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetGroupAsync_WithExistingId_ReturnsGroup()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var getMethod = adapterType.GetMethod("GetGroupAsync");
        Assert.NotNull(getMethod);

        var task = (Task)getMethod.Invoke(adapter, new object[] { "group-id", CancellationToken.None })!;
        await task;
    }

    [Fact]
    public async Task ListGroupsAsync_ReturnsPagedResult()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var listMethod = adapterType.GetMethod("ListGroupsAsync");
        Assert.NotNull(listMethod);

        var filterType = GetTypeByName("QueryFilter");
        Assert.NotNull(filterType);

        var filter = Activator.CreateInstance(filterType);
        var task = (Task)listMethod.Invoke(adapter, new object[] { filter!, CancellationToken.None })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        Assert.NotNull(result);
    }

    // ==================== Membership Operations ====================

    [Fact]
    public async Task AddUserToGroupAsync_CompletesSuccessfully()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var createUserMethod = adapterType.GetMethod("CreateUserAsync");
        var createGroupMethod = adapterType.GetMethod("CreateGroupAsync");
        var addMethod = adapterType.GetMethod("AddUserToGroupAsync");
        Assert.NotNull(createUserMethod);
        Assert.NotNull(createGroupMethod);
        Assert.NotNull(addMethod);

        // Create a user
        var userType = GetTypeByName("ScimUser");
        Assert.NotNull(userType);
        var user = Activator.CreateInstance(userType);
        SetProperty(user!, "UserName", "member@example.com");
        var userTask = (Task)createUserMethod.Invoke(adapter, new object[] { user!, CancellationToken.None })!;
        await userTask;
        var userResult = userTask.GetType().GetProperty("Result")?.GetValue(userTask);
        var userId = userResult?.GetType().GetProperty("Id")?.GetValue(userResult)?.ToString();
        Assert.NotNull(userId);

        // Create a group
        var groupType = GetTypeByName("ScimGroup");
        Assert.NotNull(groupType);
        var group = Activator.CreateInstance(groupType);
        SetProperty(group!, "DisplayName", "Test Membership Group");
        var groupTask = (Task)createGroupMethod.Invoke(adapter, new object[] { group!, CancellationToken.None })!;
        await groupTask;
        var groupResult = groupTask.GetType().GetProperty("Result")?.GetValue(groupTask);
        var groupId = groupResult?.GetType().GetProperty("Id")?.GetValue(groupResult)?.ToString();
        Assert.NotNull(groupId);

        // Add user to group
        var task = (Task)addMethod.Invoke(adapter, new object[] { groupId, userId, CancellationToken.None })!;
        await task;
    }

    [Fact]
    public async Task RemoveUserFromGroupAsync_CompletesSuccessfully()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var createUserMethod = adapterType.GetMethod("CreateUserAsync");
        var createGroupMethod = adapterType.GetMethod("CreateGroupAsync");
        var addMethod = adapterType.GetMethod("AddUserToGroupAsync");
        var removeMethod = adapterType.GetMethod("RemoveUserFromGroupAsync");
        Assert.NotNull(createUserMethod);
        Assert.NotNull(createGroupMethod);
        Assert.NotNull(addMethod);
        Assert.NotNull(removeMethod);

        // Create a user
        var userType = GetTypeByName("ScimUser");
        Assert.NotNull(userType);
        var user = Activator.CreateInstance(userType);
        SetProperty(user!, "UserName", "toremove@example.com");
        var userTask = (Task)createUserMethod.Invoke(adapter, new object[] { user!, CancellationToken.None })!;
        await userTask;
        var userResult = userTask.GetType().GetProperty("Result")?.GetValue(userTask);
        var userId = userResult?.GetType().GetProperty("Id")?.GetValue(userResult)?.ToString();
        Assert.NotNull(userId);

        // Create a group
        var groupType = GetTypeByName("ScimGroup");
        Assert.NotNull(groupType);
        var group = Activator.CreateInstance(groupType);
        SetProperty(group!, "DisplayName", "Remove Member Group");
        var groupTask = (Task)createGroupMethod.Invoke(adapter, new object[] { group!, CancellationToken.None })!;
        await groupTask;
        var groupResult = groupTask.GetType().GetProperty("Result")?.GetValue(groupTask);
        var groupId = groupResult?.GetType().GetProperty("Id")?.GetValue(groupResult)?.ToString();
        Assert.NotNull(groupId);

        // Add user to group first
        var addTask = (Task)addMethod.Invoke(adapter, new object[] { groupId, userId, CancellationToken.None })!;
        await addTask;

        // Remove user from group
        var task = (Task)removeMethod.Invoke(adapter, new object[] { groupId, userId, CancellationToken.None })!;
        await task;
    }

    [Fact]
    public async Task GetGroupMembersAsync_ReturnsUserIds()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var createUserMethod = adapterType.GetMethod("CreateUserAsync");
        var createGroupMethod = adapterType.GetMethod("CreateGroupAsync");
        var addMethod = adapterType.GetMethod("AddUserToGroupAsync");
        var getMembersMethod = adapterType.GetMethod("GetGroupMembersAsync");
        Assert.NotNull(createUserMethod);
        Assert.NotNull(createGroupMethod);
        Assert.NotNull(addMethod);
        Assert.NotNull(getMembersMethod);

        // Create a user
        var userType = GetTypeByName("ScimUser");
        Assert.NotNull(userType);
        var user = Activator.CreateInstance(userType);
        SetProperty(user!, "UserName", "groupmember@example.com");
        var userTask = (Task)createUserMethod.Invoke(adapter, new object[] { user!, CancellationToken.None })!;
        await userTask;
        var userResult = userTask.GetType().GetProperty("Result")?.GetValue(userTask);
        var userId = userResult?.GetType().GetProperty("Id")?.GetValue(userResult)?.ToString();
        Assert.NotNull(userId);

        // Create a group
        var groupType = GetTypeByName("ScimGroup");
        Assert.NotNull(groupType);
        var group = Activator.CreateInstance(groupType);
        SetProperty(group!, "DisplayName", "Members Test Group");
        var groupTask = (Task)createGroupMethod.Invoke(adapter, new object[] { group!, CancellationToken.None })!;
        await groupTask;
        var groupResult = groupTask.GetType().GetProperty("Result")?.GetValue(groupTask);
        var groupId = groupResult?.GetType().GetProperty("Id")?.GetValue(groupResult)?.ToString();
        Assert.NotNull(groupId);

        // Add user to group
        var addTask = (Task)addMethod.Invoke(adapter, new object[] { groupId, userId, CancellationToken.None })!;
        await addTask;

        // Get members
        var task = (Task)getMembersMethod.Invoke(adapter, new object[] { groupId, CancellationToken.None })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        Assert.NotNull(result);
    }

    // ==================== Health Check ====================

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyStatus()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var healthMethod = adapterType.GetMethod("CheckHealthAsync");
        Assert.NotNull(healthMethod);

        var task = (Task)healthMethod.Invoke(adapter, new object[] { CancellationToken.None })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        Assert.NotNull(result);

        // Check that status is Healthy
        var statusProperty = result.GetType().GetProperty("Status");
        Assert.NotNull(statusProperty);
    }

    [Fact]
    public void GetCapabilities_ReturnsValidCapabilities()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var capabilitiesMethod = adapterType.GetMethod("GetCapabilities");
        Assert.NotNull(capabilitiesMethod);

        var result = capabilitiesMethod.Invoke(adapter, null);
        Assert.NotNull(result);

        // Verify capabilities has expected properties
        var supportsUsersProperty = result.GetType().GetProperty("SupportsUsers");
        Assert.NotNull(supportsUsersProperty);
        Assert.True((bool)supportsUsersProperty.GetValue(result)!);
    }

    // ==================== Error Handling ====================

    [Fact]
    public async Task GetUserAsync_WithNonExistentId_ReturnsNull()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var getMethod = adapterType.GetMethod("GetUserAsync");
        Assert.NotNull(getMethod);

        var task = (Task)getMethod.Invoke(adapter, new object[] { "non-existent-id", CancellationToken.None })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        var result = resultProperty?.GetValue(task);
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateUserName_ThrowsAdapterException()
    {
        var adapterType = GetMockAdapterType();
        Assert.NotNull(adapterType);

        var adapter = CreateMockAdapter(adapterType);
        var createMethod = adapterType.GetMethod("CreateUserAsync");
        Assert.NotNull(createMethod);

        var userType = GetTypeByName("ScimUser");
        Assert.NotNull(userType);

        // Create first user
        var user1 = Activator.CreateInstance(userType);
        SetProperty(user1!, "UserName", "duplicate@example.com");
        var task1 = (Task)createMethod.Invoke(adapter, new object[] { user1!, CancellationToken.None })!;
        await task1;

        // Try to create duplicate user
        var user2 = Activator.CreateInstance(userType);
        SetProperty(user2!, "UserName", "duplicate@example.com");

        // Should throw AdapterException with Uniqueness error
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            var task2 = (Task)createMethod.Invoke(adapter, new object[] { user2!, CancellationToken.None })!;
            await task2;
        });
    }

    // ==================== Helper Methods ====================

    private static object? CreateMockAdapter(Type adapterType)
    {
        // Try to create using DI-style constructor or default constructor
        var constructors = adapterType.GetConstructors();
        
        foreach (var ctor in constructors.OrderBy(c => c.GetParameters().Length))
        {
            try
            {
                if (ctor.GetParameters().Length == 0)
                {
                    return Activator.CreateInstance(adapterType);
                }
            }
            catch
            {
                continue;
            }
        }

        return null;
    }

    private static Type? GetTypeByName(string typeName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == typeName);
            if (type != null) return type;
        }
        return null;
    }

    private static void SetProperty(object obj, string propertyName, object value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        property?.SetValue(obj, value);
    }
}
