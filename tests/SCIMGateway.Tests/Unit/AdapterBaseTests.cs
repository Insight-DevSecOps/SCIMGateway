// ==========================================================================
// T082: Unit Test for AdapterBase
// ==========================================================================
// Verifies AdapterBase abstract class provides common helper methods
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Unit;

/// <summary>
/// Unit tests for AdapterBase abstract class.
/// Validates helper methods and common functionality.
/// </summary>
public class AdapterBaseTests
{
    private static Type? GetAdapterBaseType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "AdapterBase");
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

    // ==================== Class Structure ====================

    [Fact]
    public void AdapterBase_Should_Exist()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);
    }

    [Fact]
    public void AdapterBase_Should_Be_Abstract()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);
        Assert.True(baseType.IsAbstract);
    }

    [Fact]
    public void AdapterBase_Should_Implement_IAdapter()
    {
        var baseType = GetAdapterBaseType();
        var adapterInterface = GetIAdapterType();
        
        Assert.NotNull(baseType);
        Assert.NotNull(adapterInterface);
        Assert.True(adapterInterface.IsAssignableFrom(baseType));
    }

    // ==================== Properties ====================

    [Fact]
    public void AdapterBase_Should_Have_AdapterId_Property()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var property = baseType.GetProperty("AdapterId");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void AdapterBase_Should_Have_ProviderName_Property()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var property = baseType.GetProperty("ProviderName");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void AdapterBase_Should_Have_Configuration_Property()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var property = baseType.GetProperty("Configuration");
        Assert.NotNull(property);
    }

    [Fact]
    public void AdapterBase_Should_Have_HealthStatus_Property()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var property = baseType.GetProperty("HealthStatus");
        Assert.NotNull(property);
    }

    // ==================== Helper Methods ====================

    [Fact]
    public void AdapterBase_Should_Have_GetAccessTokenAsync_Method()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var method = baseType.GetMethod("GetAccessTokenAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.Contains("Task", method.ReturnType.Name);
    }

    [Fact]
    public void AdapterBase_Should_Have_TranslateError_Method()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var method = baseType.GetMethod("TranslateError",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
    }

    [Fact]
    public void AdapterBase_Should_Have_LogOperation_Method()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var method = baseType.GetMethod("LogOperation",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
    }

    // ==================== Abstract Methods ====================

    [Fact]
    public void AdapterBase_Should_Have_Abstract_CreateUserAsync()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var method = baseType.GetMethod("CreateUserAsync");
        Assert.NotNull(method);
        Assert.True(method.IsAbstract, "CreateUserAsync should be abstract");
    }

    [Fact]
    public void AdapterBase_Should_Have_Abstract_GetUserAsync()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var method = baseType.GetMethod("GetUserAsync");
        Assert.NotNull(method);
        Assert.True(method.IsAbstract, "GetUserAsync should be abstract");
    }

    [Fact]
    public void AdapterBase_Should_Have_Abstract_UpdateUserAsync()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var method = baseType.GetMethod("UpdateUserAsync");
        Assert.NotNull(method);
        Assert.True(method.IsAbstract, "UpdateUserAsync should be abstract");
    }

    [Fact]
    public void AdapterBase_Should_Have_Abstract_DeleteUserAsync()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var method = baseType.GetMethod("DeleteUserAsync");
        Assert.NotNull(method);
        Assert.True(method.IsAbstract, "DeleteUserAsync should be abstract");
    }

    [Fact]
    public void AdapterBase_Should_Have_Abstract_ListUsersAsync()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var method = baseType.GetMethod("ListUsersAsync");
        Assert.NotNull(method);
        Assert.True(method.IsAbstract, "ListUsersAsync should be abstract");
    }

    // ==================== Constructor ====================

    [Fact]
    public void AdapterBase_Should_Have_Protected_Constructor()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var constructors = baseType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotEmpty(constructors);

        // Should have constructor accepting configuration and dependencies
        var hasConfigConstructor = constructors.Any(c => 
            c.GetParameters().Any(p => p.ParameterType.Name.Contains("Configuration")));
        Assert.True(hasConfigConstructor, "AdapterBase should have constructor accepting configuration");
    }

    // ==================== Dependencies ====================

    [Fact]
    public void AdapterBase_Should_Accept_ILogger_Dependency()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var constructors = baseType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var hasLoggerParam = constructors.Any(c =>
            c.GetParameters().Any(p => p.ParameterType.Name.Contains("ILogger")));
        Assert.True(hasLoggerParam, "AdapterBase should accept ILogger dependency");
    }

    [Fact]
    public void AdapterBase_Should_Accept_IHttpClientFactory_Dependency()
    {
        var baseType = GetAdapterBaseType();
        Assert.NotNull(baseType);

        var constructors = baseType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var hasHttpClientFactory = constructors.Any(c =>
            c.GetParameters().Any(p => p.ParameterType.Name.Contains("HttpClientFactory")));
        Assert.True(hasHttpClientFactory, "AdapterBase should accept IHttpClientFactory dependency");
    }
}
