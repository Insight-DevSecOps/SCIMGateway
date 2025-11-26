// ==========================================================================
// T046: EnterpriseUserExtension - RFC 7643 Enterprise User Extension Support
// ==========================================================================
// Helper methods for managing the Enterprise User Extension schema
// urn:ietf:params:scim:schemas:extension:enterprise:2.0:User
// ==========================================================================

using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Extensions;

/// <summary>
/// Helper class for managing Enterprise User Extension per RFC 7643 Section 4.3.
/// </summary>
public static class EnterpriseUserExtensionHelper
{
    /// <summary>
    /// Enterprise User Extension schema URN.
    /// </summary>
    public const string SchemaUrn = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";

    /// <summary>
    /// Ensures the Enterprise User schema is added to the user's schemas list
    /// if the extension has any values.
    /// </summary>
    /// <param name="user">The SCIM user to update.</param>
    public static void EnsureSchemaIfPresent(ScimUser user)
    {
        if (user == null) return;

        if (HasEnterpriseData(user) && !user.Schemas.Contains(SchemaUrn))
        {
            user.Schemas.Add(SchemaUrn);
        }
        else if (!HasEnterpriseData(user) && user.Schemas.Contains(SchemaUrn))
        {
            // Remove schema if no enterprise data
            user.Schemas.Remove(SchemaUrn);
        }
    }

    /// <summary>
    /// Checks if the user has any Enterprise User Extension data.
    /// </summary>
    /// <param name="user">The SCIM user to check.</param>
    /// <returns>True if the user has enterprise data.</returns>
    public static bool HasEnterpriseData(ScimUser user)
    {
        if (user?.EnterpriseUser == null) return false;

        var ext = user.EnterpriseUser;
        return !string.IsNullOrEmpty(ext.EmployeeNumber) ||
               !string.IsNullOrEmpty(ext.CostCenter) ||
               !string.IsNullOrEmpty(ext.Organization) ||
               !string.IsNullOrEmpty(ext.Division) ||
               !string.IsNullOrEmpty(ext.Department) ||
               ext.Manager != null;
    }

    /// <summary>
    /// Creates a new Enterprise User Extension with the specified values.
    /// </summary>
    /// <param name="employeeNumber">Employee number.</param>
    /// <param name="costCenter">Cost center.</param>
    /// <param name="organization">Organization name.</param>
    /// <param name="division">Division name.</param>
    /// <param name="department">Department name.</param>
    /// <param name="managerId">Manager's user ID.</param>
    /// <param name="managerDisplayName">Manager's display name.</param>
    /// <returns>A new EnterpriseUserExtension instance.</returns>
    public static EnterpriseUserExtension Create(
        string? employeeNumber = null,
        string? costCenter = null,
        string? organization = null,
        string? division = null,
        string? department = null,
        string? managerId = null,
        string? managerDisplayName = null)
    {
        var extension = new EnterpriseUserExtension
        {
            EmployeeNumber = employeeNumber,
            CostCenter = costCenter,
            Organization = organization,
            Division = division,
            Department = department
        };

        if (!string.IsNullOrEmpty(managerId))
        {
            extension.Manager = new ScimManager
            {
                Value = managerId,
                DisplayName = managerDisplayName
            };
        }

        return extension;
    }

    /// <summary>
    /// Sets the manager reference on the enterprise extension.
    /// </summary>
    /// <param name="extension">The extension to update.</param>
    /// <param name="managerId">The manager's user ID.</param>
    /// <param name="managerDisplayName">The manager's display name.</param>
    /// <param name="managerRef">The $ref URI to the manager resource.</param>
    public static void SetManager(
        EnterpriseUserExtension extension,
        string managerId,
        string? managerDisplayName = null,
        string? managerRef = null)
    {
        if (extension == null) return;

        extension.Manager = new ScimManager
        {
            Value = managerId,
            DisplayName = managerDisplayName,
            Ref = managerRef
        };
    }

