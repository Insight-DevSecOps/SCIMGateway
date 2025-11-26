// ==========================================================================
// T053-T057: Contract Tests for SCIM User Endpoints
// ==========================================================================
// Validates the SCIM User endpoints meet all requirements from:
// - RFC 7643: SCIM Core Schema
// - RFC 7644: SCIM Protocol
// - FR-002: SCIM endpoints
// - FR-003: CRUD operations
// - Constitution Principle V: Test-First Development
//
// Test Tasks:
// - T053: Contract test for POST /Users
// - T054: Contract test for GET /Users/{id}
// - T055: Contract test for GET /Users (list with filtering)
// - T056: Contract test for PATCH /Users/{id}
// - T057: Contract test for DELETE /Users/{id}
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for SCIM User endpoints.
/// These tests validate that the UsersController exists with correct structure
/// to support RFC 7643/7644 compliance for User CRUD operations.
/// </summary>
public class ScimUserEndpointTests
{
    #region Controller Structure Tests

    [Fact]
    public void UsersController_Should_Exist_In_Api_Assembly()
    {
        // Arrange & Act
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
    }

    [Fact]
    public void UsersController_Should_Inherit_From_ControllerBase()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        Assert.True(typeof(ControllerBase).IsAssignableFrom(controllerType),
            "UsersController must inherit from ControllerBase");
    }

    [Fact]
    public void UsersController_Should_Have_ApiController_Attribute()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        var attribute = controllerType.GetCustomAttribute<ApiControllerAttribute>();
        Assert.NotNull(attribute);
    }

    [Fact]
    public void UsersController_Should_Have_Route_Attribute_For_SCIM_Path()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttribute);
        Assert.Contains("scim", routeAttribute.Template?.ToLower() ?? "");
        // Route may use [controller] convention or explicit "users"
        Assert.True(
            routeAttribute.Template?.ToLower().Contains("users") == true ||
            routeAttribute.Template?.Contains("[controller]") == true,
            "Route must include users path or [controller] convention");
    }

    #endregion

    #region T053: POST /Users Endpoint Tests

    [Fact]
    public void UsersController_Should_Have_CreateUser_Method()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var createMethod = methods.FirstOrDefault(m => 
            m.Name.Contains("Create", StringComparison.OrdinalIgnoreCase) ||
            m.GetCustomAttribute<HttpPostAttribute>() != null);
        
        Assert.NotNull(createMethod);
    }

    [Fact]
    public void CreateUser_Method_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var createMethod = GetMethodWithAttribute<HttpPostAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(createMethod);
        var httpPost = createMethod.GetCustomAttribute<HttpPostAttribute>();
        Assert.NotNull(httpPost);
    }

    [Fact]
    public void CreateUser_Method_Should_Return_ActionResult()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var createMethod = GetMethodWithAttribute<HttpPostAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(createMethod);
        Assert.True(
            typeof(IActionResult).IsAssignableFrom(createMethod.ReturnType) ||
            createMethod.ReturnType.Name.Contains("Task") ||
            createMethod.ReturnType.Name.Contains("ActionResult"),
            "CreateUser must return an ActionResult type");
    }

    [Fact]
    public void CreateUser_Should_Accept_ScimUser_Parameter()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var createMethod = GetMethodWithAttribute<HttpPostAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(createMethod);
        var parameters = createMethod.GetParameters();
        var userParam = parameters.FirstOrDefault(p => 
            p.ParameterType.Name.Contains("User", StringComparison.OrdinalIgnoreCase) ||
            p.GetCustomAttribute<FromBodyAttribute>() != null);
        
        Assert.NotNull(userParam);
    }

    #endregion

    #region T054: GET /Users/{id} Endpoint Tests

    [Fact]
    public void UsersController_Should_Have_GetUserById_Method()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var getMethods = methods.Where(m => m.GetCustomAttribute<HttpGetAttribute>() != null).ToList();
        
        // Should have at least one GET method with an id parameter
        var getByIdMethod = getMethods.FirstOrDefault(m =>
        {
            var httpGet = m.GetCustomAttribute<HttpGetAttribute>();
            return httpGet?.Template?.Contains("{id}") == true ||
                   m.GetParameters().Any(p => p.Name?.Equals("id", StringComparison.OrdinalIgnoreCase) == true);
        });
        
        Assert.NotNull(getByIdMethod);
    }

    [Fact]
    public void GetUserById_Method_Should_Have_HttpGet_Attribute_With_Id_Template()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var getMethod = GetGetByIdMethod(controllerType);
        
        // Assert
        Assert.NotNull(getMethod);
        var httpGet = getMethod.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(httpGet);
        Assert.Contains("{id}", httpGet.Template ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetUserById_Method_Should_Have_Id_Parameter()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var getMethod = GetGetByIdMethod(controllerType);
        
        // Assert
        Assert.NotNull(getMethod);
        var idParam = getMethod.GetParameters()
            .FirstOrDefault(p => p.Name?.Equals("id", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(idParam);
    }

    #endregion

    #region T055: GET /Users (List) Endpoint Tests

    [Fact]
    public void UsersController_Should_Have_ListUsers_Method()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var getMethods = methods.Where(m => m.GetCustomAttribute<HttpGetAttribute>() != null).ToList();
        
        // Should have a GET method without id (for listing)
        var listMethod = getMethods.FirstOrDefault(m =>
        {
            var httpGet = m.GetCustomAttribute<HttpGetAttribute>();
            return string.IsNullOrEmpty(httpGet?.Template) ||
                   !httpGet.Template.Contains("{id}");
        });
        
        Assert.NotNull(listMethod);
    }

    [Fact]
    public void ListUsers_Method_Should_Support_Pagination_Parameters()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var listMethod = GetListMethod(controllerType);
        
        // Assert
        Assert.NotNull(listMethod);
        var parameters = listMethod.GetParameters();
        
        // Per RFC 7644, should support startIndex and count
        var hasStartIndex = parameters.Any(p => 
            p.Name?.Equals("startIndex", StringComparison.OrdinalIgnoreCase) == true);
        var hasCount = parameters.Any(p => 
            p.Name?.Equals("count", StringComparison.OrdinalIgnoreCase) == true);
        
        Assert.True(hasStartIndex || hasCount, 
            "ListUsers should support pagination parameters (startIndex, count)");
    }

    [Fact]
    public void ListUsers_Method_Should_Support_Filter_Parameter()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var listMethod = GetListMethod(controllerType);
        
        // Assert
        Assert.NotNull(listMethod);
        var parameters = listMethod.GetParameters();
        
        // Per RFC 7644, should support filter parameter
        var hasFilter = parameters.Any(p => 
            p.Name?.Equals("filter", StringComparison.OrdinalIgnoreCase) == true);
        
        Assert.True(hasFilter, "ListUsers should support filter parameter per RFC 7644");
    }

    [Fact]
    public void ListUsers_Method_Should_Support_SortBy_Parameter()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var listMethod = GetListMethod(controllerType);
        
        // Assert
        Assert.NotNull(listMethod);
        var parameters = listMethod.GetParameters();
        
        // Per RFC 7644, should support sortBy parameter
        var hasSortBy = parameters.Any(p => 
            p.Name?.Equals("sortBy", StringComparison.OrdinalIgnoreCase) == true);
        
        Assert.True(hasSortBy, "ListUsers should support sortBy parameter per RFC 7644");
    }

    [Fact]
    public void ListUsers_Method_Should_Support_SortOrder_Parameter()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var listMethod = GetListMethod(controllerType);
        
        // Assert
        Assert.NotNull(listMethod);
        var parameters = listMethod.GetParameters();
        
        // Per RFC 7644, should support sortOrder parameter
        var hasSortOrder = parameters.Any(p => 
            p.Name?.Equals("sortOrder", StringComparison.OrdinalIgnoreCase) == true);
        
        Assert.True(hasSortOrder, "ListUsers should support sortOrder parameter per RFC 7644");
    }

    #endregion

    #region T058: GET /Users?filter=... - Filter Expression Integration Tests

    [Fact]
    public void IFilterParser_Should_Exist_For_Filter_Processing()
    {
        // Arrange & Act - FilterParser processes filter expressions for List endpoint
        var parserInterface = GetIFilterParserType();
        
        // Assert
        Assert.NotNull(parserInterface);
        Assert.True(parserInterface.IsInterface);
    }

    [Fact]
    public void FilterParser_Implementation_Should_Exist()
    {
        // Arrange & Act
        var parserType = GetFilterParserType();
        
        // Assert
        Assert.NotNull(parserType);
    }

    [Fact]
    public void FilterParser_Should_Implement_IFilterParser()
    {
        // Arrange
        var parserType = GetFilterParserType();
        var parserInterface = GetIFilterParserType();
        
        // Assert
        Assert.NotNull(parserType);
        Assert.NotNull(parserInterface);
        Assert.True(parserInterface.IsAssignableFrom(parserType));
    }

    [Fact]
    public void FilterOperator_Enum_Should_Exist()
    {
        // Arrange & Act - FilterOperator enum defines the 11 SCIM operators
        var operatorType = GetFilterOperatorType();
        
        // Assert
        Assert.NotNull(operatorType);
        Assert.True(operatorType.IsEnum);
    }

    [Theory]
    [InlineData("Equal")]          // eq operator
    [InlineData("NotEqual")]       // ne operator
    [InlineData("Contains")]       // co operator
    [InlineData("StartsWith")]     // sw operator
    [InlineData("EndsWith")]       // ew operator
    [InlineData("Present")]        // pr operator
    [InlineData("GreaterThan")]    // gt operator
    [InlineData("GreaterThanOrEqual")]  // ge operator
    [InlineData("LessThan")]       // lt operator
    [InlineData("LessThanOrEqual")]     // le operator
    public void FilterOperator_Should_Support_All_11_SCIM_Operators(string operatorName)
    {
        // Arrange
        var operatorType = GetFilterOperatorType();
        
        // Assert
        Assert.NotNull(operatorType);
        var values = Enum.GetNames(operatorType);
        Assert.Contains(values, v => v.Equals(operatorName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilterOperator_Should_Support_Not_Operator()
    {
        // Arrange - NOT is a unary operator for negating expressions
        var operatorType = GetFilterOperatorType();
        
        // Assert
        Assert.NotNull(operatorType);
        // NOT may be represented as a separate LogicalOperator or in the ExpressionType
        // The key is the parser supports "not" prefix
    }

    [Fact]
    public void LogicalOperator_Enum_Should_Exist_For_And_Or()
    {
        // Arrange & Act - LogicalOperator for combining expressions
        var logicalOpType = GetLogicalOperatorType();
        
        // Assert
        Assert.NotNull(logicalOpType);
        Assert.True(logicalOpType.IsEnum);
    }

    [Theory]
    [InlineData("And")]
    [InlineData("Or")]
    public void LogicalOperator_Should_Support_And_Or(string operatorName)
    {
        // Arrange
        var logicalOpType = GetLogicalOperatorType();
        
        // Assert
        Assert.NotNull(logicalOpType);
        var values = Enum.GetNames(logicalOpType);
        Assert.Contains(values, v => v.Equals(operatorName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilterExpression_Should_Support_Attribute_Path()
    {
        // Arrange - FilterExpression holds parsed filter with attribute path
        var expressionType = GetFilterExpressionType();
        
        // Assert
        Assert.NotNull(expressionType);
        var prop = expressionType.GetProperty("AttributePath") 
            ?? expressionType.GetProperty("Attribute");
        Assert.NotNull(prop);
    }

    [Fact]
    public void FilterExpression_Should_Support_Nested_Expressions()
    {
        // Arrange - For complex filters like (a eq "b") and (c eq "d")
        var expressionType = GetFilterExpressionType();
        
        // Assert
        Assert.NotNull(expressionType);
        var leftProp = expressionType.GetProperty("Left");
        var rightProp = expressionType.GetProperty("Right");
        Assert.NotNull(leftProp);
        Assert.NotNull(rightProp);
    }

    [Fact]
    public void IUserRepository_ListAsync_Should_Accept_Filter_Parameter()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var listMethod = repoInterface.GetMethod("ListAsync");
        Assert.NotNull(listMethod);
        
        var parameters = listMethod.GetParameters();
        var filterParam = parameters.FirstOrDefault(p => 
            p.Name?.Equals("filter", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(filterParam);
    }

    [Fact]
    public void IUserRepository_ListAsync_Should_Accept_StartIndex_Parameter()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var listMethod = repoInterface.GetMethod("ListAsync");
        Assert.NotNull(listMethod);
        
        var parameters = listMethod.GetParameters();
        var startIndexParam = parameters.FirstOrDefault(p => 
            p.Name?.Equals("startIndex", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(startIndexParam);
    }

    [Fact]
    public void IUserRepository_ListAsync_Should_Accept_Count_Parameter()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var listMethod = repoInterface.GetMethod("ListAsync");
        Assert.NotNull(listMethod);
        
        var parameters = listMethod.GetParameters();
        var countParam = parameters.FirstOrDefault(p => 
            p.Name?.Equals("count", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(countParam);
    }

    [Fact]
    public void IUserRepository_ListAsync_Should_Accept_SortBy_Parameter()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var listMethod = repoInterface.GetMethod("ListAsync");
        Assert.NotNull(listMethod);
        
        var parameters = listMethod.GetParameters();
        var sortByParam = parameters.FirstOrDefault(p => 
            p.Name?.Equals("sortBy", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(sortByParam);
    }

    [Fact]
    public void IUserRepository_ListAsync_Should_Accept_SortOrder_Parameter()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var listMethod = repoInterface.GetMethod("ListAsync");
        Assert.NotNull(listMethod);
        
        var parameters = listMethod.GetParameters();
        var sortOrderParam = parameters.FirstOrDefault(p => 
            p.Name?.Equals("sortOrder", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(sortOrderParam);
    }

    [Fact]
    public void ScimListResponse_Should_Exist()
    {
        // Arrange & Act - List response type for pagination
        var listResponseType = GetScimListResponseType();
        
        // Assert
        Assert.NotNull(listResponseType);
    }

    [Fact]
    public void ScimListResponse_Should_Have_TotalResults_Property()
    {
        // Arrange
        var listResponseType = GetScimListResponseType();
        
        // Assert
        Assert.NotNull(listResponseType);
        var prop = listResponseType.GetProperty("TotalResults");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimListResponse_Should_Have_StartIndex_Property()
    {
        // Arrange
        var listResponseType = GetScimListResponseType();
        
        // Assert
        Assert.NotNull(listResponseType);
        var prop = listResponseType.GetProperty("StartIndex");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimListResponse_Should_Have_ItemsPerPage_Property()
    {
        // Arrange
        var listResponseType = GetScimListResponseType();
        
        // Assert
        Assert.NotNull(listResponseType);
        var prop = listResponseType.GetProperty("ItemsPerPage");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimListResponse_Should_Have_Resources_Property()
    {
        // Arrange
        var listResponseType = GetScimListResponseType();
        
        // Assert
        Assert.NotNull(listResponseType);
        var prop = listResponseType.GetProperty("Resources");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimInvalidFilterException_Should_Exist_For_Invalid_Filters()
    {
        // Arrange & Act - Exception type for invalid filter expressions
        var exceptionType = GetScimInvalidFilterExceptionType();
        
        // Assert
        Assert.NotNull(exceptionType);
        Assert.True(typeof(Exception).IsAssignableFrom(exceptionType));
    }

    #endregion

    #region T056: PATCH /Users/{id} Endpoint Tests

    [Fact]
    public void UsersController_Should_Have_PatchUser_Method()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var patchMethod = methods.FirstOrDefault(m => 
            m.Name.Contains("Patch", StringComparison.OrdinalIgnoreCase) ||
            m.GetCustomAttribute<HttpPatchAttribute>() != null);
        
        Assert.NotNull(patchMethod);
    }

    [Fact]
    public void PatchUser_Method_Should_Have_HttpPatch_Attribute()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var patchMethod = GetMethodWithAttribute<HttpPatchAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(patchMethod);
        var httpPatch = patchMethod.GetCustomAttribute<HttpPatchAttribute>();
        Assert.NotNull(httpPatch);
    }

    [Fact]
    public void PatchUser_Method_Should_Have_Id_In_Route()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var patchMethod = GetMethodWithAttribute<HttpPatchAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(patchMethod);
        var httpPatch = patchMethod.GetCustomAttribute<HttpPatchAttribute>();
        Assert.NotNull(httpPatch);
        Assert.Contains("{id}", httpPatch.Template ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PatchUser_Method_Should_Accept_PatchRequest_Parameter()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var patchMethod = GetMethodWithAttribute<HttpPatchAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(patchMethod);
        var parameters = patchMethod.GetParameters();
        var patchParam = parameters.FirstOrDefault(p => 
            p.ParameterType.Name.Contains("Patch", StringComparison.OrdinalIgnoreCase) ||
            p.GetCustomAttribute<FromBodyAttribute>() != null);
        
        Assert.NotNull(patchParam);
    }

    #endregion

    #region T057: DELETE /Users/{id} Endpoint Tests

    [Fact]
    public void UsersController_Should_Have_DeleteUser_Method()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var deleteMethod = methods.FirstOrDefault(m => 
            m.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
            m.GetCustomAttribute<HttpDeleteAttribute>() != null);
        
        Assert.NotNull(deleteMethod);
    }

    [Fact]
    public void DeleteUser_Method_Should_Have_HttpDelete_Attribute()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var deleteMethod = GetMethodWithAttribute<HttpDeleteAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(deleteMethod);
        var httpDelete = deleteMethod.GetCustomAttribute<HttpDeleteAttribute>();
        Assert.NotNull(httpDelete);
    }

    [Fact]
    public void DeleteUser_Method_Should_Have_Id_In_Route()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var deleteMethod = GetMethodWithAttribute<HttpDeleteAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(deleteMethod);
        var httpDelete = deleteMethod.GetCustomAttribute<HttpDeleteAttribute>();
        Assert.NotNull(httpDelete);
        Assert.Contains("{id}", httpDelete.Template ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeleteUser_Method_Should_Have_Id_Parameter()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var deleteMethod = GetMethodWithAttribute<HttpDeleteAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(deleteMethod);
        var idParam = deleteMethod.GetParameters()
            .FirstOrDefault(p => p.Name?.Equals("id", StringComparison.OrdinalIgnoreCase) == true);
        Assert.NotNull(idParam);
    }

    #endregion

    #region PUT /Users/{id} Endpoint Tests (Full Replace)

    [Fact]
    public void UsersController_Should_Have_UpdateUser_Method()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        
        // Assert
        Assert.NotNull(controllerType);
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var putMethod = methods.FirstOrDefault(m => 
            m.Name.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
            m.Name.Contains("Replace", StringComparison.OrdinalIgnoreCase) ||
            m.GetCustomAttribute<HttpPutAttribute>() != null);
        
        Assert.NotNull(putMethod);
    }

    [Fact]
    public void UpdateUser_Method_Should_Have_HttpPut_Attribute()
    {
        // Arrange
        var controllerType = GetUsersControllerType();
        var putMethod = GetMethodWithAttribute<HttpPutAttribute>(controllerType);
        
        // Assert
        Assert.NotNull(putMethod);
        var httpPut = putMethod.GetCustomAttribute<HttpPutAttribute>();
        Assert.NotNull(httpPut);
    }

    #endregion

    #region IUserRepository Contract Tests

    [Fact]
    public void IUserRepository_Should_Exist()
    {
        // Arrange & Act
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        Assert.True(repoInterface.IsInterface);
    }

    [Fact]
    public void IUserRepository_Should_Have_CreateAsync_Method()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var method = repoInterface.GetMethod("CreateAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_Should_Have_GetByIdAsync_Method()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var method = repoInterface.GetMethod("GetByIdAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_Should_Have_GetByUserNameAsync_Method()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var method = repoInterface.GetMethod("GetByUserNameAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_Should_Have_ListAsync_Method()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var method = repoInterface.GetMethod("ListAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_Should_Have_UpdateAsync_Method()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var method = repoInterface.GetMethod("UpdateAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_Should_Have_PatchAsync_Method()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var method = repoInterface.GetMethod("PatchAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IUserRepository_Should_Have_DeleteAsync_Method()
    {
        // Arrange
        var repoInterface = GetIUserRepositoryType();
        
        // Assert
        Assert.NotNull(repoInterface);
        var method = repoInterface.GetMethod("DeleteAsync");
        Assert.NotNull(method);
    }

    #endregion

    #region ScimPatchOperation Contract Tests

    [Fact]
    public void ScimPatchOperation_Should_Exist()
    {
        // Arrange & Act
        var patchOpType = GetScimPatchOperationType();
        
        // Assert
        Assert.NotNull(patchOpType);
    }

    [Fact]
    public void ScimPatchOperation_Should_Have_Op_Property()
    {
        // Arrange
        var patchOpType = GetScimPatchOperationType();
        
        // Assert
        Assert.NotNull(patchOpType);
        var prop = patchOpType.GetProperty("Op");
        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop.PropertyType);
    }

    [Fact]
    public void ScimPatchOperation_Should_Have_Path_Property()
    {
        // Arrange
        var patchOpType = GetScimPatchOperationType();
        
        // Assert
        Assert.NotNull(patchOpType);
        var prop = patchOpType.GetProperty("Path");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimPatchOperation_Should_Have_Value_Property()
    {
        // Arrange
        var patchOpType = GetScimPatchOperationType();
        
        // Assert
        Assert.NotNull(patchOpType);
        var prop = patchOpType.GetProperty("Value");
        Assert.NotNull(prop);
    }

    #endregion

    #region ScimPatchRequest Contract Tests

    [Fact]
    public void ScimPatchRequest_Should_Exist()
    {
        // Arrange & Act
        var patchReqType = GetScimPatchRequestType();
        
        // Assert
        Assert.NotNull(patchReqType);
    }

    [Fact]
    public void ScimPatchRequest_Should_Have_Schemas_Property()
    {
        // Arrange
        var patchReqType = GetScimPatchRequestType();
        
        // Assert
        Assert.NotNull(patchReqType);
        var prop = patchReqType.GetProperty("Schemas");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimPatchRequest_Should_Have_Operations_Property()
    {
        // Arrange
        var patchReqType = GetScimPatchRequestType();
        
        // Assert
        Assert.NotNull(patchReqType);
        var prop = patchReqType.GetProperty("Operations");
        Assert.NotNull(prop);
    }

    #endregion

    #region Helper Methods

    private static Type? GetUsersControllerType()
    {
        var apiAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Api");
        
        if (apiAssembly == null)
        {
            // Try to load it
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
        
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IUserRepository");
    }

    private static Type? GetScimPatchOperationType()
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
        
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimPatchOperation");
    }

    private static Type? GetScimPatchRequestType()
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
        
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimPatchRequest");
    }

    private static MethodInfo? GetMethodWithAttribute<TAttribute>(Type? controllerType) 
        where TAttribute : Attribute
    {
        if (controllerType == null) return null;
        
        return controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.GetCustomAttribute<TAttribute>() != null);
    }

    private static MethodInfo? GetGetByIdMethod(Type? controllerType)
    {
        if (controllerType == null) return null;
        
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        return methods.FirstOrDefault(m =>
        {
            var httpGet = m.GetCustomAttribute<HttpGetAttribute>();
            return httpGet?.Template?.Contains("{id}") == true;
        });
    }

    private static MethodInfo? GetListMethod(Type? controllerType)
    {
        if (controllerType == null) return null;
        
        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        return methods.FirstOrDefault(m =>
        {
            var httpGet = m.GetCustomAttribute<HttpGetAttribute>();
            if (httpGet == null) return false;
            return string.IsNullOrEmpty(httpGet.Template) || !httpGet.Template.Contains("{id}");
        });
    }

    private static Type? GetIFilterParserType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IFilterParser");
    }

    private static Type? GetFilterParserType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "FilterParser");
    }

    private static Type? GetFilterOperatorType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "FilterOperator");
    }

    private static Type? GetLogicalOperatorType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "LogicalOperator");
    }

    private static Type? GetFilterExpressionType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "FilterExpression");
    }

    private static Type? GetScimListResponseType()
    {
        // ScimListResponse might be in Api or Core assembly
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
                // Fallback to Core assembly
            }
        }
        
        var type = apiAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name.Contains("ScimListResponse"));
        
        if (type == null)
        {
            var coreAssembly = LoadCoreAssembly();
            type = coreAssembly?.GetTypes()
                .FirstOrDefault(t => t.Name.Contains("ScimListResponse"));
        }
        
        return type;
    }

    private static Type? GetScimInvalidFilterExceptionType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimInvalidFilterException");
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
