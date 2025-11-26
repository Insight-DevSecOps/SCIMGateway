// ==========================================================================
// T072: AdapterException with SCIM Error Type Mapping
// ==========================================================================
// Adapter-specific exception that maps to SCIM error types per RFC 7644
// ==========================================================================

using SCIMGateway.Core.Errors;

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Exception thrown by adapters when an error occurs during provider operations.
/// Maps provider-specific errors to SCIM error types for consistent error handling.
/// </summary>
public class AdapterException : Exception
{
    /// <summary>
    /// Gets the name of the provider that threw the exception.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Gets the HTTP status code returned by the provider, if available.
    /// </summary>
    public int? HttpStatusCode { get; init; }

    /// <summary>
    /// Gets the provider-specific error code, if available.
    /// </summary>
    public string? ProviderErrorCode { get; init; }

    /// <summary>
    /// Gets the SCIM error type that this exception maps to.
    /// </summary>
    public ScimErrorType ScimErrorType { get; init; } = ScimErrorType.InternalError;

    /// <summary>
    /// Gets a value indicating whether the operation can be retried.
    /// </summary>
    public bool IsRetryable { get; init; }

    /// <summary>
    /// Gets the number of seconds to wait before retrying, if applicable.
    /// </summary>
    public int? RetryAfterSeconds { get; init; }

    /// <summary>
    /// Gets the adapter ID associated with this exception.
    /// </summary>
    public string? AdapterId { get; init; }

    /// <summary>
    /// Gets the operation that was being performed when the exception occurred.
    /// </summary>
    public string? Operation { get; init; }

    /// <summary>
    /// Gets the resource ID that was being operated on, if available.
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets the resource type (User, Group, etc.) being operated on.
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    /// Gets additional details about the error from the provider.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="providerName">The name of the provider.</param>
    public AdapterException(string message, string providerName)
        : base(message)
    {
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="innerException">The inner exception.</param>
    public AdapterException(string message, string providerName, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="providerName">The name of the provider.</param>
    /// <param name="scimErrorType">The SCIM error type.</param>
    /// <param name="httpStatusCode">The HTTP status code from the provider.</param>
    public AdapterException(string message, string providerName, ScimErrorType scimErrorType, int? httpStatusCode = null)
        : base(message)
    {
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        ScimErrorType = scimErrorType;
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Gets the recommended HTTP status code for the SCIM response based on the error type.
    /// </summary>
    /// <returns>The recommended HTTP status code.</returns>
    public int GetRecommendedHttpStatusCode()
    {
        return ScimErrorType switch
        {
            ScimErrorType.InvalidSyntax => 400,
            ScimErrorType.Uniqueness => 409,
            ScimErrorType.Mutability => 400,
            ScimErrorType.InvalidFilter => 400,
            ScimErrorType.NoTarget => 400,
            ScimErrorType.TooMany => 400,
            ScimErrorType.ServerUnavailable => 503,
            ScimErrorType.ResourceNotFound => 404,
            ScimErrorType.Unauthorized => 401,
            ScimErrorType.Forbidden => 403,
            ScimErrorType.RateLimitExceeded => 429,
            ScimErrorType.Timeout => 408,
            ScimErrorType.InternalError => 500,
            ScimErrorType.Unknown => 500,
            _ => 500
        };
    }

    /// <summary>
    /// Gets the SCIM error type string for the error response.
    /// </summary>
    /// <returns>The SCIM type string or null if not applicable.</returns>
    public string? GetScimTypeString()
    {
        return ScimErrorType switch
        {
            ScimErrorType.InvalidSyntax => "invalidSyntax",
            ScimErrorType.Uniqueness => "uniqueness",
            ScimErrorType.Mutability => "mutability",
            ScimErrorType.InvalidFilter => "invalidFilter",
            ScimErrorType.NoTarget => "noTarget",
            ScimErrorType.TooMany => "tooMany",
            // These types don't have specific scimType values in the spec
            ScimErrorType.ServerUnavailable => null,
            ScimErrorType.ResourceNotFound => null,
            ScimErrorType.Unauthorized => null,
            ScimErrorType.Forbidden => null,
            ScimErrorType.RateLimitExceeded => null,
            ScimErrorType.Timeout => null,
            ScimErrorType.InternalError => null,
            ScimErrorType.Unknown => null,
            _ => null
        };
    }

    /// <summary>
    /// Creates an AdapterException from an HTTP status code.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="message">Optional error message.</param>
    /// <returns>An AdapterException with the appropriate SCIM error type.</returns>
    public static AdapterException FromHttpStatusCode(int statusCode, string providerName, string? message = null)
    {
        var scimType = statusCode switch
        {
            400 => ScimErrorType.InvalidSyntax,
            401 => ScimErrorType.Unauthorized,
            403 => ScimErrorType.Forbidden,
            404 => ScimErrorType.ResourceNotFound,
            408 => ScimErrorType.Timeout,
            409 => ScimErrorType.Uniqueness,
            429 => ScimErrorType.RateLimitExceeded,
            503 => ScimErrorType.ServerUnavailable,
            _ => statusCode >= 500 ? ScimErrorType.InternalError : ScimErrorType.Unknown
        };

        var defaultMessage = message ?? $"Provider returned HTTP {statusCode}";

        return new AdapterException(defaultMessage, providerName, scimType, statusCode)
        {
            IsRetryable = statusCode == 429 || statusCode == 503 || statusCode == 408
        };
    }
}