    /// <summary>
    /// Clears the manager reference from the enterprise extension.
    /// </summary>
    /// <param name="extension">The extension to update.</param>
    public static void ClearManager(EnterpriseUserExtension extension)
    {
        if (extension != null)
        {
            extension.Manager = null;
        }
    }

    /// <summary>
    /// Validates the Enterprise User Extension.
    /// </summary>
    /// <param name="extension">The extension to validate.</param>
    /// <returns>List of validation errors, empty if valid.</returns>
    public static IEnumerable<string> Validate(EnterpriseUserExtension? extension)
    {
        if (extension == null)
        {
            yield break;
        }

        // Validate employee number format (if present, should be alphanumeric)
        if (!string.IsNullOrEmpty(extension.EmployeeNumber))
        {
            if (extension.EmployeeNumber.Length > 256)
            {
                yield return "employeeNumber must not exceed 256 characters";
            }
        }

        // Validate cost center (if present)
        if (!string.IsNullOrEmpty(extension.CostCenter))
        {
            if (extension.CostCenter.Length > 256)
            {
                yield return "costCenter must not exceed 256 characters";
            }
        }

        // Validate organization (if present)
        if (!string.IsNullOrEmpty(extension.Organization))
        {
            if (extension.Organization.Length > 256)
            {
                yield return "organization must not exceed 256 characters";
            }
        }

        // Validate division (if present)
        if (!string.IsNullOrEmpty(extension.Division))
        {
            if (extension.Division.Length > 256)
            {
                yield return "division must not exceed 256 characters";
            }
        }

        // Validate department (if present)
        if (!string.IsNullOrEmpty(extension.Department))
        {
            if (extension.Department.Length > 256)
            {
                yield return "department must not exceed 256 characters";
            }
        }

        // Validate manager reference
        if (extension.Manager != null)
        {
            if (string.IsNullOrEmpty(extension.Manager.Value))
            {
                yield return "manager.value is required when manager is specified";
            }
        }
    }

    /// <summary>
    /// Merges enterprise extension data for PATCH operations.
    /// </summary>
    /// <param name="existing">The existing extension.</param>
    /// <param name="patch">The patch data.</param>
    /// <returns>The merged extension.</returns>
    public static EnterpriseUserExtension Merge(
        EnterpriseUserExtension? existing,
        EnterpriseUserExtension? patch)
    {
        if (patch == null) return existing ?? new EnterpriseUserExtension();
        if (existing == null) return patch;

        return new EnterpriseUserExtension
        {
            EmployeeNumber = patch.EmployeeNumber ?? existing.EmployeeNumber,
            CostCenter = patch.CostCenter ?? existing.CostCenter,
            Organization = patch.Organization ?? existing.Organization,
            Division = patch.Division ?? existing.Division,
            Department = patch.Department ?? existing.Department,
            Manager = patch.Manager ?? existing.Manager
        };
    }

    /// <summary>
    /// Copies enterprise extension data from one user to another.
    /// </summary>
    /// <param name="source">Source user.</param>
    /// <param name="target">Target user.</param>
    public static void CopyTo(ScimUser source, ScimUser target)
    {
        if (source?.EnterpriseUser == null)
        {
            target.EnterpriseUser = null;
            return;
        }

        target.EnterpriseUser = new EnterpriseUserExtension
        {
            EmployeeNumber = source.EnterpriseUser.EmployeeNumber,
            CostCenter = source.EnterpriseUser.CostCenter,
            Organization = source.EnterpriseUser.Organization,
            Division = source.EnterpriseUser.Division,
            Department = source.EnterpriseUser.Department,
            Manager = source.EnterpriseUser.Manager != null ? new ScimManager
            {
                Value = source.EnterpriseUser.Manager.Value,
                DisplayName = source.EnterpriseUser.Manager.DisplayName,
                Ref = source.EnterpriseUser.Manager.Ref
            } : null
        };

        EnsureSchemaIfPresent(target);
    }
}
