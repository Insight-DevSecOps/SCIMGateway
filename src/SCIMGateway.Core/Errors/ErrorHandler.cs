// ==========================================================================
// T024: ErrorHandler - SCIM Error Response Generation
// ==========================================================================
// Translates exceptions to SCIM error responses per RFC 7644
// ==========================================================================

using System.Net;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Errors;

/// <summary>
/// Interface for SCIM error handling.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Creates a SCIM error response.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="scimType">SCIM error type.</param>
    /// <param name="detail">Error detail message.</param>
    /// <returns>SCIM error response.</returns>
    ScimError CreateScimError(HttpStatusCode statusCode, ScimErrorType? scimType, string detail);

    /// <summary>
    /// Handles an exception and converts it to a SCIM error.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>SCIM error response.</returns>
    ScimError HandleException(Exception exception);

    /// <summary>
    /// Handles an exception asynchronously and converts it to a SCIM error.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>SCIM error response.</returns>
    Task<ScimError> HandleExceptionAsync(Exception exception);
}

/// <summary>
/// SCIM error response per RFC 7644.
/// </summary>
public class ScimError
{
    /// <summary>
    /// SCIM schemas for error response.
    /// </summary>
    public List<string> Schemas { get; set; } =
    [
        ScimConstants.Schemas.Error
    ];

    /// <summary>
    /// HTTP status code as string.
    /// </summary>
    public string Status { get; set; } = "500";

    /// <summary>
    /// SCIM error type.
    /// </summary>
    public string? ScimType { get; set; }

    /// <summary>
    /// Human-readable error description.
    /// </summary>
    public string? Detail { get; set; }
}

/// <summary>
/// SCIM error types per RFC 7644.
/// </summary>
public enum ScimErrorType
{
    /// <summary>
    /// Request syntax is invalid (400).
    /// </summary>
    InvalidSyntax,

    /// <summary>
    /// Filter expression is invalid (400).
    /// </summary>
    InvalidFilter,

    /// <summary>
    /// Too many results to return (400).
    /// </summary>
    TooMany,

    /// <summary>
    /// Uniqueness constraint violation (409).
    /// </summary>
    Uniqueness,

    /// <summary>
    /// Attribute is read-only (400).
    /// </summary>
    Mutability,

    /// <summary>
    /// PATCH path is invalid (400).
    /// </summary>
    InvalidPath,

    /// <summary>
    /// Target attribute is missing (400).
    /// </summary>
    NoTarget,

    /// <summary>
    /// Value doesn't match schema (400).
    /// </summary>
    InvalidValue,

    /// <summary>
    /// Attribute is read-only and cannot be modified (400).
    /// </summary>
    InvalidVers,

    /// <summary>
    /// Sensitive attribute cannot be returned (400).
    /// </summary>
    Sensitive,

    /// <summary>
    /// The server is not in a state to process the request (503).
    /// </summary>
    ServerUnavailable,

    /// <summary>
    /// The requested resource was not found (404).
    /// </summary>
    ResourceNotFound,

    /// <summary>
    /// Authorization failure - invalid credentials or token (401).
    /// </summary>
    Unauthorized,

    /// <summary>
    /// The client is not authorized to perform the requested operation (403).
    /// </summary>
    Forbidden,

    /// <summary>
    /// An internal server error occurred (500).
    /// </summary>
    InternalError,

    /// <summary>
    /// Rate limit exceeded (429).
    /// </summary>
    RateLimitExceeded,

    /// <summary>
    /// Request timeout (408).
    /// </summary>
    Timeout,

    /// <summary>
    /// Unknown or unspecified error type.
    /// </summary>
    Unknown
}

/// <summary>
/// Error handler implementation for SCIM error responses.
/// </summary>
public class ErrorHandler : IErrorHandler
{
    /// <inheritdoc />
    public ScimError CreateScimError(HttpStatusCode statusCode, ScimErrorType? scimType, string detail)
    {
        return new ScimError
        {
            Status = ((int)statusCode).ToString(),
            ScimType = scimType?.ToString()?.ToLowerInvariant(),
            Detail = detail
        };
    }

