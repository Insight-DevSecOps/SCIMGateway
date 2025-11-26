// ==========================================================================
// T075: AdapterContext - Utilities for Adapters
// ==========================================================================
// Provides utilities to adapters: logging, error handling, schema validation,
// tenant info
// ==========================================================================

using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Configuration;
using SCIMGateway.Core.Errors;
using SCIMGateway.Core.Models;
using SCIMGateway.Core.Validation;

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Interface for adapter context providing common utilities.
/// </summary>
public interface IAdapterContext
{
    /// <summary>
    /// Gets the current tenant ID.
    /// </summary>
    string TenantId { get; }

    /// <summary>
    /// Gets the current provider ID.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Gets the adapter configuration.
    /// </summary>
    AdapterConfiguration Configuration { get; }

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Validates a SCIM user against the schema.
    /// </summary>
    ValidationResult ValidateUser(ScimUser user);

    /// <summary>
    /// Validates a SCIM group against the schema.
    /// </summary>
    ValidationResult ValidateGroup(ScimGroup group);

    /// <summary>
    /// Creates a SCIM error response.
    /// </summary>
    ScimError CreateError(int statusCode, string message, ScimErrorType? scimType = null);

    /// <summary>
    /// Logs an operation with structured data.
    /// </summary>
    void LogOperation(string operation, string resourceType, string? resourceId, bool success, TimeSpan duration);

    /// <summary>
    /// Gets the correlation ID for the current request.
    /// </summary>
    string? CorrelationId { get; }
}

/// <summary>
/// Context providing utilities to adapter implementations.
/// </summary>
public class AdapterContext : IAdapterContext
{
    private readonly ISchemaValidator? _schemaValidator;
    private readonly IErrorHandler? _errorHandler;

    /// <inheritdoc />
    public string TenantId { get; }

    /// <inheritdoc />
    public string ProviderId { get; }

    /// <inheritdoc />
    public AdapterConfiguration Configuration { get; }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterContext"/> class.
    /// </summary>
    public AdapterContext(
        string tenantId,
        string providerId,
        AdapterConfiguration configuration,
        ILogger logger,
        ISchemaValidator? schemaValidator = null,
        IErrorHandler? errorHandler = null)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        ProviderId = providerId ?? throw new ArgumentNullException(nameof(providerId));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaValidator = schemaValidator;
        _errorHandler = errorHandler;
    }

    /// <inheritdoc />
    public ValidationResult ValidateUser(ScimUser user)
    {
        if (_schemaValidator == null)
        {
            return ValidationResult.Success();
        }

        var errors = new List<ValidationError>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            errors.Add(new ValidationError
            {
                Path = "userName",
                Message = "userName is required",
                ErrorType = ValidationErrorType.RequiredAttributeMissing
            });
        }

        // Validate email format if present
        if (user.Emails?.Any() == true)
        {
            foreach (var email in user.Emails.Where(e => !string.IsNullOrEmpty(e.Value)))
            {
                if (!IsValidEmail(email.Value!))
                {
                    errors.Add(new ValidationError
                    {
                        Path = "emails.value",
                        Message = $"Invalid email format: {email.Value}",
                        ErrorType = ValidationErrorType.InvalidFormat
                    });
                }
            }
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    /// <inheritdoc />
    public ValidationResult ValidateGroup(ScimGroup group)
    {
        if (_schemaValidator == null)
        {
            return ValidationResult.Success();
        }

        var errors = new List<ValidationError>();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(group.DisplayName))
        {
            errors.Add(new ValidationError
            {
                Path = "displayName",
                Message = "displayName is required",
                ErrorType = ValidationErrorType.RequiredAttributeMissing
            });
        }

        // Validate members if present
        if (group.Members?.Any() == true)
        {
            foreach (var member in group.Members)
            {
                if (string.IsNullOrEmpty(member.Value))
                {
                    errors.Add(new ValidationError
                    {
                        Path = "members.value",
                        Message = "Member value is required",
                        ErrorType = ValidationErrorType.RequiredAttributeMissing
                    });
                }
            }
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    /// <inheritdoc />
    public ScimError CreateError(int statusCode, string message, ScimErrorType? scimType = null)
    {
        if (_errorHandler != null)
        {
            return _errorHandler.CreateScimError(
                (System.Net.HttpStatusCode)statusCode,
                scimType,
                message);
        }

        return new ScimError
        {
            Status = statusCode.ToString(),
            ScimType = scimType?.ToString()?.ToLowerInvariant(),
            Detail = message
        };
    }

    /// <inheritdoc />
    public void LogOperation(string operation, string resourceType, string? resourceId, bool success, TimeSpan duration)
    {
        if (success)
        {
            Logger.LogInformation(
                "Adapter operation: {Operation} {ResourceType} {ResourceId} completed in {Duration}ms - Tenant: {TenantId}, Provider: {ProviderId}, CorrelationId: {CorrelationId}",
                operation,
                resourceType,
                resourceId ?? "N/A",
                duration.TotalMilliseconds,
                TenantId,
                ProviderId,
                CorrelationId ?? "N/A");
        }
        else
        {
            Logger.LogWarning(
                "Adapter operation failed: {Operation} {ResourceType} {ResourceId} after {Duration}ms - Tenant: {TenantId}, Provider: {ProviderId}, CorrelationId: {CorrelationId}",
                operation,
                resourceType,
                resourceId ?? "N/A",
                duration.TotalMilliseconds,
                TenantId,
                ProviderId,
                CorrelationId ?? "N/A");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Factory for creating adapter contexts.
/// </summary>
public interface IAdapterContextFactory
{
    /// <summary>
    /// Creates an adapter context for the given tenant and provider.
    /// </summary>
    IAdapterContext Create(string tenantId, string providerId, AdapterConfiguration configuration);
}

/// <summary>
/// Default implementation of adapter context factory.
/// </summary>
public class AdapterContextFactory : IAdapterContextFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISchemaValidator? _schemaValidator;
    private readonly IErrorHandler? _errorHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdapterContextFactory"/> class.
    /// </summary>
    public AdapterContextFactory(
        ILoggerFactory loggerFactory,
        ISchemaValidator? schemaValidator = null,
        IErrorHandler? errorHandler = null)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _schemaValidator = schemaValidator;
        _errorHandler = errorHandler;
    }

    /// <inheritdoc />
    public IAdapterContext Create(string tenantId, string providerId, AdapterConfiguration configuration)
    {
        var logger = _loggerFactory.CreateLogger($"AdapterContext.{providerId}");
        return new AdapterContext(tenantId, providerId, configuration, logger, _schemaValidator, _errorHandler);
    }
}
