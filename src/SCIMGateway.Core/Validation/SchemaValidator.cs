// ==========================================================================
// T023: SchemaValidator - SCIM Schema Validation per RFC 7643
// ==========================================================================
// Validates SCIM resources against RFC 7643 schema requirements
// ==========================================================================

using System.Text.RegularExpressions;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Validation;

/// <summary>
/// Interface for SCIM schema validation.
/// </summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates a SCIM user asynchronously.
    /// </summary>
    Task<ValidationResult> ValidateUserAsync(ScimUser user);

    /// <summary>
    /// Validates a SCIM user.
    /// </summary>
    ValidationResult ValidateUser(ScimUser user);

    /// <summary>
    /// Validates a SCIM group asynchronously.
    /// </summary>
    Task<ValidationResult> ValidateGroupAsync(ScimGroup group);

    /// <summary>
    /// Validates a SCIM group.
    /// </summary>
    ValidationResult ValidateGroup(ScimGroup group);

    /// <summary>
    /// Validates a PATCH request asynchronously.
    /// </summary>
    Task<ValidationResult> ValidatePatchRequestAsync(IEnumerable<PatchOperation> operations);

    /// <summary>
    /// Validates a PATCH request.
    /// </summary>
    ValidationResult ValidatePatchRequest(IEnumerable<PatchOperation> operations);

    /// <summary>
    /// Validates PATCH operations.
    /// </summary>
    ValidationResult ValidatePatchOperations(IEnumerable<PatchOperation> operations);

    /// <summary>
    /// Validates a SCIM filter expression asynchronously.
    /// </summary>
    Task<ValidationResult> ValidateFilterAsync(string filter);

    /// <summary>
    /// Validates a SCIM filter expression.
    /// </summary>
    ValidationResult ValidateFilter(string filter);
}

/// <summary>
/// Result of validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Collection of validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; set; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with an error.
    /// </summary>
    public static ValidationResult Failure(string path, string message, ValidationErrorType errorType = ValidationErrorType.InvalidValue)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = [new ValidationError { Path = path, Message = message, ErrorType = errorType }]
        };
    }

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
}

/// <summary>
/// A single validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// JSON path to the invalid attribute.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Type of validation error.
    /// </summary>
    public ValidationErrorType ErrorType { get; set; }
}

/// <summary>
/// Types of validation errors.
/// </summary>
public enum ValidationErrorType
{
    /// <summary>Required attribute is missing.</summary>
    RequiredAttributeMissing,
    
    /// <summary>Attribute value is invalid.</summary>
    InvalidValue,
    
    /// <summary>Attribute type mismatch.</summary>
    TypeMismatch,
    
    /// <summary>Invalid format (e.g., email format).</summary>
    InvalidFormat,
    
    /// <summary>Schema URI is invalid.</summary>
    InvalidSchema,
    
    /// <summary>Uniqueness constraint violated.</summary>
    UniquenessViolation,
    
    /// <summary>Mutability constraint violated.</summary>
    MutabilityViolation,
    
    /// <summary>Invalid filter expression.</summary>
    InvalidFilter,
    
    /// <summary>Invalid PATCH operation.</summary>
    InvalidPatchOperation
}

/// <summary>
/// Constants for SCIM schema URIs.
/// </summary>
public static class ScimSchemaUris
{
    /// <summary>SCIM User schema URI.</summary>
    public const string User = "urn:ietf:params:scim:schemas:core:2.0:User";

    /// <summary>SCIM Group schema URI.</summary>
    public const string Group = "urn:ietf:params:scim:schemas:core:2.0:Group";

    /// <summary>SCIM Enterprise User extension schema URI.</summary>
    public const string EnterpriseUser = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

    /// <summary>SCIM ListResponse schema URI.</summary>
    public const string ListResponse = "urn:ietf:params:scim:api:messages:2.0:ListResponse";

    /// <summary>SCIM Error schema URI.</summary>
    public const string Error = "urn:ietf:params:scim:api:messages:2.0:Error";

    /// <summary>SCIM PatchOp schema URI.</summary>
    public const string PatchOp = "urn:ietf:params:scim:api:messages:2.0:PatchOp";

    /// <summary>SCIM ServiceProviderConfig schema URI.</summary>
    public const string ServiceProviderConfig = "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig";

