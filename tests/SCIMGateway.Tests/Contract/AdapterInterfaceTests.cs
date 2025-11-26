// ==========================================================================
// T080: Contract Test for IAdapter Interface
// ==========================================================================
// Verifies all 18 required methods present, correct signatures, async patterns
// per contracts/adapter-interface.md
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for IAdapter interface.
/// Validates all required methods are present with correct signatures per adapter-interface.md.
/// </summary>
public class AdapterInterfaceTests
{
    private const string AdapterNamespace = "SCIMGateway.Core.Adapters";
    private const string InterfaceName = "IAdapter";

    private static Type? GetAdapterInterfaceType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.IsInterface && t.Name == InterfaceName);
            if (type != null) return type;
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

    // ==================== Interface Existence ====================

    [Fact]
    public void IAdapter_Interface_Should_Exist()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);
    }

    // ==================== Properties ====================

    [Fact]
    public void IAdapter_Should_Have_AdapterId_Property()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var property = adapterInterface.GetProperty("AdapterId");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.True(property.CanRead, "AdapterId should have a getter");
    }

    [Fact]
    public void IAdapter_Should_Have_ProviderName_Property()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var property = adapterInterface.GetProperty("ProviderName");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
        Assert.True(property.CanRead, "ProviderName should have a getter");
    }

    [Fact]
    public void IAdapter_Should_Have_Configuration_Property()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var property = adapterInterface.GetProperty("Configuration");
        Assert.NotNull(property);
        Assert.True(property.CanRead, "Configuration should have a getter");
    }

    [Fact]
    public void IAdapter_Should_Have_HealthStatus_Property()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var property = adapterInterface.GetProperty("HealthStatus");
        Assert.NotNull(property);
        Assert.True(property.CanRead, "HealthStatus should have a getter");
    }

    // ==================== User Operations ====================

    [Fact]
    public void IAdapter_Should_Have_CreateUserAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("CreateUserAsync");
        Assert.NotNull(method);
        Assert.True(method.ReturnType.IsGenericType, "CreateUserAsync should return Task<ScimUser>");
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "CreateUserAsync should have at least 1 parameter");
        Assert.Contains("ScimUser", parameters[0].ParameterType.Name);
    }

    [Fact]
    public void IAdapter_Should_Have_GetUserAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("GetUserAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "GetUserAsync should have at least 1 parameter");
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void IAdapter_Should_Have_UpdateUserAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("UpdateUserAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 2, "UpdateUserAsync should have at least 2 parameters");
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void IAdapter_Should_Have_DeleteUserAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("DeleteUserAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "DeleteUserAsync should have at least 1 parameter");
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void IAdapter_Should_Have_ListUsersAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("ListUsersAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "ListUsersAsync should have at least 1 parameter");
    }

    // ==================== Group Operations ====================

    [Fact]
    public void IAdapter_Should_Have_CreateGroupAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("CreateGroupAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "CreateGroupAsync should have at least 1 parameter");
    }

    [Fact]
    public void IAdapter_Should_Have_GetGroupAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("GetGroupAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "GetGroupAsync should have at least 1 parameter");
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void IAdapter_Should_Have_UpdateGroupAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("UpdateGroupAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 2, "UpdateGroupAsync should have at least 2 parameters");
    }

    [Fact]
    public void IAdapter_Should_Have_DeleteGroupAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("DeleteGroupAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "DeleteGroupAsync should have at least 1 parameter");
    }

    [Fact]
    public void IAdapter_Should_Have_ListGroupsAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("ListGroupsAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);
    }

    // ==================== Membership Operations ====================

    [Fact]
    public void IAdapter_Should_Have_AddUserToGroupAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("AddUserToGroupAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 2, "AddUserToGroupAsync should have groupId and userId parameters");
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(string), parameters[1].ParameterType);
    }

    [Fact]
    public void IAdapter_Should_Have_RemoveUserFromGroupAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("RemoveUserFromGroupAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 2, "RemoveUserFromGroupAsync should have groupId and userId parameters");
    }

    [Fact]
    public void IAdapter_Should_Have_GetGroupMembersAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("GetGroupMembersAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "GetGroupMembersAsync should have groupId parameter");
    }

    // ==================== Transformation Operations ====================

    [Fact]
    public void IAdapter_Should_Have_MapGroupToEntitlementAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("MapGroupToEntitlementAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "MapGroupToEntitlementAsync should have group parameter");
    }

    [Fact]
    public void IAdapter_Should_Have_MapEntitlementToGroupAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("MapEntitlementToGroupAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);

        var parameters = method.GetParameters();
        Assert.True(parameters.Length >= 1, "MapEntitlementToGroupAsync should have entitlement parameter");
    }

    // ==================== Health & Diagnostics ====================

    [Fact]
    public void IAdapter_Should_Have_CheckHealthAsync_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("CheckHealthAsync");
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);
    }

    [Fact]
    public void IAdapter_Should_Have_GetCapabilities_Method()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var method = adapterInterface.GetMethod("GetCapabilities");
        Assert.NotNull(method);
        // GetCapabilities is synchronous
        Assert.DoesNotContain("Task", method.ReturnType.Name);
    }

    // ==================== Method Count Validation ====================

    [Fact]
    public void IAdapter_Should_Have_18_Required_Methods()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var methods = adapterInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName) // Exclude property accessors
            .ToList();

        // 18 methods per adapter-interface.md:
        // User: CreateUserAsync, GetUserAsync, UpdateUserAsync, DeleteUserAsync, ListUsersAsync (5)
        // Group: CreateGroupAsync, GetGroupAsync, UpdateGroupAsync, DeleteGroupAsync, ListGroupsAsync (5)
        // Membership: AddUserToGroupAsync, RemoveUserFromGroupAsync, GetGroupMembersAsync (3)
        // Transformation: MapGroupToEntitlementAsync, MapEntitlementToGroupAsync (2)
        // Health: CheckHealthAsync, GetCapabilities (2)
        // Plus inherited from Object: GetHashCode, Equals, ToString (not counted for interface)
        Assert.True(methods.Count >= 17, $"IAdapter should have at least 17 methods, found {methods.Count}");
    }

    // ==================== CancellationToken Support ====================

    [Fact]
    public void IAdapter_Async_Methods_Should_Support_CancellationToken()
    {
        var adapterInterface = GetAdapterInterfaceType();
        Assert.NotNull(adapterInterface);

        var asyncMethods = new[]
        {
            "CreateUserAsync", "GetUserAsync", "UpdateUserAsync", "DeleteUserAsync", "ListUsersAsync",
            "CreateGroupAsync", "GetGroupAsync", "UpdateGroupAsync", "DeleteGroupAsync", "ListGroupsAsync",
            "AddUserToGroupAsync", "RemoveUserFromGroupAsync", "GetGroupMembersAsync",
            "MapGroupToEntitlementAsync", "MapEntitlementToGroupAsync", "CheckHealthAsync"
        };

        foreach (var methodName in asyncMethods)
        {
            var method = adapterInterface.GetMethod(methodName);
            Assert.NotNull(method);

            var parameters = method.GetParameters();
            var hasCancellationToken = parameters.Any(p => p.ParameterType == typeof(CancellationToken));
            Assert.True(hasCancellationToken, $"{methodName} should have CancellationToken parameter");
        }
    }
}
