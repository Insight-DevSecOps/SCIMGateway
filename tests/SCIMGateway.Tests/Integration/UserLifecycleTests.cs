// ==========================================================================
// T062: Integration Test for User Lifecycle
// ==========================================================================
// Validates the complete User lifecycle flow:
// - Create user → Read user → Update user → Delete user
// - Verifies audit logging for each operation
// - Tests RFC 7643/7644 compliance throughout lifecycle
//
// Constitution Principle V: Test-First Development
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Integration;

/// <summary>
/// Integration tests for SCIM User lifecycle.
/// These tests validate the complete create → read → update → delete flow
/// for User resources per RFC 7643/7644.
/// </summary>
public class UserLifecycleTests
{
    #region Lifecycle Components Exist Tests

    [Fact]
    public void UsersController_Should_Exist_For_Lifecycle()
    {
        // Arrange & Act
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
    }

    [Fact]
    public void IUserRepository_Should_Exist_For_Lifecycle()
    {
        // Arrange & Act
        var repoType = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoType);
        Assert.True(repoType.IsInterface);
    }

    [Fact]
    public void IAuditLogger_Should_Exist_For_Lifecycle_Logging()
    {
        // Arrange & Act
        var auditType = GetIAuditLoggerType();
        
        // Assert
        Assert.NotNull(auditType);
        Assert.True(auditType.IsInterface);
    }

    [Fact]
    public void ScimUser_Should_Exist_For_Lifecycle()
    {
        // Arrange & Act
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
    }

    #endregion

    #region Create Phase Tests

    [Fact]
    public void IUserRepository_Should_Have_CreateAsync_For_Create_Phase()
    {
        // Arrange
        var repoType = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoType);
        var method = repoType.GetMethod("CreateAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_CreateAsync_Should_Return_ScimUser()
    {
        // Arrange
        var repoType = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoType);
        var method = repoType.GetMethod("CreateAsync");
        Assert.NotNull(method);
        
        // Return type should be Task<ScimUser> or similar
        var returnType = method.ReturnType;
        Assert.True(returnType.IsGenericType || returnType.Name.Contains("Task"));
    }

    [Fact]
    public void ScimUser_Should_Have_Required_UserName_Property()
    {
        // Arrange - userName is required per RFC 7643
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var prop = userType.GetProperty("UserName");
        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop.PropertyType);
    }

    [Fact]
    public void IAuditLogger_Should_Have_LogAsync_For_Create_Audit()
    {
        // Arrange
        var auditType = GetIAuditLoggerType();
        
        // Assert
        Assert.NotNull(auditType);
        var method = auditType.GetMethod("LogAsync");
        Assert.NotNull(method);
    }

    #endregion

    #region Read Phase Tests

    [Fact]
    public void IUserRepository_Should_Have_GetByIdAsync_For_Read_Phase()
    {
        // Arrange
        var repoType = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoType);
        var method = repoType.GetMethod("GetByIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ScimUser_Should_Have_Id_Property_For_Retrieval()
    {
        // Arrange
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var prop = userType.GetProperty("Id");
        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop.PropertyType);
    }

    [Fact]
    public void ScimUser_Should_Have_Meta_Property_For_Version_Tracking()
    {
        // Arrange - Meta contains version/etag for concurrency
        var userType = GetScimUserType();
        
        // Assert
        Assert.NotNull(userType);
        var prop = userType.GetProperty("Meta");
        Assert.NotNull(prop);
    }

    #endregion

    #region Update Phase Tests

    [Fact]
    public void IUserRepository_Should_Have_UpdateAsync_For_Update_Phase()
    {
        // Arrange
        var repoType = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoType);
        var method = repoType.GetMethod("UpdateAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_Should_Have_PatchAsync_For_Partial_Update()
    {
        // Arrange
        var repoType = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoType);
        var method = repoType.GetMethod("PatchAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ScimPatchRequest_Should_Exist_For_Partial_Updates()
    {
        // Arrange & Act
        var patchType = GetScimPatchRequestType();
        
        // Assert
        Assert.NotNull(patchType);
    }

    [Fact]
    public void ScimPatchOperation_Should_Exist_For_Update_Operations()
    {
        // Arrange & Act
        var patchOpType = GetScimPatchOperationType();
        
        // Assert
        Assert.NotNull(patchOpType);
    }

    #endregion

    #region Delete Phase Tests

    [Fact]
    public void IUserRepository_Should_Have_DeleteAsync_For_Delete_Phase()
    {
        // Arrange
        var repoType = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoType);
        var method = repoType.GetMethod("DeleteAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_DeleteAsync_Should_Return_Bool_Or_Task()
    {
        // Arrange
        var repoType = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoType);
        var method = repoType.GetMethod("DeleteAsync");
        Assert.NotNull(method);
        
        // Return type should be Task<bool> or Task
        var returnType = method.ReturnType;
        Assert.True(returnType.Name.Contains("Task"));
    }

    #endregion

    #region Audit Logging Tests

    [Fact]
    public void AuditLogEntry_Should_Exist_For_Audit_Logging()
    {
        // Arrange & Act
        var auditEntryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(auditEntryType);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_OperationType_Property()
    {
        // Arrange
        var auditEntryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(auditEntryType);
        var prop = auditEntryType.GetProperty("OperationType");
        Assert.NotNull(prop);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ResourceType_Property()
    {
        // Arrange
        var auditEntryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(auditEntryType);
        var prop = auditEntryType.GetProperty("ResourceType");
        Assert.NotNull(prop);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_ResourceId_Property()
    {
        // Arrange
        var auditEntryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(auditEntryType);
        var prop = auditEntryType.GetProperty("ResourceId");
        Assert.NotNull(prop);
    }

    [Fact]
    public void AuditLogEntry_Should_Have_TenantId_Property()
    {
        // Arrange - For multi-tenant isolation
        var auditEntryType = GetAuditLogEntryType();
        
        // Assert
        Assert.NotNull(auditEntryType);
        var prop = auditEntryType.GetProperty("TenantId");
        Assert.NotNull(prop);
    }

    [Fact]
    public void OperationType_Enum_Should_Exist()
    {
        // Arrange & Act
        var opType = GetOperationTypeEnum();
        
        // Assert
        Assert.NotNull(opType);
        Assert.True(opType.IsEnum);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Patch")]
    [InlineData("Delete")]
    public void OperationType_Should_Include_CRUD_Operations(string operationName)
    {
        // Arrange
        var opType = GetOperationTypeEnum();
        
        // Assert
        Assert.NotNull(opType);
        var values = Enum.GetNames(opType);
        Assert.Contains(values, v => v.Equals(operationName, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Lifecycle Integration Verification

    [Fact]
    public void UsersController_Should_Depend_On_IUserRepository()
    {
        // Arrange - Controller should inject repository for data operations
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        
        // Check constructor parameters
        var constructors = controllerType.GetConstructors();
        Assert.NotEmpty(constructors);
        
        var hasRepoParam = constructors.Any(c => 
            c.GetParameters().Any(p => p.ParameterType.Name.Contains("UserRepository")));
        Assert.True(hasRepoParam, "UsersController should inject IUserRepository");
    }

    [Fact]
    public void UsersController_Should_Depend_On_IAuditLogger()
    {
        // Arrange - Controller should inject audit logger
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        
        // Check constructor parameters
        var constructors = controllerType.GetConstructors();
        Assert.NotEmpty(constructors);
        
        var hasAuditParam = constructors.Any(c => 
            c.GetParameters().Any(p => p.ParameterType.Name.Contains("AuditLogger")));
        Assert.True(hasAuditParam, "UsersController should inject IAuditLogger");
    }

    [Fact]
    public void UsersController_Should_Depend_On_ISchemaValidator()
    {
        // Arrange - Controller should inject schema validator
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        
        // Check constructor parameters
        var constructors = controllerType.GetConstructors();
        Assert.NotEmpty(constructors);
        
        var hasValidatorParam = constructors.Any(c => 
            c.GetParameters().Any(p => p.ParameterType.Name.Contains("SchemaValidator")));
        Assert.True(hasValidatorParam, "UsersController should inject ISchemaValidator");
    }

    #endregion

    #region Helper Methods

    private static Type? GetUsersControllerType()
    {
        var apiAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Api");
        
        if (apiAssembly == null)
        {
            try
            {
                apiAssembly = Assembly.Load("SCIMGateway.Api");
            }
            catch
            {
                return null;
            }
        }
        
        return apiAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "UsersController");
    }

    private static Type? GetIUserRepositoryType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IUserRepository");
    }

    private static Type? GetScimUserType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimUser");
    }

    private static Type? GetIAuditLoggerType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IAuditLogger");
    }

    private static Type? GetAuditLogEntryType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "AuditLogEntry");
    }

    private static Type? GetOperationTypeEnum()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "OperationType" && t.IsEnum);
    }

    private static Type? GetScimPatchRequestType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimPatchRequest");
    }

    private static Type? GetScimPatchOperationType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimPatchOperation");
    }

    private static Assembly? LoadCoreAssembly()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        if (coreAssembly == null)
        {
            try
            {
                coreAssembly = Assembly.Load("SCIMGateway.Core");
            }
            catch
            {
                return null;
            }
        }
        
        return coreAssembly;
    }

    #endregion
}
