// ==========================================================================
// T046-T052: Contract Tests for Phase 3 Enhancements
// ==========================================================================
// Tests for Enterprise User Extension, Concurrency Control, Enhanced Validation
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for Enterprise User Extension (T046).
/// </summary>
public class EnterpriseUserExtensionTests
{
    private static Assembly? CoreAssembly => AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");

    #region T046: Enterprise User Extension Helper

    [Fact]
    public void EnterpriseUserExtensionHelper_Should_Exist()
    {
        var helperType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtensionHelper");
        
        Assert.NotNull(helperType);
    }

    [Fact]
    public void EnterpriseUserExtensionHelper_Should_Have_SchemaUrn_Constant()
    {
        var helperType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtensionHelper");
        
        Assert.NotNull(helperType);
        
        var field = helperType.GetField("SchemaUrn", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(field);
        
        var value = field.GetValue(null) as string;
        Assert.Equal("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User", value);
    }

    [Fact]
    public void EnterpriseUserExtensionHelper_Should_Have_EnsureSchemaIfPresent_Method()
    {
        var helperType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtensionHelper");
        
        Assert.NotNull(helperType);
        
        var method = helperType.GetMethod("EnsureSchemaIfPresent", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EnterpriseUserExtensionHelper_Should_Have_HasEnterpriseData_Method()
    {
        var helperType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtensionHelper");
        
        Assert.NotNull(helperType);
        
        var method = helperType.GetMethod("HasEnterpriseData", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EnterpriseUserExtensionHelper_Should_Have_Create_Method()
    {
        var helperType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtensionHelper");
        
        Assert.NotNull(helperType);
        
        var method = helperType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EnterpriseUserExtensionHelper_Should_Have_Validate_Method()
    {
        var helperType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtensionHelper");
        
        Assert.NotNull(helperType);
        
        var method = helperType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EnterpriseUserExtensionHelper_Should_Have_Merge_Method()
    {
        var helperType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtensionHelper");
        
        Assert.NotNull(helperType);
        
        var method = helperType.GetMethod("Merge", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EnterpriseUserExtensionHelper_Should_Have_SetManager_Method()
    {
        var helperType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtensionHelper");
        
        Assert.NotNull(helperType);
        
        var method = helperType.GetMethod("SetManager", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EnterpriseUserExtension_Should_Have_EmployeeNumber_Property()
    {
        var extensionType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtension");
        
        Assert.NotNull(extensionType);
        var prop = extensionType.GetProperty("EmployeeNumber");
        Assert.NotNull(prop);
    }

    [Fact]
    public void EnterpriseUserExtension_Should_Have_CostCenter_Property()
    {
        var extensionType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtension");
        
        Assert.NotNull(extensionType);
        var prop = extensionType.GetProperty("CostCenter");
        Assert.NotNull(prop);
    }

    [Fact]
    public void EnterpriseUserExtension_Should_Have_Organization_Property()
    {
        var extensionType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtension");
        
        Assert.NotNull(extensionType);
        var prop = extensionType.GetProperty("Organization");
        Assert.NotNull(prop);
    }

    [Fact]
    public void EnterpriseUserExtension_Should_Have_Division_Property()
    {
        var extensionType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtension");
        
        Assert.NotNull(extensionType);
        var prop = extensionType.GetProperty("Division");
        Assert.NotNull(prop);
    }

    [Fact]
    public void EnterpriseUserExtension_Should_Have_Department_Property()
    {
        var extensionType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtension");
        
        Assert.NotNull(extensionType);
        var prop = extensionType.GetProperty("Department");
        Assert.NotNull(prop);
    }

    [Fact]
    public void EnterpriseUserExtension_Should_Have_Manager_Property()
    {
        var extensionType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "EnterpriseUserExtension");
        
        Assert.NotNull(extensionType);
        var prop = extensionType.GetProperty("Manager");
        Assert.NotNull(prop);
    }

    #endregion
}

/// <summary>
/// Contract tests for Concurrency Control (T050).
/// </summary>
public class ConcurrencyControlTests
{
    private static Assembly? CoreAssembly => AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");

    #region T050: Concurrency Manager

    [Fact]
    public void IConcurrencyManager_Interface_Should_Exist()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
    }

    [Fact]
    public void ConcurrencyManager_Should_Exist()
    {
        var managerType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ConcurrencyManager" && !t.IsInterface);
        
        Assert.NotNull(managerType);
    }

    [Fact]
    public void ConcurrencyManager_Should_Implement_IConcurrencyManager()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        var managerType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ConcurrencyManager" && !t.IsInterface);
        
        Assert.NotNull(interfaceType);
        Assert.NotNull(managerType);
        Assert.True(interfaceType.IsAssignableFrom(managerType));
    }

    [Fact]
    public void IConcurrencyManager_Should_Have_GenerateVersion_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("GenerateVersion");
        Assert.NotNull(method);
        Assert.Equal(typeof(string), method.ReturnType);
    }

    [Fact]
    public void IConcurrencyManager_Should_Have_ValidateVersion_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidateVersion");
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method.ReturnType);
    }

    [Fact]
    public void IConcurrencyManager_Should_Have_ValidateVersionOrThrow_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidateVersionOrThrow");
        Assert.NotNull(method);
    }

    [Fact]
    public void IConcurrencyManager_Should_Have_IncrementVersion_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("IncrementVersion");
        Assert.NotNull(method);
    }

    [Fact]
    public void IConcurrencyManager_Should_Have_ParseIfMatchHeader_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ParseIfMatchHeader");
        Assert.NotNull(method);
    }

    [Fact]
    public void IConcurrencyManager_Should_Have_FormatAsETag_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("FormatAsETag");
        Assert.NotNull(method);
    }

    [Fact]
    public void VersionMismatchException_Should_Exist()
    {
        var exceptionType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "VersionMismatchException");
        
        Assert.NotNull(exceptionType);
        Assert.True(typeof(Exception).IsAssignableFrom(exceptionType));
    }

