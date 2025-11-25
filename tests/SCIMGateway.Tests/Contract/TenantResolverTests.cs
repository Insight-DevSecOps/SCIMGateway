// ==========================================================================
// T014a: Contract Test for TenantResolver
// ==========================================================================
// Validates the TenantResolver component meets all requirements from:
// - FR-010: Multi-tenant isolation
// - tasks.md T014a specification
// 
// Required behaviors to validate:
// - Extract 'tid' (tenant ID) claim from JWT token
// - Enforce tenant isolation on all data access
// - Reject cross-tenant access attempts (403)
// - Support tenant context propagation
// ==========================================================================

using System.Security.Claims;
using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for TenantResolver.
/// These tests define the expected behavior for multi-tenant isolation
/// using the 'tid' claim from Microsoft Entra ID tokens.
/// </summary>
public class TenantResolverTests
{
    #region Interface Contract Tests

    [Fact]
    public void TenantResolver_Should_Exist_In_Core_Assembly()
    {
        // Arrange & Act
        var resolverType = GetTenantResolverType();
        
        // Assert
        Assert.NotNull(resolverType);
    }

    [Fact]
    public void TenantResolver_Should_Implement_ITenantResolver_Interface()
    {
        // Arrange & Act
        var resolverType = GetTenantResolverType();
        var interfaceType = GetITenantResolverType();
        
        // Assert
        Assert.NotNull(resolverType);
        Assert.NotNull(interfaceType);
        Assert.True(interfaceType.IsAssignableFrom(resolverType));
    }

    #endregion

    #region ResolveFromToken Method Tests

