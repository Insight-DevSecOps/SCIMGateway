// ==========================================================================
// T020a: Contract Test for EntitlementMapping Model
// ==========================================================================
// Validates the EntitlementMapping model meets all requirements from:
// - FR-022-028: Group/entitlement transformation
// - tasks.md T020a specification
// 
// Required fields to validate:
// - providerId, providerEntitlementId, name, type
// - mappedGroups, priority
// ==========================================================================

using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for EntitlementMapping model.
/// These tests define the expected schema for mapping SCIM groups
/// to provider-specific entitlements.
/// </summary>
public class EntitlementMappingTests
{
    #region Model Existence Tests

    [Fact]
    public void EntitlementMapping_Should_Exist_In_Core_Assembly()
    {
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
    }

    #endregion

    #region Required Fields Tests

    [Fact]
    public void EntitlementMapping_Should_Have_Id_Property()
    {
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
        var idProperty = mappingType.GetProperty("Id");
        Assert.NotNull(idProperty);
    }

    [Fact]
    public void EntitlementMapping_Should_Have_ProviderId_Property()
    {
        // Which provider this mapping applies to
        
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
        var providerIdProperty = mappingType.GetProperty("ProviderId");
        Assert.NotNull(providerIdProperty);
    }

    [Fact]
    public void EntitlementMapping_Should_Have_ProviderEntitlementId_Property()
    {
        // The entitlement ID in the provider system
        
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
        var providerEntitlementIdProperty = mappingType.GetProperty("ProviderEntitlementId");
        Assert.NotNull(providerEntitlementIdProperty);
    }

    [Fact]
    public void EntitlementMapping_Should_Have_Name_Property()
    {
        // Human-readable name
        
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
        var nameProperty = mappingType.GetProperty("Name");
        Assert.NotNull(nameProperty);
    }

    [Fact]
    public void EntitlementMapping_Should_Have_Type_Property()
    {
        // ROLE, PERMISSION_SET, PROFILE, ORG_UNIT, GROUP
        
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
        var typeProperty = mappingType.GetProperty("Type");
        Assert.NotNull(typeProperty);
    }

    [Fact]
    public void EntitlementMapping_Should_Have_MappedGroups_Property()
    {
        // SCIM groups that map to this entitlement
        
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
        var mappedGroupsProperty = mappingType.GetProperty("MappedGroups");
        Assert.NotNull(mappedGroupsProperty);
    }

    [Fact]
    public void EntitlementMapping_Should_Have_Priority_Property()
    {
        // Priority for conflict resolution
        
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
        var priorityProperty = mappingType.GetProperty("Priority");
        Assert.NotNull(priorityProperty);
    }

    [Fact]
    public void EntitlementMapping_Should_Have_TenantId_Property()
    {
        // Tenant isolation
        
        // Arrange & Act
        var mappingType = GetEntitlementMappingType();
        
        // Assert
        Assert.NotNull(mappingType);
        var tenantIdProperty = mappingType.GetProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
    }

    #endregion

    #region Entitlement Type Enum Tests

    [Fact]
    public void EntitlementType_Enum_Should_Exist()
    {
        // Arrange & Act
        var enumType = GetEntitlementTypeEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.True(enumType.IsEnum);
    }

    [Fact]
    public void EntitlementType_Should_Have_Role_Value()
    {
        // Arrange & Act
        var enumType = GetEntitlementTypeEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("Role", Enum.GetNames(enumType));
    }

    [Fact]
    public void EntitlementType_Should_Have_PermissionSet_Value()
    {
        // Arrange & Act
        var enumType = GetEntitlementTypeEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("PermissionSet", Enum.GetNames(enumType));
    }

    [Fact]
    public void EntitlementType_Should_Have_Profile_Value()
    {
        // Arrange & Act
        var enumType = GetEntitlementTypeEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("Profile", Enum.GetNames(enumType));
    }

    [Fact]
    public void EntitlementType_Should_Have_OrgUnit_Value()
    {
        // Arrange & Act
        var enumType = GetEntitlementTypeEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("OrgUnit", Enum.GetNames(enumType));
    }

    [Fact]
    public void EntitlementType_Should_Have_Group_Value()
    {
        // Arrange & Act
        var enumType = GetEntitlementTypeEnumType();
        
        // Assert
        Assert.NotNull(enumType);
        Assert.Contains("Group", Enum.GetNames(enumType));
    }

    #endregion

    #region Helper Methods

    private static Type? GetEntitlementMappingType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.EntitlementMapping")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Transformations.EntitlementMapping");
    }

    private static Type? GetEntitlementTypeEnumType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.EntitlementType")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Transformations.EntitlementType");
    }

    #endregion
}