    /// <summary>SCIM ResourceType schema URI.</summary>
    public const string ResourceType = "urn:ietf:params:scim:schemas:core:2.0:ResourceType";

    /// <summary>SCIM Schema schema URI.</summary>
    public const string Schema = "urn:ietf:params:scim:schemas:core:2.0:Schema";
}

/// <summary>
/// PATCH operation model per RFC 7644.
/// </summary>
public class PatchOperation
{
    /// <summary>
    /// Operation type: add, remove, replace.
    /// </summary>
    public string Op { get; set; } = string.Empty;

    /// <summary>
    /// Alias for Op.
    /// </summary>
    public string Operation => Op;

    /// <summary>
    /// Target path for the operation.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Value for add/replace operations.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// SCIM schema validator implementation.
/// </summary>
public partial class SchemaValidator : ISchemaValidator
{
    // RFC 5322 email pattern (simplified)
    private static readonly Regex EmailRegex = EmailPattern();
    
    // Phone number pattern (E.164 and common formats)
    private static readonly Regex PhoneRegex = PhonePattern();

    /// <inheritdoc />
    public Task<ValidationResult> ValidateUserAsync(ScimUser user)
    {
        return Task.FromResult(ValidateUser(user));
    }

    /// <inheritdoc />
    public ValidationResult ValidateUser(ScimUser user)
    {
        var errors = new List<ValidationError>();

        // T048: userName is required per RFC 7643
        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            errors.Add(new ValidationError
            {
                Path = "userName",
                Message = "userName is required",
                ErrorType = ValidationErrorType.RequiredAttributeMissing
            });
        }
        else
        {
            // T048: Validate userName format (alphanumeric, email-like, or simple string)
            if (user.UserName.Length > 256)
            {
                errors.Add(new ValidationError
                {
                    Path = "userName",
                    Message = "userName must not exceed 256 characters",
                    ErrorType = ValidationErrorType.InvalidValue
                });
            }
        }

        // T048: Validate displayName if present
        if (!string.IsNullOrEmpty(user.DisplayName) && user.DisplayName.Length > 256)
        {
            errors.Add(new ValidationError
            {
                Path = "displayName",
                Message = "displayName must not exceed 256 characters",
                ErrorType = ValidationErrorType.InvalidValue
            });
        }

        // Validate schemas if present
        if (user.Schemas != null && user.Schemas.Count > 0)
        {
            if (!user.Schemas.Contains(ScimSchemaUris.User))
            {
                errors.Add(new ValidationError
                {
                    Path = "schemas",
                    Message = $"schemas must include {ScimSchemaUris.User}",
                    ErrorType = ValidationErrorType.InvalidSchema
                });
            }
        }

        // T048: Validate emails if present (RFC 5322 format)
        if (user.Emails != null)
        {
            var primaryCount = 0;
            for (int i = 0; i < user.Emails.Count; i++)
            {
                var email = user.Emails[i];
                if (!string.IsNullOrEmpty(email.Value) && !IsValidEmail(email.Value))
                {
                    errors.Add(new ValidationError
                    {
                        Path = $"emails[{i}].value",
                        Message = "Invalid email format per RFC 5322",
                        ErrorType = ValidationErrorType.InvalidFormat
                    });
                }
                if (email.Primary)
                {
                    primaryCount++;
                }
            }
            // Only one primary email allowed
            if (primaryCount > 1)
            {
                errors.Add(new ValidationError
                {
                    Path = "emails",
                    Message = "Only one email can be marked as primary",
                    ErrorType = ValidationErrorType.InvalidValue
                });
            }
        }

        // T048: Validate phone numbers if present
        if (user.PhoneNumbers != null)
        {
            var primaryCount = 0;
            for (int i = 0; i < user.PhoneNumbers.Count; i++)
            {
                var phone = user.PhoneNumbers[i];
                if (string.IsNullOrWhiteSpace(phone.Value))
                {
                    errors.Add(new ValidationError
                    {
                        Path = $"phoneNumbers[{i}].value",
                        Message = "Phone number value cannot be empty",
                        ErrorType = ValidationErrorType.InvalidValue
                    });
                }
                else if (!IsValidPhoneNumber(phone.Value))
                {
                    errors.Add(new ValidationError
                    {
                        Path = $"phoneNumbers[{i}].value",
                        Message = "Invalid phone number format",
                        ErrorType = ValidationErrorType.InvalidFormat
                    });
                }
                if (phone.Primary)
                {
                    primaryCount++;
                }
            }
            if (primaryCount > 1)
            {
                errors.Add(new ValidationError
                {
                    Path = "phoneNumbers",
                    Message = "Only one phone number can be marked as primary",
                    ErrorType = ValidationErrorType.InvalidValue
                });
            }
        }