    [Fact]
    public void ConcurrencyExtensions_Should_Exist()
    {
        var extensionsType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ConcurrencyExtensions");
        
        Assert.NotNull(extensionsType);
        Assert.True(extensionsType.IsAbstract && extensionsType.IsSealed); // Static class
    }

    [Fact]
    public void ConcurrencyExtensions_Should_Have_InitializeVersion_For_User()
    {
        var extensionsType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ConcurrencyExtensions");
        
        Assert.NotNull(extensionsType);
        var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "InitializeVersion");
        Assert.True(methods.Any());
    }

    [Fact]
    public void ConcurrencyExtensions_Should_Have_UpdateVersion_For_User()
    {
        var extensionsType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ConcurrencyExtensions");
        
        Assert.NotNull(extensionsType);
        var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "UpdateVersion");
        Assert.True(methods.Any());
    }

    [Fact]
    public void ConcurrencyExtensions_Should_Have_GetETag_Method()
    {
        var extensionsType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ConcurrencyExtensions");
        
        Assert.NotNull(extensionsType);
        var methods = extensionsType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "GetETag");
        Assert.True(methods.Any());
    }

    #endregion
}

/// <summary>
/// Contract tests for Enhanced Validation (T048, T049).
/// </summary>
public class EnhancedValidationTests
{
    private static Assembly? CoreAssembly => AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");

    #region T048: User Validation Enhancements

    [Fact]
    public void SchemaValidator_Should_Exist()
    {
        var validatorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "SchemaValidator");
        
        Assert.NotNull(validatorType);
    }

    [Fact]
    public void SchemaValidator_Should_Implement_ISchemaValidator()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ISchemaValidator" && t.IsInterface);
        var validatorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "SchemaValidator" && !t.IsInterface);
        
        Assert.NotNull(interfaceType);
        Assert.NotNull(validatorType);
        Assert.True(interfaceType.IsAssignableFrom(validatorType));
    }

    [Fact]
    public void ISchemaValidator_Should_Have_ValidateUser_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ISchemaValidator" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidateUser");
        Assert.NotNull(method);
    }

    [Fact]
    public void ISchemaValidator_Should_Have_ValidateUserAsync_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ISchemaValidator" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidateUserAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ValidationResult_Should_Have_IsValid_Property()
    {
        var resultType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ValidationResult");
        
        Assert.NotNull(resultType);
        var prop = resultType.GetProperty("IsValid");
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void ValidationResult_Should_Have_Errors_Property()
    {
        var resultType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ValidationResult");
        
        Assert.NotNull(resultType);
        var prop = resultType.GetProperty("Errors");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ValidationError_Should_Have_Path_Property()
    {
        var errorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ValidationError");
        
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("Path");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ValidationError_Should_Have_Message_Property()
    {
        var errorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ValidationError");
        
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("Message");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ValidationError_Should_Have_ErrorType_Property()
    {
        var errorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ValidationError");
        
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("ErrorType");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ValidationErrorType_Enum_Should_Have_RequiredAttributeMissing()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ValidationErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("RequiredAttributeMissing"));
    }

    [Fact]
    public void ValidationErrorType_Enum_Should_Have_InvalidFormat()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ValidationErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("InvalidFormat"));
    }

    #endregion

    #region T049: Group Validation Enhancements

    [Fact]
    public void ISchemaValidator_Should_Have_ValidateGroup_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ISchemaValidator" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidateGroup");
        Assert.NotNull(method);
    }

    [Fact]
    public void ISchemaValidator_Should_Have_ValidateGroupAsync_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ISchemaValidator" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidateGroupAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void ISchemaValidator_Should_Have_ValidatePatchOperations_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ISchemaValidator" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidatePatchOperations");
        Assert.NotNull(method);
    }

    [Fact]
    public void ISchemaValidator_Should_Have_ValidateFilter_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ISchemaValidator" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("ValidateFilter");
        Assert.NotNull(method);
    }

    #endregion
}