    /// <inheritdoc />
    public ScimError HandleException(Exception exception)
    {
        return exception switch
        {
            ScimException scimEx => CreateScimError(scimEx.StatusCode, scimEx.ScimType, scimEx.Message),
            
            ArgumentNullException argEx => CreateScimError(
                HttpStatusCode.BadRequest, 
                ScimErrorType.InvalidValue, 
                $"Missing required parameter: {argEx.ParamName}"),
            
            ArgumentException argEx => CreateScimError(
                HttpStatusCode.BadRequest, 
                ScimErrorType.InvalidValue, 
                argEx.Message),
            
            UnauthorizedAccessException => CreateScimError(
                HttpStatusCode.Forbidden, 
                null, 
                "Access denied"),
            
            KeyNotFoundException => CreateScimError(
                HttpStatusCode.NotFound, 
                null, 
                "Resource not found"),
            
            NotImplementedException => CreateScimError(
                HttpStatusCode.NotImplemented, 
                null, 
                "Operation not supported"),
            
            InvalidOperationException invEx => CreateScimError(
                HttpStatusCode.BadRequest, 
                ScimErrorType.InvalidSyntax, 
                invEx.Message),
            
            _ => CreateScimError(
                HttpStatusCode.InternalServerError, 
                null, 
                "An unexpected error occurred")
        };
    }

    /// <inheritdoc />
    public Task<ScimError> HandleExceptionAsync(Exception exception)
    {
        return Task.FromResult(HandleException(exception));
    }

    /// <summary>
    /// Maps HTTP status codes to SCIM error types.
    /// </summary>
    public static ScimErrorType? GetScimTypeForStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => ScimErrorType.InvalidSyntax,
            HttpStatusCode.Conflict => ScimErrorType.Uniqueness,
            _ => null
        };
    }

    /// <summary>
    /// Creates a 400 Bad Request error.
    /// </summary>
    public static ScimError BadRequest(string detail, ScimErrorType? scimType = ScimErrorType.InvalidSyntax)
    {
        return new ScimError
        {
            Status = "400",
            ScimType = scimType?.ToString()?.ToLowerInvariant(),
            Detail = detail
        };
    }

    /// <summary>
    /// Creates a 401 Unauthorized error.
    /// </summary>
    public static ScimError Unauthorized(string detail = "Authentication required")
    {
        return new ScimError
        {
            Status = "401",
            Detail = detail
        };
    }

    /// <summary>
    /// Creates a 403 Forbidden error.
    /// </summary>
    public static ScimError Forbidden(string detail = "Access denied")
    {
        return new ScimError
        {
            Status = "403",
            Detail = detail
        };
    }

    /// <summary>
    /// Creates a 404 Not Found error.
    /// </summary>
    public static ScimError NotFound(string resourceType, string resourceId)
    {
        return new ScimError
        {
            Status = "404",
            Detail = $"{resourceType} with id '{resourceId}' not found"
        };
    }

    /// <summary>
    /// Creates a 409 Conflict error.
    /// </summary>
    public static ScimError Conflict(string detail, ScimErrorType? scimType = ScimErrorType.Uniqueness)
    {
        return new ScimError
        {
            Status = "409",
            ScimType = scimType?.ToString()?.ToLowerInvariant(),
            Detail = detail
        };
    }

    /// <summary>
    /// Creates a 412 Precondition Failed error.
    /// </summary>
    public static ScimError PreconditionFailed(string detail = "Version mismatch")
    {
        return new ScimError
        {
            Status = "412",
            Detail = detail
        };
    }

    /// <summary>
    /// Creates a 422 Unprocessable Entity error.
    /// </summary>
    public static ScimError UnprocessableEntity(string detail)
    {
        return new ScimError
        {
            Status = "422",
            ScimType = ScimErrorType.InvalidValue.ToString().ToLowerInvariant(),
            Detail = detail
        };
    }

    /// <summary>
    /// Creates a 429 Too Many Requests error.
    /// </summary>
    public static ScimError TooManyRequests(string detail = "Rate limit exceeded")
    {
        return new ScimError
        {
            Status = "429",
            Detail = detail
        };
    }

    /// <summary>
    /// Creates a 500 Internal Server Error.
    /// </summary>
    public static ScimError InternalServerError(string detail = "An unexpected error occurred")
    {
        return new ScimError
        {
            Status = "500",
            Detail = detail
        };
    }

    /// <summary>
    /// Creates a 501 Not Implemented error.
    /// </summary>
    public static ScimError NotImplemented(string detail = "Operation not supported")
    {
        return new ScimError
        {
            Status = "501",
            Detail = detail
        };
    }
}

