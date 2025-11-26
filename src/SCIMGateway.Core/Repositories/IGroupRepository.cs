// ==========================================================================
// T040-T045: IGroupRepository - Group Data Access Interface
// ==========================================================================
// Repository interface for SCIM Group CRUD operations
// ==========================================================================

using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Repositories;

/// <summary>
/// Repository interface for SCIM Group operations.
/// </summary>
public interface IGroupRepository
{
    /// <summary>
    /// Creates a new group.
    /// </summary>
    /// <param name="group">The group to create.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created group with assigned ID.</returns>
    Task<ScimGroup> CreateAsync(ScimGroup group, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a group by ID.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The group, or null if not found.</returns>
    Task<ScimGroup?> GetByIdAsync(string id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a group by displayName.
    /// </summary>
    /// <param name="displayName">The displayName.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The group, or null if not found.</returns>
    Task<ScimGroup?> GetByDisplayNameAsync(string displayName, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a group by externalId.
    /// </summary>
    /// <param name="externalId">The external ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The group, or null if not found.</returns>
    Task<ScimGroup?> GetByExternalIdAsync(string externalId, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists groups with optional filtering and pagination.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="filter">Optional SCIM filter expression.</param>
    /// <param name="startIndex">1-based start index for pagination.</param>
    /// <param name="count">Maximum number of results.</param>
    /// <param name="sortBy">Optional attribute to sort by.</param>
    /// <param name="sortOrder">Sort order ("ascending" or "descending").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged list of groups and total count.</returns>
    Task<(IEnumerable<ScimGroup> Groups, int TotalCount)> ListAsync(
        string tenantId,
        string? filter = null,
        int startIndex = 1,
        int count = 100,
        string? sortBy = null,
        string? sortOrder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a group (full replacement).
    /// </summary>
    /// <param name="group">The updated group.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated group.</returns>
    Task<ScimGroup> UpdateAsync(ScimGroup group, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Patches a group (partial update).
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="operations">The patch operations to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The patched group.</returns>
    Task<ScimGroup> PatchAsync(string id, string tenantId, IEnumerable<ScimPatchOperation> operations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a group.
    /// </summary>
    /// <param name="id">The group ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a displayName exists within a tenant.
    /// </summary>
    /// <param name="displayName">The displayName to check.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="excludeGroupId">Optional group ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if displayName exists.</returns>
    Task<bool> DisplayNameExistsAsync(string displayName, string tenantId, string? excludeGroupId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a member to a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="member">The member to add.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated group.</returns>
    Task<ScimGroup> AddMemberAsync(string groupId, ScimGroupMember member, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a member from a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="memberId">The member ID to remove.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated group.</returns>
    Task<ScimGroup> RemoveMemberAsync(string groupId, string memberId, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the members of a group.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of group members.</returns>
    Task<IEnumerable<ScimGroupMember>> GetMembersAsync(string groupId, string tenantId, CancellationToken cancellationToken = default);
}
