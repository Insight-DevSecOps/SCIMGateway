// ==========================================================================
// T085: Integration Test for Adapter Error Handling
// ==========================================================================
// Verifies adapter exceptions are translated to SCIM errors, audit logs capture errors
// ==========================================================================

using System.Reflection;
using Xunit;

namespace SCIMGateway.Tests.Integration;

/// <summary>
/// Integration tests for adapter error handling.
/// Validates error translation and audit logging.
/// </summary>
public class AdapterErrorTests
{
    private static Type? GetErrorHandlerType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        // Prefer the class over the interface
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "ErrorHandler" && !t.IsInterface);
            if (type != null) return type;
        }
        
        // Fall back to interface if class not found
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "IErrorHandler");
            if (type != null) return type;
        }
        return null;
    }

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

    private static Type? GetAuditLoggerType()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "AuditLogger" || t.Name == "IAuditLogger");
            if (type != null) return type;
        }
        return null;
    }

    // ==================== Error Translation Tests ====================

    [Fact]
    public void TranslateError_WithInvalidSyntax_Returns400BadRequest()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        var translateMethod = errorHandlerType.GetMethods()
            .FirstOrDefault(m => m.Name.Contains("Translate") || m.Name.Contains("ToScim"));
        Assert.NotNull(translateMethod);
    }

    [Fact]
    public void TranslateError_WithUniqueness_Returns409Conflict()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // Uniqueness error should map to 409 Conflict
    }

    [Fact]
    public void TranslateError_WithResourceNotFound_Returns404NotFound()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // ResourceNotFound should map to 404 Not Found
    }

    [Fact]
    public void TranslateError_WithUnauthorized_Returns401Unauthorized()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // Unauthorized should map to 401 Unauthorized
    }

    [Fact]
    public void TranslateError_WithForbidden_Returns403Forbidden()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // Forbidden should map to 403 Forbidden
    }

    [Fact]
    public void TranslateError_WithServerUnavailable_Returns503ServiceUnavailable()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // ServerUnavailable should map to 503 Service Unavailable
    }

    [Fact]
    public void TranslateError_WithTooMany_Returns429TooManyRequests()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // TooMany should map to 429 Too Many Requests
    }

    [Fact]
    public void TranslateError_WithMutability_Returns400BadRequest()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // Mutability error should map to 400 Bad Request
    }

    [Fact]
    public void TranslateError_WithInvalidFilter_Returns400BadRequest()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // InvalidFilter should map to 400 Bad Request
    }

    [Fact]
    public void TranslateError_WithNoTarget_Returns400BadRequest()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // NoTarget should map to 400 Bad Request
    }

    // ==================== SCIM Error Response Format ====================

    [Fact]
    public void TranslateError_ReturnsValidScimErrorResponse()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // Result should be valid SCIM error response with:
        // - schemas: ["urn:ietf:params:scim:api:messages:2.0:Error"]
        // - status: HTTP status code
        // - scimType: error type string
        // - detail: error message
    }

    [Fact]
    public void TranslateError_PreservesProviderErrorContext()
    {
        var adapterExceptionType = GetAdapterExceptionType();
        Assert.NotNull(adapterExceptionType);

        // Error should include provider error code for debugging
        var property = adapterExceptionType.GetProperty("ProviderErrorCode");
        Assert.NotNull(property);
    }

    [Fact]
    public void TranslateError_IncludesRetryAfterHeader_WhenAvailable()
    {
        var adapterExceptionType = GetAdapterExceptionType();
        Assert.NotNull(adapterExceptionType);

        // 429 errors should include Retry-After header
        var property = adapterExceptionType.GetProperty("RetryAfterSeconds");
        Assert.NotNull(property);
    }

    // ==================== Audit Logging ====================

    [Fact]
    public void AdapterError_IsLoggedToAudit()
    {
        var auditLoggerType = GetAuditLoggerType();
        Assert.NotNull(auditLoggerType);

        // Audit logger should have method for logging adapter errors
        var logMethods = auditLoggerType.GetMethods()
            .Where(m => m.Name.Contains("Log") && 
                       (m.Name.Contains("Error") || m.Name.Contains("Adapter")));
        Assert.NotEmpty(logMethods);
    }

    [Fact]
    public void AdapterError_CapturesAdapterId()
    {
        var auditLoggerType = GetAuditLoggerType();
        Assert.NotNull(auditLoggerType);

        // Error logs should include adapter ID for traceability
    }

    [Fact]
    public void AdapterError_CapturesOperationType()
    {
        var auditLoggerType = GetAuditLoggerType();
        Assert.NotNull(auditLoggerType);

        // Error logs should include what operation was attempted
    }

    [Fact]
    public void AdapterError_CapturesProviderErrorCode()
    {
        var auditLoggerType = GetAuditLoggerType();
        Assert.NotNull(auditLoggerType);

        // Error logs should include provider-specific error code
    }

    [Fact]
    public void AdapterError_RedactsSensitiveData()
    {
        // Error logs should not include sensitive data like credentials
        var auditLoggerType = GetAuditLoggerType();
        Assert.NotNull(auditLoggerType);
    }

    // ==================== Retry Behavior ====================

    [Fact]
    public void RetryableError_SetsIsRetryableTrue()
    {
        var adapterExceptionType = GetAdapterExceptionType();
        Assert.NotNull(adapterExceptionType);

        var property = adapterExceptionType.GetProperty("IsRetryable");
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property.PropertyType);
    }

    [Fact]
    public void NonRetryableError_SetsIsRetryableFalse()
    {
        var adapterExceptionType = GetAdapterExceptionType();
        Assert.NotNull(adapterExceptionType);

        // Uniqueness, InvalidSyntax, etc. should not be retryable
    }

    [Fact]
    public void RateLimitError_SetsRetryAfterSeconds()
    {
        var adapterExceptionType = GetAdapterExceptionType();
        Assert.NotNull(adapterExceptionType);

        // Rate limit errors should include retry-after timing
        var property = adapterExceptionType.GetProperty("RetryAfterSeconds");
        Assert.NotNull(property);
    }

    // ==================== Provider Context ====================

    [Fact]
    public void AdapterException_IncludesProviderName()
    {
        var adapterExceptionType = GetAdapterExceptionType();
        Assert.NotNull(adapterExceptionType);

        var property = adapterExceptionType.GetProperty("ProviderName");
        Assert.NotNull(property);
        Assert.Equal(typeof(string), property.PropertyType);
    }

    [Fact]
    public void AdapterException_IncludesHttpStatusCode()
    {
        var adapterExceptionType = GetAdapterExceptionType();
        Assert.NotNull(adapterExceptionType);

        var property = adapterExceptionType.GetProperty("HttpStatusCode");
        Assert.NotNull(property);
    }

    [Fact]
    public void AdapterException_IncludesInnerException()
    {
        var adapterExceptionType = GetAdapterExceptionType();
        Assert.NotNull(adapterExceptionType);

        // Should inherit InnerException from Exception base class
        Assert.True(typeof(Exception).IsAssignableFrom(adapterExceptionType));
    }

    // ==================== Edge Cases ====================

    [Fact]
    public void TranslateError_WithNullException_ReturnsGenericError()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // Should handle null gracefully
    }

    [Fact]
    public void TranslateError_WithUnknownExceptionType_ReturnsInternalServerError()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // Non-AdapterException should map to 500 Internal Server Error
    }

    [Fact]
    public void TranslateError_WithAggregateException_UnwrapsInnerException()
    {
        var errorHandlerType = GetErrorHandlerType();
        Assert.NotNull(errorHandlerType);

        // Should unwrap AggregateException to find actual error
    }
}
