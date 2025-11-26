// ==========================================================================
// T066: IAdapter Interface
// ==========================================================================
// Adapter interface for SaaS provider integration per contracts/adapter-interface.md
// All provider-specific adapters must implement this interface.
// ==========================================================================

using SCIMGateway.Core.Configuration;
using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Adapter interface for SaaS provider integration.
/// All provider-specific adapters must implement this interface.
/// </summary>
public interface IAdapter
{
    /// <summary>
    /// Unique identifier for this adapter instance.
    /// Format: "{providerName}-{environment}" (e.g., "salesforce-prod")
    /// </summary>
    string AdapterId { get; }

    /// <summary>
    /// Provider name (Salesforce, Workday, ServiceNow, etc.)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Adapter configuration (credentials, endpoints, settings)
    /// </summary>
    AdapterConfiguration Configuration { get; }

    /// <summary>
    /// Adapter health status
    /// </summary>
    AdapterHealthStatus HealthStatus { get; }

    // ==================== User Operations ====================

    /// <summary>
    /// Create a new user in the provider system.
    /// </summary>
    /// <param name="user">SCIM User object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user with provider-assigned ID</returns>
    /// <exception cref="AdapterException">Provider-specific error</exception>
    Task<ScimUser> CreateUserAsync(
        ScimUser user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a user by ID.
    /// </summary>
    /// <param name="userId">Provider user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIM User object or null if not found</returns>
    Task<ScimUser?> GetUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing user.
    /// </summary>
    /// <param name="userId">Provider user ID</param>
    /// <param name="user">Updated SCIM User object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user with new version</returns>
    Task<ScimUser> UpdateUserAsync(
        string userId,
        ScimUser user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a user.
    /// </summary>
    /// <param name="userId">Provider user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List users with filtering and pagination.
    /// </summary>
    /// <param name="filter">SCIM filter query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of users</returns>
    Task<PagedResult<ScimUser>> ListUsersAsync(
        QueryFilter filter,
        CancellationToken cancellationToken = default);

    // ==================== Group Operations ====================

    /// <summary>
    /// Create a new group in the provider system.
    /// </summary>
    /// <param name="group">SCIM Group object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created group with provider-assigned ID</returns>
    Task<ScimGroup> CreateGroupAsync(
        ScimGroup group,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a group by ID.
    /// </summary>
    /// <param name="groupId">Provider group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIM Group object or null if not found</returns>
    Task<ScimGroup?> GetGroupAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing group.
    /// </summary>
    /// <param name="groupId">Provider group ID</param>
    /// <param name="group">Updated SCIM Group object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated group with new version</returns>
    Task<ScimGroup> UpdateGroupAsync(
        string groupId,
        ScimGroup group,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a group.
    /// </summary>
    /// <param name="groupId">Provider group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteGroupAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List groups with filtering and pagination.
    /// </summary>
    /// <param name="filter">SCIM filter query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of groups</returns>
    Task<PagedResult<ScimGroup>> ListGroupsAsync(
        QueryFilter filter,
        CancellationToken cancellationToken = default);

    // ==================== Membership Operations ====================

    /// <summary>
    /// Add a user to a group/role/entitlement.
    /// </summary>
    /// <param name="groupId">Provider group/role ID</param>
    /// <param name="userId">Provider user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddUserToGroupAsync(
        string groupId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a user from a group/role/entitlement.
    /// </summary>
    /// <param name="groupId">Provider group/role ID</param>
    /// <param name="userId">Provider user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveUserFromGroupAsync(
        string groupId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all members of a group/role.
    /// </summary>
    /// <param name="groupId">Provider group/role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user IDs</returns>
    Task<IEnumerable<string>> GetGroupMembersAsync(
        string groupId,
        CancellationToken cancellationToken = default);

    // ==================== Transformation Operations ====================

    /// <summary>
    /// Map a SCIM group to provider-specific entitlement(s).
    /// </summary>
    /// <param name="group">SCIM Group</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider entitlement mapping</returns>
    Task<EntitlementMapping> MapGroupToEntitlementAsync(
        ScimGroup group,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverse map: provider entitlement to SCIM group.
    /// </summary>
    /// <param name="entitlement">Entitlement mapping</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SCIM Group representation</returns>
    Task<ScimGroup> MapEntitlementToGroupAsync(
        EntitlementMapping entitlement,
        CancellationToken cancellationToken = default);

    // ==================== Health & Diagnostics ====================

    /// <summary>
    /// Check adapter health status (connectivity, auth, rate limits).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current health status</returns>
    Task<AdapterHealthStatus> CheckHealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get adapter capabilities and supported features.
    /// </summary>
    /// <returns>Adapter capabilities</returns>
    AdapterCapabilities GetCapabilities();
}