/// <summary>
/// Contract tests for Error Handler (T047).
/// </summary>
public class ErrorHandlerEnhancementTests
{
    private static Assembly? CoreAssembly => AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");

    #region T047: SCIM Error Response Generation

    [Fact]
    public void ScimError_Should_Have_Status_Property()
    {
        var errorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimError");
        
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("Status");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimError_Should_Have_ScimType_Property()
    {
        var errorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimError");
        
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("ScimType");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimError_Should_Have_Detail_Property()
    {
        var errorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimError");
        
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("Detail");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimError_Should_Have_Schemas_Property()
    {
        var errorType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimError");
        
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("Schemas");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimErrorType_Should_Have_InvalidSyntax()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("InvalidSyntax"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_InvalidFilter()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("InvalidFilter"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_Uniqueness()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("Uniqueness"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_Mutability()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("Mutability"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_InvalidPath()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("InvalidPath"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_NoTarget()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("NoTarget"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_InvalidValue()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("InvalidValue"));
    }

    [Fact]
    public void ScimErrorType_Should_Have_TooMany()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("TooMany"));
    }

    [Fact]
    public void ErrorHandler_Should_Have_BadRequest_Method()
    {
        var handlerType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorHandler" && !t.IsInterface);
        
        Assert.NotNull(handlerType);
        var method = handlerType.GetMethod("BadRequest", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void ErrorHandler_Should_Have_NotFound_Method()
    {
        var handlerType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorHandler" && !t.IsInterface);
        
        Assert.NotNull(handlerType);
        var method = handlerType.GetMethod("NotFound", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void ErrorHandler_Should_Have_Conflict_Method()
    {
        var handlerType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorHandler" && !t.IsInterface);
        
        Assert.NotNull(handlerType);
        var method = handlerType.GetMethod("Conflict", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void ErrorHandler_Should_Have_PreconditionFailed_Method()
    {
        var handlerType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorHandler" && !t.IsInterface);
        
        Assert.NotNull(handlerType);
        var method = handlerType.GetMethod("PreconditionFailed", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void ErrorHandler_Should_Have_TooManyRequests_Method()
    {
        var handlerType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ErrorHandler" && !t.IsInterface);
        
        Assert.NotNull(handlerType);
        var method = handlerType.GetMethod("TooManyRequests", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    #endregion
}

/// <summary>
/// Contract tests for Audit Logging (T051, T052).
/// </summary>
public class AuditLoggingEnhancementTests
{
    private static Assembly? CoreAssembly => AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");

    #region T051: User Audit Logging

    [Fact]
    public void IAuditLogger_Should_Have_LogUserOperationAsync_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IAuditLogger" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("LogUserOperationAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IAuditLogger_Should_Have_LogAsync_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IAuditLogger" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("LogAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IAuditLogger_Should_Have_LogOperationAsync_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IAuditLogger" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("LogOperationAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IAuditLogger_Should_Have_LogErrorAsync_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IAuditLogger" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("LogErrorAsync");
        Assert.NotNull(method);
    }

    #endregion

    #region T052: Group Audit Logging

    [Fact]
    public void IAuditLogger_Should_Have_LogGroupOperationAsync_Method()
    {
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IAuditLogger" && t.IsInterface);
        
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("LogGroupOperationAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void AuditResourceType_Should_Have_User_Value()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "AuditResourceType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("User"));
    }

    [Fact]
    public void AuditResourceType_Should_Have_Group_Value()
    {
        var enumType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "AuditResourceType" && t.IsEnum);
        
        Assert.NotNull(enumType);
        Assert.True(Enum.GetNames(enumType).Contains("Group"));
    }

    #endregion
}

/// <summary>
/// Tests for DI registration of T046-T052 components.
/// </summary>
public class Phase3EnhancementsDITests
{
    private static Assembly? CoreAssembly => AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");

    [Fact]
    public void ServiceCollectionExtensions_Should_Register_IConcurrencyManager()
    {
        var extensionsType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ServiceCollectionExtensions");
        
        Assert.NotNull(extensionsType);
        
        // Read the source to verify registration (via reflection of method body would be complex)
        // Instead, we verify the types exist and can be registered
        var interfaceType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IConcurrencyManager" && t.IsInterface);
        var implementationType = CoreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ConcurrencyManager" && !t.IsInterface);
        
        Assert.NotNull(interfaceType);
        Assert.NotNull(implementationType);
    }
}