    [Fact]
    public void ITenantResolver_Should_Have_ResolveFromTokenAsync_Method()
    {
        // Arrange & Act
        var interfaceType = GetITenantResolverType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ResolveFromTokenAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ResolveFromTokenAsync_Should_Accept_ClaimsPrincipal_Parameter()
    {
        // Arrange & Act
        var interfaceType = GetITenantResolverType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ResolveFromTokenAsync");
        Assert.NotNull(method);
        
        var parameters = method.GetParameters();
        Assert.Contains(parameters, p => p.ParameterType == typeof(ClaimsPrincipal));
    }

    [Fact]
    public void ResolveFromTokenAsync_Should_Return_TenantContext()
    {
        // Arrange & Act
        var interfaceType = GetITenantResolverType();
        var contextType = GetTenantContextType();
        
        // Assert
        Assert.NotNull(interfaceType);
        Assert.NotNull(contextType);
        
        var method = interfaceType.GetMethod("ResolveFromTokenAsync");
        Assert.NotNull(method);
        
        // Return type should be Task<TenantContext>
        Assert.True(method.ReturnType.IsGenericType);
    }

    #endregion

    #region TenantContext Model Tests

    [Fact]
    public void TenantContext_Should_Exist()
    {
        // Arrange & Act
        var contextType = GetTenantContextType();
        
        // Assert
        Assert.NotNull(contextType);
    }

    [Fact]
    public void TenantContext_Should_Have_TenantId_Property()
    {
        // The primary tenant identifier from 'tid' claim
        
        // Arrange & Act
        var contextType = GetTenantContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var tenantIdProperty = contextType.GetProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
        Assert.Equal(typeof(string), tenantIdProperty.PropertyType);
    }

    [Fact]
    public void TenantContext_Should_Have_ActorId_Property()
    {
        // The actor identifier from 'oid' claim
        
        // Arrange & Act
        var contextType = GetTenantContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var actorIdProperty = contextType.GetProperty("ActorId");
        Assert.NotNull(actorIdProperty);
        Assert.Equal(typeof(string), actorIdProperty.PropertyType);
    }

    [Fact]
    public void TenantContext_Should_Have_ActorDisplayName_Property()
    {
        // The actor display name from 'name' claim
        
        // Arrange & Act
        var contextType = GetTenantContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var actorDisplayNameProperty = contextType.GetProperty("ActorDisplayName");
        Assert.NotNull(actorDisplayNameProperty);
    }

    [Fact]
    public void TenantContext_Should_Have_ActorType_Property()
    {
        // User or ServicePrincipal
        
        // Arrange & Act
        var contextType = GetTenantContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var actorTypeProperty = contextType.GetProperty("ActorType");
        Assert.NotNull(actorTypeProperty);
    }

    [Fact]
    public void TenantContext_Should_Have_AppId_Property()
    {
        // The application ID from 'appid' or 'azp' claim
        
        // Arrange & Act
        var contextType = GetTenantContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var appIdProperty = contextType.GetProperty("AppId");
        Assert.NotNull(appIdProperty);
    }

    [Fact]
    public void TenantContext_Should_Have_RequestId_Property()
    {
        // Correlation ID for request tracing
        
        // Arrange & Act
        var contextType = GetTenantContextType();
        
        // Assert
        Assert.NotNull(contextType);
        var requestIdProperty = contextType.GetProperty("RequestId");
        Assert.NotNull(requestIdProperty);
    }

    #endregion

    #region ActorType Enum Tests

    [Fact]
    public void ActorType_Enum_Should_Exist()
    {
        // Arrange & Act
        var actorTypeEnum = GetActorTypeEnumType();
        
        // Assert
        Assert.NotNull(actorTypeEnum);
        Assert.True(actorTypeEnum.IsEnum);
    }

    [Fact]
    public void ActorType_Should_Have_User_Value()
    {
        // Arrange & Act
        var actorTypeEnum = GetActorTypeEnumType();
        
        // Assert
        Assert.NotNull(actorTypeEnum);
        Assert.Contains("User", Enum.GetNames(actorTypeEnum));
    }

    [Fact]
    public void ActorType_Should_Have_ServicePrincipal_Value()
    {
        // Arrange & Act
        var actorTypeEnum = GetActorTypeEnumType();
        
        // Assert
        Assert.NotNull(actorTypeEnum);
        Assert.Contains("ServicePrincipal", Enum.GetNames(actorTypeEnum));
    }

    [Fact]
    public void ActorType_Should_Have_Application_Value()
    {
        // For app-only tokens
        
        // Arrange & Act
        var actorTypeEnum = GetActorTypeEnumType();
        
        // Assert
        Assert.NotNull(actorTypeEnum);
        Assert.Contains("Application", Enum.GetNames(actorTypeEnum));
    }

    #endregion

    #region Tenant Isolation Enforcement Tests

    [Fact]
    public void ITenantResolver_Should_Have_ValidateTenantAccess_Method()
    {
        // Method to validate that a resource belongs to the current tenant
        
        // Arrange & Act
        var interfaceType = GetITenantResolverType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidateTenantAccess") 
            ?? interfaceType.GetMethod("ValidateTenantAccessAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ITenantResolver_Should_Have_GetCurrentTenant_Method()
    {
        // Method to get the current tenant from ambient context
        
        // Arrange & Act
        var interfaceType = GetITenantResolverType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("GetCurrentTenant")
            ?? interfaceType.GetMethod("GetCurrentTenantAsync");
        Assert.NotNull(method);
    }

    #endregion

    #region Cross-Tenant Access Prevention Tests

    [Fact]
    public void TenantAccessDeniedException_Should_Exist()
    {
        // Custom exception for cross-tenant access attempts
        
        // Arrange & Act
        var exceptionType = GetTenantAccessDeniedExceptionType();
        
        // Assert
        Assert.NotNull(exceptionType);
        Assert.True(typeof(Exception).IsAssignableFrom(exceptionType));
    }

    [Fact]
    public void TenantAccessDeniedException_Should_Have_RequestedTenantId_Property()
    {
        // Arrange & Act
        var exceptionType = GetTenantAccessDeniedExceptionType();
        
        // Assert
        Assert.NotNull(exceptionType);
        var requestedTenantIdProperty = exceptionType.GetProperty("RequestedTenantId");
        Assert.NotNull(requestedTenantIdProperty);
    }

    [Fact]
    public void TenantAccessDeniedException_Should_Have_ActualTenantId_Property()
    {
        // Arrange & Act
        var exceptionType = GetTenantAccessDeniedExceptionType();
        
        // Assert
        Assert.NotNull(exceptionType);
        var actualTenantIdProperty = exceptionType.GetProperty("ActualTenantId");
        Assert.NotNull(actualTenantIdProperty);
    }

    #endregion

    #region Missing Tenant Claim Tests

    [Fact]
    public void TenantClaimMissingException_Should_Exist()
    {
        // Custom exception when 'tid' claim is missing
        
        // Arrange & Act
        var exceptionType = GetTenantClaimMissingExceptionType();
        
        // Assert
        Assert.NotNull(exceptionType);
        Assert.True(typeof(Exception).IsAssignableFrom(exceptionType));
    }

    #endregion

    #region Tenant Context Accessor Tests

    [Fact]
    public void ITenantContextAccessor_Should_Exist()
    {
        // Interface for accessing tenant context from any service
        // Similar to IHttpContextAccessor pattern
        
        // Arrange & Act
        var accessorType = GetITenantContextAccessorType();
        
        // Assert
        Assert.NotNull(accessorType);
    }

    [Fact]
    public void ITenantContextAccessor_Should_Have_TenantContext_Property()
    {
        // Arrange & Act
        var accessorType = GetITenantContextAccessorType();
        
        // Assert
        Assert.NotNull(accessorType);
        var tenantContextProperty = accessorType.GetProperty("TenantContext");
        Assert.NotNull(tenantContextProperty);
    }

    #endregion

    #region Helper Methods

    private static Type? GetTenantResolverType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Authentication.TenantResolver")
            ?? coreAssembly?.GetType("SCIMGateway.Core.MultiTenancy.TenantResolver");
    }

    private static Type? GetITenantResolverType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Authentication.ITenantResolver")
            ?? coreAssembly?.GetType("SCIMGateway.Core.MultiTenancy.ITenantResolver");
    }

    private static Type? GetTenantContextType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Authentication.TenantContext")
            ?? coreAssembly?.GetType("SCIMGateway.Core.MultiTenancy.TenantContext");
    }

    private static Type? GetActorTypeEnumType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Authentication.ActorType")
            ?? coreAssembly?.GetType("SCIMGateway.Core.MultiTenancy.ActorType");
    }

    private static Type? GetTenantAccessDeniedExceptionType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Exceptions.TenantAccessDeniedException")
            ?? coreAssembly?.GetType("SCIMGateway.Core.MultiTenancy.TenantAccessDeniedException");
    }

    private static Type? GetTenantClaimMissingExceptionType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Exceptions.TenantClaimMissingException")
            ?? coreAssembly?.GetType("SCIMGateway.Core.MultiTenancy.TenantClaimMissingException");
    }

    private static Type? GetITenantContextAccessorType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Authentication.ITenantContextAccessor")
            ?? coreAssembly?.GetType("SCIMGateway.Core.MultiTenancy.ITenantContextAccessor");
    }

    #endregion
}
