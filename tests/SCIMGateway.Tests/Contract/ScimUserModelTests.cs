// ==========================================================================
// T018a: Contract Test for ScimUser Model
// ==========================================================================
// Validates the ScimUser model meets all requirements from:
// - RFC 7643: SCIM Core Schema
// - FR-001: SCIM 2.0 schema compliance
// - tasks.md T018a specification
// 
// Required attributes to validate:
// - Core attributes: id, userName, displayName, name, emails, phoneNumbers
// - Meta attributes: resourceType, created, lastModified, location, version
// - Enterprise extension attributes
// - Internal attributes: tenantId, adapterId, syncState
// ==========================================================================

using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for ScimUser model.
/// These tests define the expected schema for SCIM User resources
/// per RFC 7643 and enterprise extensions.
/// </summary>
public class ScimUserModelTests
{
    #region Model Existence Tests

    [Fact]
    public void ScimUser_Should_Exist_In_Core_Assembly()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
    }

    #endregion

    #region RFC 7643 Required Attributes Tests

    [Fact]
    public void ScimUser_Should_Have_Schemas_Property()
    {
        // SCIM resources must include schemas array
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var schemasProperty = userType.GetProperty("Schemas");
        Assert.NotNull(schemasProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Id_Property()
    {
        // Unique identifier for the resource
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var idProperty = userType.GetProperty("Id");
        Assert.NotNull(idProperty);
        Assert.Equal(typeof(string), idProperty.PropertyType);
    }

    [Fact]
    public void ScimUser_Should_Have_UserName_Property()
    {
        // Unique identifier for the user (required by RFC 7643)
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var userNameProperty = userType.GetProperty("UserName");
        Assert.NotNull(userNameProperty);
        Assert.Equal(typeof(string), userNameProperty.PropertyType);
    }

    [Fact]
    public void ScimUser_Should_Have_ExternalId_Property()
    {
        // Identifier from the provisioning client
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var externalIdProperty = userType.GetProperty("ExternalId");
        Assert.NotNull(externalIdProperty);
    }

    #endregion

    #region Optional Core Attributes Tests

    [Fact]
    public void ScimUser_Should_Have_DisplayName_Property()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var displayNameProperty = userType.GetProperty("DisplayName");
        Assert.NotNull(displayNameProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Name_Property()
    {
        // Complex type with formatted, familyName, givenName, etc.
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var nameProperty = userType.GetProperty("Name");
        Assert.NotNull(nameProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_NickName_Property()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var nickNameProperty = userType.GetProperty("NickName");
        Assert.NotNull(nickNameProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_ProfileUrl_Property()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var profileUrlProperty = userType.GetProperty("ProfileUrl");
        Assert.NotNull(profileUrlProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Title_Property()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var titleProperty = userType.GetProperty("Title");
        Assert.NotNull(titleProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_UserType_Property()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var userTypeProperty = userType.GetProperty("UserType");
        Assert.NotNull(userTypeProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_PreferredLanguage_Property()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var preferredLanguageProperty = userType.GetProperty("PreferredLanguage");
        Assert.NotNull(preferredLanguageProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Locale_Property()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var localeProperty = userType.GetProperty("Locale");
        Assert.NotNull(localeProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Timezone_Property()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var timezoneProperty = userType.GetProperty("Timezone");
        Assert.NotNull(timezoneProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Active_Property()
    {
        // Boolean indicating if user is active
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var activeProperty = userType.GetProperty("Active");
        Assert.NotNull(activeProperty);
        Assert.Equal(typeof(bool), activeProperty.PropertyType);
    }

    [Fact]
    public void ScimUser_Should_Have_Password_Property()
    {
        // Write-only password (never returned)
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var passwordProperty = userType.GetProperty("Password");
        Assert.NotNull(passwordProperty);
    }

    #endregion

    #region Multi-Valued Attributes Tests

    [Fact]
    public void ScimUser_Should_Have_Emails_Property()
    {
        // Multi-valued email addresses
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var emailsProperty = userType.GetProperty("Emails");
        Assert.NotNull(emailsProperty);
        Assert.True(emailsProperty.PropertyType.IsGenericType);
    }

    [Fact]
    public void ScimUser_Should_Have_PhoneNumbers_Property()
    {
        // Multi-valued phone numbers
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var phoneNumbersProperty = userType.GetProperty("PhoneNumbers");
        Assert.NotNull(phoneNumbersProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Ims_Property()
    {
        // Instant messaging addresses
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var imsProperty = userType.GetProperty("Ims");
        Assert.NotNull(imsProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Photos_Property()
    {
        // URLs of user photos
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var photosProperty = userType.GetProperty("Photos");
        Assert.NotNull(photosProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Addresses_Property()
    {
        // Physical mailing addresses
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var addressesProperty = userType.GetProperty("Addresses");
        Assert.NotNull(addressesProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Groups_Property()
    {
        // Groups the user belongs to (read-only)
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var groupsProperty = userType.GetProperty("Groups");
        Assert.NotNull(groupsProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Entitlements_Property()
    {
        // Entitlements the user has
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var entitlementsProperty = userType.GetProperty("Entitlements");
        Assert.NotNull(entitlementsProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_Roles_Property()
    {
        // Roles the user has
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var rolesProperty = userType.GetProperty("Roles");
        Assert.NotNull(rolesProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_X509Certificates_Property()
    {
        // X.509 certificates
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var x509CertificatesProperty = userType.GetProperty("X509Certificates");
        Assert.NotNull(x509CertificatesProperty);
    }

    #endregion

    #region Meta Attributes Tests

    [Fact]
    public void ScimUser_Should_Have_Meta_Property()
    {
        // Metadata about the resource
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var metaProperty = userType.GetProperty("Meta");
        Assert.NotNull(metaProperty);
    }

    [Fact]
    public void ScimMeta_Should_Have_ResourceType_Property()
    {
        // "User" for user resources
        
        // Arrange & Act
        var metaType = GetScimMetaType();
        
        // Assert
        Assert.NotNull(metaType);
        var resourceTypeProperty = metaType.GetProperty("ResourceType");
        Assert.NotNull(resourceTypeProperty);
    }

    [Fact]
    public void ScimMeta_Should_Have_Created_Property()
    {
        // DateTime resource was created
        
        // Arrange & Act
        var metaType = GetScimMetaType();
        
        // Assert
        Assert.NotNull(metaType);
        var createdProperty = metaType.GetProperty("Created");
        Assert.NotNull(createdProperty);
    }

    [Fact]
    public void ScimMeta_Should_Have_LastModified_Property()
    {
        // DateTime resource was last modified
        
        // Arrange & Act
        var metaType = GetScimMetaType();
        
        // Assert
        Assert.NotNull(metaType);
        var lastModifiedProperty = metaType.GetProperty("LastModified");
        Assert.NotNull(lastModifiedProperty);
    }

    [Fact]
    public void ScimMeta_Should_Have_Location_Property()
    {
        // URI of the resource
        
        // Arrange & Act
        var metaType = GetScimMetaType();
        
        // Assert
        Assert.NotNull(metaType);
        var locationProperty = metaType.GetProperty("Location");
        Assert.NotNull(locationProperty);
    }

    [Fact]
    public void ScimMeta_Should_Have_Version_Property()
    {
        // ETag for optimistic concurrency
        
        // Arrange & Act
        var metaType = GetScimMetaType();
        
        // Assert
        Assert.NotNull(metaType);
        var versionProperty = metaType.GetProperty("Version");
        Assert.NotNull(versionProperty);
    }

    #endregion

    #region Name Complex Type Tests

    [Fact]
    public void ScimName_Should_Exist()
    {
        // Arrange & Act
        var nameType = GetScimNameType();
        
        // Assert
        Assert.NotNull(nameType);
    }

    [Fact]
    public void ScimName_Should_Have_Formatted_Property()
    {
        // Full name formatted for display
        
        // Arrange & Act
        var nameType = GetScimNameType();
        
        // Assert
        Assert.NotNull(nameType);
        var formattedProperty = nameType.GetProperty("Formatted");
        Assert.NotNull(formattedProperty);
    }

    [Fact]
    public void ScimName_Should_Have_FamilyName_Property()
    {
        // Arrange & Act
        var nameType = GetScimNameType();
        
        // Assert
        Assert.NotNull(nameType);
        var familyNameProperty = nameType.GetProperty("FamilyName");
        Assert.NotNull(familyNameProperty);
    }

    [Fact]
    public void ScimName_Should_Have_GivenName_Property()
    {
        // Arrange & Act
        var nameType = GetScimNameType();
        
        // Assert
        Assert.NotNull(nameType);
        var givenNameProperty = nameType.GetProperty("GivenName");
        Assert.NotNull(givenNameProperty);
    }

    [Fact]
    public void ScimName_Should_Have_MiddleName_Property()
    {
        // Arrange & Act
        var nameType = GetScimNameType();
        
        // Assert
        Assert.NotNull(nameType);
        var middleNameProperty = nameType.GetProperty("MiddleName");
        Assert.NotNull(middleNameProperty);
    }

    [Fact]
    public void ScimName_Should_Have_HonorificPrefix_Property()
    {
        // Arrange & Act
        var nameType = GetScimNameType();
        
        // Assert
        Assert.NotNull(nameType);
        var honorificPrefixProperty = nameType.GetProperty("HonorificPrefix");
        Assert.NotNull(honorificPrefixProperty);
    }

    [Fact]
    public void ScimName_Should_Have_HonorificSuffix_Property()
    {
        // Arrange & Act
        var nameType = GetScimNameType();
        
        // Assert
        Assert.NotNull(nameType);
        var honorificSuffixProperty = nameType.GetProperty("HonorificSuffix");
        Assert.NotNull(honorificSuffixProperty);
    }

    #endregion

    #region Email Complex Type Tests

    [Fact]
    public void ScimEmail_Should_Exist()
    {
        // Arrange & Act
        var emailType = GetScimEmailType();
        
        // Assert
        Assert.NotNull(emailType);
    }

    [Fact]
    public void ScimEmail_Should_Have_Value_Property()
    {
        // Email address value
        
        // Arrange & Act
        var emailType = GetScimEmailType();
        
        // Assert
        Assert.NotNull(emailType);
        var valueProperty = emailType.GetProperty("Value");
        Assert.NotNull(valueProperty);
    }

    [Fact]
    public void ScimEmail_Should_Have_Type_Property()
    {
        // work, home, other
        
        // Arrange & Act
        var emailType = GetScimEmailType();
        
        // Assert
        Assert.NotNull(emailType);
        var typeProperty = emailType.GetProperty("Type");
        Assert.NotNull(typeProperty);
    }

    [Fact]
    public void ScimEmail_Should_Have_Primary_Property()
    {
        // Boolean indicating primary email
        
        // Arrange & Act
        var emailType = GetScimEmailType();
        
        // Assert
        Assert.NotNull(emailType);
        var primaryProperty = emailType.GetProperty("Primary");
        Assert.NotNull(primaryProperty);
    }

    [Fact]
    public void ScimEmail_Should_Have_Display_Property()
    {
        // Human-readable display value
        
        // Arrange & Act
        var emailType = GetScimEmailType();
        
        // Assert
        Assert.NotNull(emailType);
        var displayProperty = emailType.GetProperty("Display");
        Assert.NotNull(displayProperty);
    }

    #endregion

    #region Internal Attributes Tests (SDK-specific)

    [Fact]
    public void ScimUser_Should_Have_TenantId_Property()
    {
        // Internal: tenant isolation
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var tenantIdProperty = userType.GetProperty("TenantId");
        Assert.NotNull(tenantIdProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_AdapterId_Property()
    {
        // Internal: which adapter manages this user
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var adapterIdProperty = userType.GetProperty("AdapterId");
        Assert.NotNull(adapterIdProperty);
    }

    [Fact]
    public void ScimUser_Should_Have_SyncState_Property()
    {
        // Internal: synchronization state
        
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var syncStateProperty = userType.GetProperty("SyncState");
        Assert.NotNull(syncStateProperty);
    }

    #endregion

    #region Enterprise User Extension Tests

    [Fact]
    public void EnterpriseUser_Extension_Should_Exist()
    {
        // urn:ietf:params:scim:schemas:extension:enterprise:2.0:User
        
        // Arrange & Act
        var enterpriseUserType = GetEnterpriseUserExtensionType();
        
        // Assert
        Assert.NotNull(enterpriseUserType);
    }

    [Fact]
    public void EnterpriseUser_Should_Have_EmployeeNumber_Property()
    {
        // Arrange & Act
        var enterpriseUserType = GetEnterpriseUserExtensionType();
        
        // Assert
        Assert.NotNull(enterpriseUserType);
        var employeeNumberProperty = enterpriseUserType.GetProperty("EmployeeNumber");
        Assert.NotNull(employeeNumberProperty);
    }

    [Fact]
    public void EnterpriseUser_Should_Have_CostCenter_Property()
    {
        // Arrange & Act
        var enterpriseUserType = GetEnterpriseUserExtensionType();
        
        // Assert
        Assert.NotNull(enterpriseUserType);
        var costCenterProperty = enterpriseUserType.GetProperty("CostCenter");
        Assert.NotNull(costCenterProperty);
    }

    [Fact]
    public void EnterpriseUser_Should_Have_Organization_Property()
    {
        // Arrange & Act
        var enterpriseUserType = GetEnterpriseUserExtensionType();
        
        // Assert
        Assert.NotNull(enterpriseUserType);
        var organizationProperty = enterpriseUserType.GetProperty("Organization");
        Assert.NotNull(organizationProperty);
    }

    [Fact]
    public void EnterpriseUser_Should_Have_Division_Property()
    {
        // Arrange & Act
        var enterpriseUserType = GetEnterpriseUserExtensionType();
        
        // Assert
        Assert.NotNull(enterpriseUserType);
        var divisionProperty = enterpriseUserType.GetProperty("Division");
        Assert.NotNull(divisionProperty);
    }

    [Fact]
    public void EnterpriseUser_Should_Have_Department_Property()
    {
        // Arrange & Act
        var enterpriseUserType = GetEnterpriseUserExtensionType();
        
        // Assert
        Assert.NotNull(enterpriseUserType);
        var departmentProperty = enterpriseUserType.GetProperty("Department");
        Assert.NotNull(departmentProperty);
    }

    [Fact]
    public void EnterpriseUser_Should_Have_Manager_Property()
    {
        // Manager reference with value, $ref, displayName
        
        // Arrange & Act
        var enterpriseUserType = GetEnterpriseUserExtensionType();
        
        // Assert
        Assert.NotNull(enterpriseUserType);
        var managerProperty = enterpriseUserType.GetProperty("Manager");
        Assert.NotNull(managerProperty);
    }

    #endregion

    #region Helper Methods

    private static Type? GetScimUserType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.ScimUser")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Scim.ScimUser");
    }

    private static Type? GetScimMetaType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.ScimMeta")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Scim.ScimMeta");
    }

    private static Type? GetScimNameType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.ScimName")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Scim.ScimName");
    }

    private static Type? GetScimEmailType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.ScimEmail")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Scim.ScimEmail");
    }

    private static Type? GetEnterpriseUserExtensionType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Models.EnterpriseUserExtension")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Scim.EnterpriseUserExtension")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Extensions.EnterpriseUserExtension");
    }

    #endregion
}
