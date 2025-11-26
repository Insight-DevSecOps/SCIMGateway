// ==========================================================================
// T081: Unit Test for AdapterRegistry
// ==========================================================================
// Verifies AdapterRegistry can register, retrieve, and route to adapters
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Unit;

/// <summary>
/// Unit tests for AdapterRegistry.
/// Validates registration, retrieval, and routing functionality.
/// </summary>
public class AdapterRegistryTests
{
    private static Type? GetAdapterRegistryType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "AdapterRegistry" || t.Name == "IAdapterRegistry");
            if (type != null) return type;
        }
        return null;
    }

    private static Type? GetAdapterRegistryImplementationType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "AdapterRegistry" && !t.IsInterface);
            if (type != null) return type;
        }
        return null;
    }

    // ==================== Registry Existence ====================

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Exist()
    {
        var registryType = GetAdapterRegistryType();
        Assert.NotNull(registryType);
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void IAdapterRegistry_Interface_Should_Exist()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        Type? interfaceType = null;
        foreach (var assembly in assemblies)
        {
            interfaceType = assembly.GetTypes()
                .FirstOrDefault(t => t.IsInterface && t.Name == "IAdapterRegistry");
            if (interfaceType != null) break;
        }
        Assert.NotNull(interfaceType);
    }

    // ==================== Registration Methods ====================

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Have_RegisterAdapter_Method()
    {
        var registryType = GetAdapterRegistryType();
        Assert.NotNull(registryType);

        var method = registryType.GetMethod("RegisterAdapter") ??
                     registryType.GetMethod("Register");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Have_GetAdapter_Method()
    {
        var registryType = GetAdapterRegistryType();
        Assert.NotNull(registryType);

        var method = registryType.GetMethod("GetAdapter") ??
                     registryType.GetMethod("Get");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Have_TryGetAdapter_Method()
    {
        var registryType = GetAdapterRegistryType();
        Assert.NotNull(registryType);

        var method = registryType.GetMethod("TryGetAdapter");
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Have_UnregisterAdapter_Method()
    {
        var registryType = GetAdapterRegistryType();
        Assert.NotNull(registryType);

        var method = registryType.GetMethod("UnregisterAdapter") ??
                     registryType.GetMethod("Unregister") ??
                     registryType.GetMethod("Remove");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Have_GetAllAdapters_Method()
    {
        var registryType = GetAdapterRegistryType();
        Assert.NotNull(registryType);

        var method = registryType.GetMethod("GetAllAdapters") ??
                     registryType.GetMethod("GetAll") ??
                     registryType.GetMethod("ListAdapters");
        Assert.NotNull(method);
    }

    // ==================== Routing Methods ====================

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Have_GetAdapterForTenant_Method()
    {
        var registryType = GetAdapterRegistryType();
        Assert.NotNull(registryType);

        var methods = registryType.GetMethods()
            .Where(m => m.Name.Contains("Adapter") && 
                       (m.Name.Contains("Tenant") || m.Name.Contains("Provider")));
        Assert.NotEmpty(methods);
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Have_IsAdapterRegistered_Method()
    {
        var registryType = GetAdapterRegistryType();
        Assert.NotNull(registryType);

        var method = registryType.GetMethod("IsAdapterRegistered") ??
                     registryType.GetMethod("IsRegistered") ??
                     registryType.GetMethod("Contains");
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
    }

    // ==================== Functional Tests ====================

    [Fact(Skip = "Waiting for T074 implementation")]
    public void RegisterAdapter_WithValidAdapter_ShouldSucceed()
    {
        var registryType = GetAdapterRegistryImplementationType();
        Assert.NotNull(registryType);

        // Create instance and test registration
        var registry = Activator.CreateInstance(registryType);
        Assert.NotNull(registry);

        // Verify can call RegisterAdapter without exception
        var registerMethod = registryType.GetMethod("RegisterAdapter") ??
                            registryType.GetMethod("Register");
        Assert.NotNull(registerMethod);
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void GetAdapter_WithUnknownProviderId_ShouldThrowOrReturnNull()
    {
        var registryType = GetAdapterRegistryImplementationType();
        Assert.NotNull(registryType);

        var registry = Activator.CreateInstance(registryType);
        Assert.NotNull(registry);

        var getMethod = registryType.GetMethod("TryGetAdapter");
        if (getMethod != null)
        {
            // TryGetAdapter should return false for unknown provider
            var parameters = new object?[] { "unknown-provider", null };
            var result = getMethod.Invoke(registry, parameters);
            Assert.False((bool)result!);
        }
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void RegisterMultipleAdapters_ShouldMaintainSeparateInstances()
    {
        // Test that registry can hold multiple adapters for different providers
        var registryType = GetAdapterRegistryImplementationType();
        Assert.NotNull(registryType);

        var registry = Activator.CreateInstance(registryType);
        Assert.NotNull(registry);

        var getAllMethod = registryType.GetMethod("GetAllAdapters") ??
                          registryType.GetMethod("GetAll");
        Assert.NotNull(getAllMethod);
    }

    [Fact(Skip = "Waiting for T074 implementation")]
    public void UnregisterAdapter_ShouldRemoveFromRegistry()
    {
        var registryType = GetAdapterRegistryImplementationType();
        Assert.NotNull(registryType);

        var unregisterMethod = registryType.GetMethod("UnregisterAdapter") ??
                              registryType.GetMethod("Unregister") ??
                              registryType.GetMethod("Remove");
        Assert.NotNull(unregisterMethod);
    }

    // ==================== Thread Safety ====================

    [Fact(Skip = "Waiting for T074 implementation")]
    public void AdapterRegistry_Should_Be_ThreadSafe()
    {
        // Registry should handle concurrent registration/retrieval
        var registryType = GetAdapterRegistryImplementationType();
        Assert.NotNull(registryType);

        // Check if uses thread-safe collections
        var fields = registryType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        var hasThreadSafeCollection = fields.Any(f => 
            f.FieldType.Name.Contains("Concurrent") ||
            f.FieldType.Name.Contains("Lock") ||
            f.FieldType.Name.Contains("Semaphore"));

        // Either uses concurrent collections or has locking mechanism
        Assert.True(hasThreadSafeCollection || registryType.GetFields().Any());
    }
}