/// <summary>
/// Base exception for SCIM errors.
/// </summary>
public class ScimException : Exception
{
    /// <summary>
    /// HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// SCIM error type.
    /// </summary>
    public ScimErrorType? ScimType { get; }

    /// <summary>
    /// Creates a new SCIM exception.
    /// </summary>
    public ScimException(HttpStatusCode statusCode, ScimErrorType? scimType, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ScimType = scimType;
    }

    /// <summary>
    /// Creates a new SCIM exception with inner exception.
    /// </summary>
    public ScimException(HttpStatusCode statusCode, ScimErrorType? scimType, string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ScimType = scimType;
    }
}

/// <summary>
/// Exception for resource not found errors.
/// </summary>
public class ResourceNotFoundException : ScimException
{
    public string ResourceType { get; }
    public string ResourceId { get; }

    public ResourceNotFoundException(string resourceType, string resourceId)
        : base(HttpStatusCode.NotFound, null, $"{resourceType} with id '{resourceId}' not found")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

/// <summary>
/// Exception for validation errors.
/// </summary>
public class ValidationException : ScimException
{
    public ValidationException(string message, ScimErrorType scimType = ScimErrorType.InvalidValue)
        : base(HttpStatusCode.BadRequest, scimType, message)
    {
    }
}

/// <summary>
/// Exception for uniqueness violations.
/// </summary>
public class UniquenessException : ScimException
{
    public string AttributeName { get; }
    public string AttributeValue { get; }

    public UniquenessException(string attributeName, string attributeValue)
        : base(HttpStatusCode.Conflict, ScimErrorType.Uniqueness, 
            $"A resource with {attributeName}='{attributeValue}' already exists")
    {
        AttributeName = attributeName;
        AttributeValue = attributeValue;
    }
}

/// <summary>
/// Exception for version mismatch errors.
/// </summary>
public class VersionMismatchException : ScimException
{
    public string ExpectedVersion { get; }
    public string ActualVersion { get; }

    public VersionMismatchException(string expectedVersion, string actualVersion)
        : base(HttpStatusCode.PreconditionFailed, null, 
            $"Version mismatch: expected '{expectedVersion}', actual '{actualVersion}'")
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

/// <summary>
/// Exception for conflict errors (e.g., resource already exists).
/// </summary>
public class ScimConflictException : ScimException
{
    public ScimConflictException(string message)
        : base(HttpStatusCode.Conflict, ScimErrorType.Uniqueness, message)
    {
    }
}

/// <summary>
/// Exception for resource not found errors.
/// </summary>
public class ScimNotFoundException : ScimException
{
    public ScimNotFoundException(string message)
        : base(HttpStatusCode.NotFound, null, message)
    {
    }
}

/// <summary>
/// Exception for invalid filter expressions.
/// </summary>
public class ScimInvalidFilterException : ScimException
{
    public ScimInvalidFilterException(string message)
        : base(HttpStatusCode.BadRequest, ScimErrorType.InvalidFilter, message)
    {
    }
}

/// <summary>
/// Exception for invalid syntax errors.
/// </summary>
public class ScimInvalidSyntaxException : ScimException
{
    public ScimInvalidSyntaxException(string message)
        : base(HttpStatusCode.BadRequest, ScimErrorType.InvalidSyntax, message)
    {
    }
}
