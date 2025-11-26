// ==========================================================================
// T034-T038: IUserRepository - User Data Access Interface
// ==========================================================================
// Repository interface for SCIM User CRUD operations
// ==========================================================================

using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Repositories;

/// <summary>
/// Repository interface for SCIM User operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user with assigned ID.</returns>
    Task<ScimUser> CreateAsync(ScimUser user, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<ScimUser?> GetByIdAsync(string id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by userName.
    /// </summary>
    /// <param name="userName">The userName.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<ScimUser?> GetByUserNameAsync(string userName, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by externalId.
    /// </summary>
    /// <param name="externalId">The external ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null if not found.</returns>
    Task<ScimUser?> GetByExternalIdAsync(string externalId, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists users with optional filtering and pagination.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="filter">Optional SCIM filter expression.</param>
    /// <param name="startIndex">1-based start index for pagination.</param>
    /// <param name="count">Maximum number of results.</param>
    /// <param name="sortBy">Optional attribute to sort by.</param>
    /// <param name="sortOrder">Sort order ("ascending" or "descending").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of users and total count.</returns>
    Task<(IEnumerable<ScimUser> Users, int TotalCount)> ListAsync(
        string tenantId,
        string? filter = null,
        int startIndex = 1,
        int count = 100,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user (full replacement).
    /// </summary>
    /// <param name="user">The updated user.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user.</returns>
    Task<ScimUser> UpdateAsync(ScimUser user, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Patches a user (partial update).
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="operations">The patch operations to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The patched user.</returns>
    Task<ScimUser> PatchAsync(string id, string tenantId, IEnumerable<ScimPatchOperation> operations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a userName exists within a tenant.
    /// </summary>
    /// <param name="userName">The userName to check.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="excludeUserId">Optional user ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if userName exists.</returns>
    Task<bool> UserNameExistsAsync(string userName, string tenantId, string? excludeUserId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// SCIM Patch Operation per RFC 7644.
/// </summary>
public class ScimPatchOperation
{
    /// <summary>
    /// The operation type ("add", "remove", "replace").
    /// </summary>
    public string Op { get; set; } = string.Empty;

    /// <summary>
    /// The target path for the operation.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// The value for the operation.
    /// </summary>
    public object? Value { get; set; }
}

/// <summary>
/// SCIM Patch Request per RFC 7644.
/// </summary>
public class ScimPatchRequest
{
    /// <summary>
    /// SCIM schemas for patch request.
    /// </summary>
    public List<string> Schemas { get; set; } = [ScimConstants.Schemas.PatchOp];

    /// <summary>
    /// The patch operations to apply.
    /// </summary>
    public List<ScimPatchOperation> Operations { get; set; } = [];
}