        // T048: Validate addresses if present
        if (user.Addresses != null)
        {
            var primaryCount = 0;
            for (int i = 0; i < user.Addresses.Count; i++)
            {
                var address = user.Addresses[i];
                if (address.Primary)
                {
                    primaryCount++;
                }
                // Validate type if present
                if (!string.IsNullOrEmpty(address.Type) && 
                    !IsValidAddressType(address.Type))
                {
                    errors.Add(new ValidationError
                    {
                        Path = $"addresses[{i}].type",
                        Message = "Invalid address type. Valid types are: work, home, other",
                        ErrorType = ValidationErrorType.InvalidValue
                    });
                }
            }
            if (primaryCount > 1)
            {
                errors.Add(new ValidationError
                {
                    Path = "addresses",
                    Message = "Only one address can be marked as primary",
                    ErrorType = ValidationErrorType.InvalidValue
                });
            }
        }

        // T048: Validate name components if present
        if (user.Name != null)
        {
            if (!string.IsNullOrEmpty(user.Name.FamilyName) && user.Name.FamilyName.Length > 256)
            {
                errors.Add(new ValidationError
                {
                    Path = "name.familyName",
                    Message = "familyName must not exceed 256 characters",
                    ErrorType = ValidationErrorType.InvalidValue
                });
            }
            if (!string.IsNullOrEmpty(user.Name.GivenName) && user.Name.GivenName.Length > 256)
            {
                errors.Add(new ValidationError
                {
                    Path = "name.givenName",
                    Message = "givenName must not exceed 256 characters",
                    ErrorType = ValidationErrorType.InvalidValue
                });
            }
        }

        // T048: Validate Enterprise User Extension if present
        if (user.EnterpriseUser != null)
        {
            var extErrors = ValidateEnterpriseExtension(user.EnterpriseUser);
            errors.AddRange(extErrors);
        }

        // T048: Validate active is boolean (always true in C#, but check for null issues)
        // active defaults to true if not specified, which is valid

        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }

