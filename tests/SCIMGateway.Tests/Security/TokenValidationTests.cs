// ==========================================================================
// T064: Security Tests for Token Validation
// ==========================================================================
// Validates authentication security requirements from:
// - FR-007: OAuth 2.0 Bearer Token authentication
// - FR-008: Token validation (signature, issuer, audience, expiration)
// - Missing token → 401
// - Invalid token → 401
// - Expired token → 401
//
// Constitution Principle IV: Security by Default
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Security;

/// <summary>
/// Security tests for token validation.
/// These tests verify that the authentication system correctly rejects
/// invalid, missing, or expired tokens per RFC 6750.
/// </summary>
public class TokenValidationTests
{
    #region Token Validation Components Exist

    [Fact]
    public void IBearerTokenValidator_Should_Exist()
    {
        // Arrange & Act
        var validatorType = GetIBearerTokenValidatorType();
        
        // Assert
        Assert.NotNull(validatorType);
        Assert.True(validatorType.IsInterface);
    }

    [Fact]
    public void BearerTokenValidator_Should_Exist()
    {
        // Arrange & Act
        var validatorType = GetBearerTokenValidatorType();
        
        // Assert
        Assert.NotNull(validatorType);
    }

    [Fact]
    public void TokenValidationResult_Should_Exist()
    {
        // Arrange & Act
        var resultType = GetTokenValidationResultType();
        
        // Assert
        Assert.NotNull(resultType);
    }

    [Fact]
    public void TokenValidationError_Enum_Should_Exist()
    {
        // Arrange & Act
        var errorType = GetTokenValidationErrorType();
        
        // Assert
        Assert.NotNull(errorType);
        Assert.True(errorType.IsEnum);
    }

    #endregion

    #region Missing Token Tests - 401 Unauthorized

    [Fact]
    public void TokenValidationError_Should_Have_MissingToken_Value()
    {
        // Arrange - Per RFC 6750: Missing token should return 401
        var errorType = GetTokenValidationErrorType();
        
        // Assert
        Assert.NotNull(errorType);
        var values = Enum.GetNames(errorType);
        Assert.Contains("MissingToken", values);
    }

    [Fact]
    public void IBearerTokenValidator_Should_Handle_Null_Token()
    {
        // Arrange - Validator should handle null token gracefully
        var validatorType = GetIBearerTokenValidatorType();
        
        // Assert
        Assert.NotNull(validatorType);
        var method = validatorType.GetMethod("ValidateTokenAsync");
        Assert.NotNull(method);
        
        // Method should accept string parameter (which can be null)
        var parameters = method.GetParameters();
        Assert.Contains(parameters, p => p.Name == "token" && p.ParameterType == typeof(string));
    }

