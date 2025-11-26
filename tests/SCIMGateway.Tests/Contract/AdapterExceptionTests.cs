// ==========================================================================
// T081: Contract Test for AdapterException Error Mapping
// ==========================================================================
// Verifies AdapterException properly maps to SCIM errors per adapter-interface.md
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for AdapterException and SCIM error mapping.
/// </summary>
public class AdapterExceptionTests
{
    private static Type? GetAdapterExceptionType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "AdapterException");
            if (type != null) return type;
        }
        return null;
    }

    private static Type? GetScimErrorTypeEnum()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.IsEnum && t.Name == "ScimErrorType");
            if (type != null) return type;
        }
        return null;
    }

    // ==================== AdapterException Structure ====================

    [Fact]
    public void AdapterException_Should_Exist()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);
    }

    [Fact]
    public void AdapterException_Should_Inherit_From_Exception()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);
        Assert.True(typeof(Exception).IsAssignableFrom(exceptionType));
    }

    [Fact]
    public void AdapterException_Should_Have_ProviderName_Property()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);

        var property = exceptionType.GetProperty("ProviderName");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void AdapterException_Should_Have_HttpStatusCode_Property()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);

        var property = exceptionType.GetProperty("HttpStatusCode");
        Assert.NotNull(property);
        // Should be nullable int
        Assert.True(property.PropertyType == typeof(int?) || property.PropertyType == typeof(int));
    }

    [Fact]
    public void AdapterException_Should_Have_ProviderErrorCode_Property()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);

        var property = exceptionType.GetProperty("ProviderErrorCode");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void AdapterException_Should_Have_ScimErrorType_Property()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);

        var property = exceptionType.GetProperty("ScimErrorType");
        Assert.NotNull(property);
    }

    [Fact]
    public void AdapterException_Should_Have_IsRetryable_Property()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);

        var property = exceptionType.GetProperty("IsRetryable");
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property.PropertyType);
    }

    [Fact]
    public void AdapterException_Should_Have_RetryAfterSeconds_Property()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);

        var property = exceptionType.GetProperty("RetryAfterSeconds");
        Assert.NotNull(property);
    }

    // ==================== ScimErrorType Enum ====================

    [Fact]
    public void ScimErrorType_Enum_Should_Exist()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(enumType.IsEnum);
    }

    [Fact]
    public void ScimErrorType_Should_Have_InvalidSyntax()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("InvalidSyntax"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_Uniqueness()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("Uniqueness"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_Mutability()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("Mutability"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_InvalidFilter()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("InvalidFilter"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_NoTarget()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("NoTarget"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_TooMany()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("TooMany"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_ServerUnavailable()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("ServerUnavailable"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_ResourceNotFound()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("ResourceNotFound"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_Unauthorized()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("Unauthorized"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_Forbidden()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("Forbidden"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_All_Required_Values()
    {
        var enumType = GetScimErrorTypeEnum();
        Assert.NotNull(enumType);

        var expectedValues = new[]
        {
            "InvalidSyntax", "Uniqueness", "Mutability", "InvalidFilter",
            "NoTarget", "TooMany", "ServerUnavailable", "ResourceNotFound",
            "Unauthorized", "Forbidden"
        };

        var actualValues = Enum.GetNames(enumType);
        foreach (var expected in expectedValues)
        {
            Assert.Contains(expected, actualValues);
        }
    }

    // ==================== Constructor Tests ====================

    [Fact]
    public void AdapterException_Should_Have_Constructor_With_Message_And_ProviderName()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);

        var constructor = exceptionType.GetConstructor(new[] { typeof(string), typeof(string) });
        Assert.NotNull(constructor);
    }

    [Fact]
    public void AdapterException_Should_Have_Constructor_With_InnerException()
    {
        var exceptionType = GetAdapterExceptionType();
        Assert.NotNull(exceptionType);

        var constructor = exceptionType.GetConstructor(new[] { typeof(string), typeof(string), typeof(Exception) });
        Assert.NotNull(constructor);
    }
}