    /// <summary>
    /// Validates the Enterprise User Extension.
    /// </summary>
    private static IEnumerable<ValidationError> ValidateEnterpriseExtension(EnterpriseUserExtension ext)
    {
        var basePath = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

        if (!string.IsNullOrEmpty(ext.EmployeeNumber) && ext.EmployeeNumber.Length > 256)
        {
            yield return new ValidationError
            {
                Path = $"{basePath}.employeeNumber",
                Message = "employeeNumber must not exceed 256 characters",
                ErrorType = ValidationErrorType.InvalidValue
            };
        }

        if (!string.IsNullOrEmpty(ext.CostCenter) && ext.CostCenter.Length > 256)
        {
            yield return new ValidationError
            {
                Path = $"{basePath}.costCenter",
                Message = "costCenter must not exceed 256 characters",
                ErrorType = ValidationErrorType.InvalidValue
            };
        }

        if (!string.IsNullOrEmpty(ext.Organization) && ext.Organization.Length > 256)
        {
            yield return new ValidationError
            {
                Path = $"{basePath}.organization",
                Message = "organization must not exceed 256 characters",
                ErrorType = ValidationErrorType.InvalidValue
            };
        }

        if (!string.IsNullOrEmpty(ext.Division) && ext.Division.Length > 256)
        {
            yield return new ValidationError
            {
                Path = $"{basePath}.division",
                Message = "division must not exceed 256 characters",
                ErrorType = ValidationErrorType.InvalidValue
            };
        }

        if (!string.IsNullOrEmpty(ext.Department) && ext.Department.Length > 256)
        {
            yield return new ValidationError
            {
                Path = $"{basePath}.department",
                Message = "department must not exceed 256 characters",
                ErrorType = ValidationErrorType.InvalidValue
            };
        }

        if (ext.Manager != null && string.IsNullOrWhiteSpace(ext.Manager.Value))
        {
            yield return new ValidationError
            {
                Path = $"{basePath}.manager.value",
                Message = "manager.value is required when manager is specified",
                ErrorType = ValidationErrorType.RequiredAttributeMissing
            };
        }
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateGroupAsync(ScimGroup group)
    {
        return Task.FromResult(ValidateGroup(group));
    }

    /// <inheritdoc />
    public ValidationResult ValidateGroup(ScimGroup group)
    {
        var errors = new List<ValidationError>();

        // T049: displayName is required for groups per RFC 7643
        if (string.IsNullOrWhiteSpace(group.DisplayName))
        {
            errors.Add(new ValidationError
            {
                Path = "displayName",
                Message = "displayName is required",
                ErrorType = ValidationErrorType.RequiredAttributeMissing
            });
        }
        else
        {
            // T049: Validate displayName length
            if (group.DisplayName.Length > 256)
            {
                errors.Add(new ValidationError
                {
                    Path = "displayName",
                    Message = "displayName must not exceed 256 characters",
                    ErrorType = ValidationErrorType.InvalidValue
                });
            }
        }

        // Validate schemas if present
        if (group.Schemas != null && group.Schemas.Count > 0)
        {
            if (!group.Schemas.Contains(ScimSchemaUris.Group))
            {
                errors.Add(new ValidationError
                {
                    Path = "schemas",
                    Message = $"schemas must include {ScimSchemaUris.Group}",
                    ErrorType = ValidationErrorType.InvalidSchema
                });
            }
        }

        // T049: Validate members if present
        if (group.Members != null)
        {
            var memberValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            for (int i = 0; i < group.Members.Count; i++)
            {
                var member = group.Members[i];
                
                // T049: value is required for each member
                if (string.IsNullOrWhiteSpace(member.Value))
                {
                    errors.Add(new ValidationError
                    {
                        Path = $"members[{i}].value",
                        Message = "Member value (ID) is required",
                        ErrorType = ValidationErrorType.RequiredAttributeMissing
                    });
                }
                else
                {
                    // Check for duplicate members
                    if (!memberValues.Add(member.Value))
                    {
                        errors.Add(new ValidationError
                        {
                            Path = $"members[{i}].value",
                            Message = $"Duplicate member with value '{member.Value}'",
                            ErrorType = ValidationErrorType.InvalidValue
                        });
                    }
                }

                // T049: Validate member type if present
                if (!string.IsNullOrEmpty(member.Type) && 
                    !IsValidMemberType(member.Type))
                {
                    errors.Add(new ValidationError
                    {
                        Path = $"members[{i}].type",
                        Message = "Invalid member type. Valid types are: User, Group",
                        ErrorType = ValidationErrorType.InvalidValue
                    });
                }

                // T049: Validate display if present (should not exceed 256 chars)
                if (!string.IsNullOrEmpty(member.Display) && member.Display.Length > 256)
                {
                    errors.Add(new ValidationError
                    {
                        Path = $"members[{i}].display",
                        Message = "Member display must not exceed 256 characters",
                        ErrorType = ValidationErrorType.InvalidValue
                    });
                }
            }
        }

        // T049: Validate externalId if present
        if (!string.IsNullOrEmpty(group.ExternalId) && group.ExternalId.Length > 256)
        {
            errors.Add(new ValidationError
            {
                Path = "externalId",
                Message = "externalId must not exceed 256 characters",
                ErrorType = ValidationErrorType.InvalidValue
            });
        }

        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidatePatchRequestAsync(IEnumerable<PatchOperation> operations)
    {
        return Task.FromResult(ValidatePatchRequest(operations));
    }

    /// <inheritdoc />
    public ValidationResult ValidatePatchRequest(IEnumerable<PatchOperation> operations)
    {
        return ValidatePatchOperations(operations);
    }

    /// <inheritdoc />
    public ValidationResult ValidatePatchOperations(IEnumerable<PatchOperation> operations)
    {
        var errors = new List<ValidationError>();
        var operationList = operations.ToList();

        if (operationList.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Path = "Operations",
                Message = "At least one operation is required",
                ErrorType = ValidationErrorType.InvalidPatchOperation
            });
            return ValidationResult.Failure(errors);
        }

        for (int i = 0; i < operationList.Count; i++)
        {
            var op = operationList[i];

            // op is required
            if (string.IsNullOrWhiteSpace(op.Op))
            {
                errors.Add(new ValidationError
                {
                    Path = $"Operations[{i}].op",
                    Message = "op is required",
                    ErrorType = ValidationErrorType.RequiredAttributeMissing
                });
                continue;
            }

            // op must be one of: add, remove, replace
            var normalizedOp = op.Op.ToLowerInvariant();
            if (normalizedOp != "add" && normalizedOp != "remove" && normalizedOp != "replace")
            {
                errors.Add(new ValidationError
                {
                    Path = $"Operations[{i}].op",
                    Message = "op must be 'add', 'remove', or 'replace'",
                    ErrorType = ValidationErrorType.InvalidValue
                });
                continue;
            }

            // add and replace require value
            if (normalizedOp == "add" || normalizedOp == "replace")
            {
                if (op.Value == null)
                {
                    errors.Add(new ValidationError
                    {
                        Path = $"Operations[{i}].value",
                        Message = $"value is required for '{op.Op}' operation",
                        ErrorType = ValidationErrorType.RequiredAttributeMissing
                    });
                }
            }

            // remove requires path (except when removing all)
            // Note: This is optional per RFC 7644 - path can be omitted for remove
        }

        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateFilterAsync(string filter)
    {
        return Task.FromResult(ValidateFilter(filter));
    }

    /// <inheritdoc />
    public ValidationResult ValidateFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return ValidationResult.Success(); // Empty filter is valid (return all)
        }