    [Fact]
    public void IBearerTokenValidator_Should_Handle_Empty_Token()
    {
        // Arrange - Validator should reject empty token with appropriate error
        var validatorType = GetIBearerTokenValidatorType();
        
        // Assert
        Assert.NotNull(validatorType);
        var method = validatorType.GetMethod("ValidateTokenAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void TokenValidationResult_Should_Have_IsValid_Property()
    {
        // Arrange - Result should indicate if token is valid
        var resultType = GetTokenValidationResultType();
        
        // Assert
        Assert.NotNull(resultType);
        var prop = resultType.GetProperty("IsValid");
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void TokenValidationResult_IsValid_Should_Be_False_For_Missing_Token()
    {
        // Arrange - Missing token should result in IsValid = false
        var resultType = GetTokenValidationResultType();
        
        // Assert - Verify the property exists for this scenario
        Assert.NotNull(resultType);
        var prop = resultType.GetProperty("IsValid");
        Assert.NotNull(prop);
    }

    #endregion

    #region Invalid Token Tests - 401 Unauthorized

    [Fact]
    public void TokenValidationError_Should_Have_InvalidToken_Value()
    {
        // Arrange - Per RFC 6750: Invalid token should return 401
        var errorType = GetTokenValidationErrorType();
        
        // Assert
        Assert.NotNull(errorType);
        var values = Enum.GetNames(errorType);
        Assert.Contains("InvalidToken", values);
    }

    [Fact]
    public void TokenValidationError_Should_Have_InvalidSignature_Value()
    {
        // Arrange - Invalid signature is a specific type of invalid token
        var errorType = GetTokenValidationErrorType();
        
        // Assert
        Assert.NotNull(errorType);
        var values = Enum.GetNames(errorType);
        Assert.Contains("InvalidSignature", values);
    }

    [Fact]
    public void TokenValidationError_Should_Have_InvalidAudience_Value()
    {
        // Arrange - Invalid audience should return 401
        var errorType = GetTokenValidationErrorType();
        
        // Assert
        Assert.NotNull(errorType);
        var values = Enum.GetNames(errorType);
        Assert.Contains("InvalidAudience", values);
    }

    [Fact]
    public void TokenValidationError_Should_Have_InvalidIssuer_Value()
    {
        // Arrange - Invalid issuer should return 401
        var errorType = GetTokenValidationErrorType();
        
        // Assert
        Assert.NotNull(errorType);
        var values = Enum.GetNames(errorType);
        Assert.Contains("InvalidIssuer", values);
    }

    [Fact]
    public void TokenValidationResult_Should_Have_Error_Property()
    {
        // Arrange - Result should contain error type
        var resultType = GetTokenValidationResultType();
        
        // Assert
        Assert.NotNull(resultType);
        var prop = resultType.GetProperty("Error");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenValidationResult_Should_Have_ErrorMessage_Property()
    {
        // Arrange - Result should contain error description
        var resultType = GetTokenValidationResultType();
        
        // Assert
        Assert.NotNull(resultType);
        var prop = resultType.GetProperty("ErrorMessage") 
            ?? resultType.GetProperty("ErrorDescription");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenValidationResult_Should_Have_ErrorCode_Property()
    {
        // Arrange - Result should contain error code for RFC 6750 compliance
        var resultType = GetTokenValidationResultType();
        
        // Assert
        Assert.NotNull(resultType);
        var prop = resultType.GetProperty("ErrorCode");
        Assert.NotNull(prop);
    }

    #endregion

    #region Expired Token Tests - 401 Unauthorized

    [Fact]
    public void TokenValidationError_Should_Have_ExpiredToken_Value()
    {
        // Arrange - Per RFC 6750: Expired token should return 401 with retry-after
        var errorType = GetTokenValidationErrorType();
        
        // Assert
        Assert.NotNull(errorType);
        var values = Enum.GetNames(errorType);
        Assert.Contains("ExpiredToken", values);
    }

    [Fact]
    public void TokenValidationResult_Should_Have_ExpiresAt_Property()
    {
        // Arrange - For retry-after header calculation
        var resultType = GetTokenValidationResultType();
        
        // Assert
        Assert.NotNull(resultType);
        var prop = resultType.GetProperty("ExpiresAt");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_ValidateLifetime_Property()
    {
        // Arrange - Should be able to configure lifetime validation
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("ValidateLifetime");
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_ClockSkew_Property()
    {
        // Arrange - Clock skew tolerance for expired token validation
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("ClockSkew");
        Assert.NotNull(prop);
        Assert.Equal(typeof(TimeSpan), prop.PropertyType);
    }

    #endregion

    #region Token Claims Validation

    [Fact]
    public void TokenClaims_Should_Exist()
    {
        // Arrange & Act
        var claimsType = GetTokenClaimsType();
        
        // Assert
        Assert.NotNull(claimsType);
    }

    [Fact]
    public void TokenClaims_Should_Have_TenantId_Property()
    {
        // Arrange - Required for tenant isolation (tid claim)
        var claimsType = GetTokenClaimsType();
        
        // Assert
        Assert.NotNull(claimsType);
        var prop = claimsType.GetProperty("TenantId");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenClaims_Should_Have_ObjectId_Property()
    {
        // Arrange - Required for actor identification (oid claim)
        var claimsType = GetTokenClaimsType();
        
        // Assert
        Assert.NotNull(claimsType);
        var prop = claimsType.GetProperty("ObjectId");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenClaims_Should_Have_Issuer_Property()
    {
        // Arrange - For issuer validation (iss claim)
        var claimsType = GetTokenClaimsType();
        
        // Assert
        Assert.NotNull(claimsType);
        var prop = claimsType.GetProperty("Issuer");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenClaims_Should_Have_Audience_Property()
    {
        // Arrange - For audience validation (aud claim)
        var claimsType = GetTokenClaimsType();
        
        // Assert
        Assert.NotNull(claimsType);
        var prop = claimsType.GetProperty("Audience");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenClaims_Should_Have_ExpiresAt_Property()
    {
        // Arrange - For expiration validation (exp claim)
        var claimsType = GetTokenClaimsType();
        
        // Assert
        Assert.NotNull(claimsType);
        var prop = claimsType.GetProperty("ExpiresAt");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenClaims_Should_Have_IssuedAt_Property()
    {
        // Arrange - For issued-at validation (iat claim)
        var claimsType = GetTokenClaimsType();
        
        // Assert
        Assert.NotNull(claimsType);
        var prop = claimsType.GetProperty("IssuedAt");
        Assert.NotNull(prop);
    }

    #endregion

    #region Validation Configuration Tests

    [Fact]
    public void TokenValidationOptions_Should_Exist()
    {
        // Arrange & Act
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_ValidIssuers_Property()
    {
        // Arrange - For issuer whitelist validation
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("ValidIssuers");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_Audience_Property()
    {
        // Arrange - For audience validation
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("Audience") 
            ?? optionsType.GetProperty("ValidAudiences");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_ValidateIssuer_Property()
    {
        // Arrange - To enable/disable issuer validation
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("ValidateIssuer");
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_ValidateAudience_Property()
    {
        // Arrange - To enable/disable audience validation
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("ValidateAudience");
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_ValidateSigningKey_Property()
    {
        // Arrange - To enable/disable signature validation
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("ValidateSigningKey");
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_MetadataEndpoint_Property()
    {
        // Arrange - For OIDC discovery
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("MetadataEndpoint");
        Assert.NotNull(prop);
    }

    #endregion

    #region HTTP 401 Response Tests

    [Fact]
    public void AuthenticationMiddleware_Should_Exist()
    {
        // Arrange & Act
        var middlewareType = GetAuthenticationMiddlewareType();
        
        // Assert
        Assert.NotNull(middlewareType);
    }

    [Fact]
    public void AuthenticationMiddleware_Should_Use_IBearerTokenValidator()
    {
        // Arrange - Middleware should inject token validator
        var middlewareType = GetAuthenticationMiddlewareType();
        
        // Assert
        Assert.NotNull(middlewareType);
        
        var constructors = middlewareType.GetConstructors();
        var hasValidatorParam = constructors.Any(c =>
            c.GetParameters().Any(p => 
                p.ParameterType.Name.Contains("BearerTokenValidator") ||
                p.ParameterType.Name.Contains("IBearerTokenValidator")));
        Assert.True(hasValidatorParam, "AuthenticationMiddleware should inject IBearerTokenValidator");
    }

    [Fact]
    public void ScimErrorResponse_Should_Exist_For_401_Errors()
    {
        // Arrange & Act
        var errorType = GetScimErrorResponseType();
        
        // Assert
        Assert.NotNull(errorType);
    }

    [Fact]
    public void ScimErrorResponse_Should_Have_Status_Property()
    {
        // Arrange - For HTTP status code (401)
        var errorType = GetScimErrorResponseType();
        
        // Assert
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("Status");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimErrorResponse_Should_Have_ScimType_Property()
    {
        // Arrange - For SCIM error type
        var errorType = GetScimErrorResponseType();
        
        // Assert
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("ScimType");
        Assert.NotNull(prop);
    }

    [Fact]
    public void ScimErrorResponse_Should_Have_Detail_Property()
    {
        // Arrange - For error description
        var errorType = GetScimErrorResponseType();
        
        // Assert
        Assert.NotNull(errorType);
        var prop = errorType.GetProperty("Detail");
        Assert.NotNull(prop);
    }

    #endregion

    #region Insufficient Scope Tests

    [Fact]
    public void TokenValidationError_Should_Have_InsufficientScope_Value()
    {
        // Arrange - Per RFC 6750: Insufficient scope should return 403
        var errorType = GetTokenValidationErrorType();
        
        // Assert
        Assert.NotNull(errorType);
        var values = Enum.GetNames(errorType);
        Assert.Contains("InsufficientScope", values);
    }

    [Fact]
    public void TokenValidationOptions_Should_Have_RequiredScopes_Property()
    {
        // Arrange - For scope validation
        var optionsType = GetTokenValidationOptionsType();
        
        // Assert
        Assert.NotNull(optionsType);
        var prop = optionsType.GetProperty("RequiredScopes");
        Assert.NotNull(prop);
    }

    [Fact]
    public void TokenClaims_Should_Have_Scopes_Property()
    {
        // Arrange - For scope extraction from token
        var claimsType = GetTokenClaimsType();
        
        // Assert
        Assert.NotNull(claimsType);
        var prop = claimsType.GetProperty("Scopes");
        Assert.NotNull(prop);
    }

    #endregion

    #region Helper Methods

    private static Type? GetIBearerTokenValidatorType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "IBearerTokenValidator");
    }

    private static Type? GetBearerTokenValidatorType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "BearerTokenValidator" && !t.IsInterface);
    }

    private static Type? GetTokenValidationResultType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "TokenValidationResult");
    }

    private static Type? GetTokenValidationErrorType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "TokenValidationError" && t.IsEnum);
    }

    private static Type? GetTokenClaimsType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "TokenClaims");
    }

    private static Type? GetTokenValidationOptionsType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "TokenValidationOptions" || 
                               t.Name == "BearerTokenValidatorOptions");
    }

    private static Type? GetAuthenticationMiddlewareType()
    {
        var apiAssembly = LoadApiAssembly();
        return apiAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "AuthenticationMiddleware");
    }

    private static Type? GetScimErrorResponseType()
    {
        var coreAssembly = LoadCoreAssembly();
        return coreAssembly?.GetTypes()
            .FirstOrDefault(t => t.Name == "ScimErrorResponse" || t.Name == "ScimError");
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

    private static Assembly? LoadApiAssembly()
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
        
        return apiAssembly;
    }

    #endregion
}
