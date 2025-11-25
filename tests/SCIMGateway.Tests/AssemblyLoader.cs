// ==========================================================================
// AssemblyLoader - Ensures test assemblies are properly loaded
// ==========================================================================

using System.Reflection;
using System.Runtime.CompilerServices;

namespace SCIMGateway.Tests;

/// <summary>
/// Module initializer to ensure assemblies are loaded before any tests run.
/// </summary>
internal static class ModuleInit
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        AssemblyLoader.EnsureLoaded();
    }
}

/// <summary>
/// Helper class to ensure assemblies are loaded for reflection-based tests.
/// </summary>
public static class AssemblyLoader
{
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the SCIMGateway.Core assembly.
    /// </summary>
    public static Assembly CoreAssembly { get; private set; } = null!;

    /// <summary>
    /// Gets the SCIMGateway.Api assembly.
    /// </summary>
    public static Assembly ApiAssembly { get; private set; } = null!;

    /// <summary>
    /// Ensures the assemblies are loaded.
    /// Call this from test class constructors or [AssemblyInitialize].
    /// </summary>
    public static void EnsureLoaded()
    {
        if (_initialized) return;

        lock (_lock)
        {
            if (_initialized) return;

            // Reference types from the assemblies to force them to load
            // This ensures AppDomain.CurrentDomain.GetAssemblies() will find them
            
            // From Core assembly
            var coreType = typeof(SCIMGateway.Core.Models.ScimUser);
            CoreAssembly = coreType.Assembly;

            // From Api assembly  
            var apiType = typeof(SCIMGateway.Api.Middleware.AuthenticationMiddleware);
            ApiAssembly = apiType.Assembly;

            _initialized = true;
        }
    }

    /// <summary>
    /// Gets a type from the Core assembly by name.
    /// </summary>
    public static Type? GetCoreType(string fullTypeName)
    {
        EnsureLoaded();
        return CoreAssembly.GetType(fullTypeName);
    }

    /// <summary>
    /// Gets a type from the Api assembly by name.
    /// </summary>
    public static Type? GetApiType(string fullTypeName)
    {
        EnsureLoaded();
        return ApiAssembly.GetType(fullTypeName);
    }
}