        // Basic filter validation
        // Full RFC 7644 filter parsing is done in FilterParser
        // Here we just do basic syntax checks

        var errors = new List<ValidationError>();

        // Check for balanced parentheses
        int parenCount = 0;
        foreach (char c in filter)
        {
            if (c == '(') parenCount++;
            else if (c == ')') parenCount--;

            if (parenCount < 0)
            {
                errors.Add(new ValidationError
                {
                    Path = "filter",
                    Message = "Unbalanced parentheses in filter expression",
                    ErrorType = ValidationErrorType.InvalidFilter
                });
                break;
            }
        }

        if (parenCount != 0 && errors.Count == 0)
        {
            errors.Add(new ValidationError
            {
                Path = "filter",
                Message = "Unbalanced parentheses in filter expression",
                ErrorType = ValidationErrorType.InvalidFilter
            });
        }

        // Check for valid operators
        var validOperators = new[] { "eq", "ne", "co", "sw", "ew", "gt", "ge", "lt", "le", "pr", "and", "or", "not" };
        var words = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in words)
        {
            // Skip quoted strings
            if (word.StartsWith('"') || word.EndsWith('"'))
                continue;

            // Skip attribute paths
            if (word.Contains('.') || word.Contains('['))
                continue;

            // Check if lowercase version is a known operator or attribute name
            var lowerWord = word.ToLowerInvariant().Trim('(', ')');
            if (lowerWord.Length > 0 && 
                !validOperators.Contains(lowerWord) &&
                !IsValidAttributeName(lowerWord))
            {
                // This could be a value, which is fine
                // Only report error if it looks like a malformed operator
                if (lowerWord.Length == 2 && !lowerWord.All(char.IsLetter))
                {
                    errors.Add(new ValidationError
                    {
                        Path = "filter",
                        Message = $"Invalid operator or attribute: {word}",
                        ErrorType = ValidationErrorType.InvalidFilter
                    });
                }
            }
        }

        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }

    private static bool IsValidEmail(string email)
    {
        return EmailRegex.IsMatch(email);
    }

    private static bool IsValidPhoneNumber(string phone)
    {
        // Accept various phone formats: +1234567890, (123) 456-7890, 123-456-7890, etc.
        return PhoneRegex.IsMatch(phone);
    }

    private static bool IsValidAttributeName(string name)
    {
        // SCIM attribute names are alphanumeric and can contain dots
        return !string.IsNullOrEmpty(name) && 
               name.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_');
    }

    private static bool IsValidAddressType(string type)
    {
        // Valid address types per RFC 7643
        return type.Equals("work", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("home", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("other", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidMemberType(string type)
    {
        // Valid member types for groups
        return type.Equals("User", StringComparison.OrdinalIgnoreCase) ||
               type.Equals("Group", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[\+]?[(]?[0-9]{1,4}[)]?[-\s\./0-9]*$", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();
}
