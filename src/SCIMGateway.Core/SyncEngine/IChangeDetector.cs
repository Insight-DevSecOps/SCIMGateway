// ==========================================================================
// T121-T123: IChangeDetector - Change Detection Interface
// ==========================================================================
// Interface for detecting changes, drift, and conflicts between systems
// ==========================================================================

using SCIMGateway.Core.Models;

namespace SCIMGateway.Core.SyncEngine;

/// <summary>
/// Interface for detecting changes between Entra ID and provider states.
/// </summary>
public interface IChangeDetector
{
    /// <summary>
    /// Detects all changes between previous and current states.
    /// </summary>
    /// <param name="context">The change detection context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Change detection result with drift and conflict reports.</returns>
    Task<ChangeDetectionResult> DetectChangesAsync(ChangeDetectionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects user changes between previous and current states.
    /// </summary>
    /// <param name="previousUsers">Previous user state.</param>
    /// <param name="currentUsers">Current user state.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>List of detected user changes.</returns>
    IEnumerable<DriftReport> DetectUserChanges(
        IEnumerable<ScimUser> previousUsers,
        IEnumerable<ScimUser> currentUsers,
        string tenantId,
        string providerId);

    /// <summary>
    /// Detects group changes between previous and current states.
    /// </summary>
    /// <param name="previousGroups">Previous group state.</param>
    /// <param name="currentGroups">Current group state.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>List of detected group changes.</returns>
    IEnumerable<DriftReport> DetectGroupChanges(
        IEnumerable<ScimGroup> previousGroups,
        IEnumerable<ScimGroup> currentGroups,
        string tenantId,
        string providerId);

    /// <summary>
    /// Detects drift between Entra ID state and provider state.
    /// </summary>
    /// <param name="entraState">The Entra ID state.</param>
    /// <param name="providerState">The provider state.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>List of detected drift reports.</returns>
    IEnumerable<DriftReport> DetectDrift(
        ResourceState entraState,
        ResourceState providerState,
        string tenantId,
        string providerId);

    /// <summary>
    /// Detects conflicts where the same resource was modified in both systems.
    /// </summary>
    /// <param name="entraChanges">Changes detected in Entra ID.</param>
    /// <param name="providerChanges">Changes detected in provider.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>List of detected conflict reports.</returns>
    IEnumerable<ConflictReport> DetectConflicts(
        IEnumerable<DriftReport> entraChanges,
        IEnumerable<DriftReport> providerChanges,
        string tenantId,
        string providerId);

    /// <summary>
    /// Compares two resource states and identifies attribute-level differences.
    /// </summary>
    /// <param name="previous">Previous state.</param>
    /// <param name="current">Current state.</param>
    /// <returns>List of attribute-level differences.</returns>
    IEnumerable<AttributeDrift> CompareAttributes(object previous, object current);

    /// <summary>
    /// Computes a hash of the resource state for quick comparison.
    /// </summary>
    /// <param name="resources">The resources to hash.</param>
    /// <returns>Hash of the resource state.</returns>
    string ComputeStateHash(IEnumerable<object> resources);
}

/// <summary>
/// Context for change detection operation.
/// </summary>
public class ChangeDetectionContext
{
    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Previous Entra ID state.
    /// </summary>
    public ResourceState? PreviousEntraState { get; set; }

    /// <summary>
    /// Current Entra ID state.
    /// </summary>
    public ResourceState? CurrentEntraState { get; set; }

    /// <summary>
    /// Previous provider state.
    /// </summary>
    public ResourceState? PreviousProviderState { get; set; }

    /// <summary>
    /// Current provider state.
    /// </summary>
    public ResourceState? CurrentProviderState { get; set; }

    /// <summary>
    /// Last known state hash for quick comparison.
    /// </summary>
    public string? LastKnownStateHash { get; set; }

    /// <summary>
    /// Sync direction for reconciliation context.
    /// </summary>
    public SyncDirection SyncDirection { get; set; } = SyncDirection.EntraToSaas;

    /// <summary>
    /// Attributes to ignore during comparison.
    /// </summary>
    public HashSet<string>? IgnoredAttributes { get; set; }
}

/// <summary>
/// Represents the state of resources at a point in time.
/// </summary>
public class ResourceState
{
    /// <summary>
    /// Users in this state.
    /// </summary>
    public List<ScimUser> Users { get; set; } = new();

    /// <summary>
    /// Groups in this state.
    /// </summary>
    public List<ScimGroup> Groups { get; set; } = new();

    /// <summary>
    /// Timestamp when this state was captured.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Hash of this state for quick comparison.
    /// </summary>
    public string? StateHash { get; set; }

    /// <summary>
    /// Source of this state (Entra or Provider).
    /// </summary>
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Result of a change detection operation.
/// </summary>
public class ChangeDetectionResult
{
    /// <summary>
    /// Whether any changes were detected.
    /// </summary>
    public bool HasChanges => DriftReports.Count > 0 || ConflictReports.Count > 0;

    /// <summary>
    /// Whether any conflicts were detected.
    /// </summary>
    public bool HasConflicts => ConflictReports.Count > 0;

    /// <summary>
    /// Detected drift reports.
    /// </summary>
    public List<DriftReport> DriftReports { get; set; } = new();

    /// <summary>
    /// Detected conflict reports.
    /// </summary>
    public List<ConflictReport> ConflictReports { get; set; } = new();

    /// <summary>
    /// Users added.
    /// </summary>
    public int UsersAdded { get; set; }

    /// <summary>
    /// Users modified.
    /// </summary>
    public int UsersModified { get; set; }

    /// <summary>
    /// Users deleted.
    /// </summary>
    public int UsersDeleted { get; set; }

    /// <summary>
    /// Groups added.
    /// </summary>
    public int GroupsAdded { get; set; }

    /// <summary>
    /// Groups modified.
    /// </summary>
    public int GroupsModified { get; set; }

    /// <summary>
    /// Groups deleted.
    /// </summary>
    public int GroupsDeleted { get; set; }

    /// <summary>
    /// Total changes detected.
    /// </summary>
    public int TotalChanges => UsersAdded + UsersModified + UsersDeleted + 
                               GroupsAdded + GroupsModified + GroupsDeleted;

    /// <summary>
    /// Hash of the new state after detection.
    /// </summary>
    public string? NewStateHash { get; set; }

    /// <summary>
    /// Timestamp of detection.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Summary of detection results.
    /// </summary>
    public string Summary => $"Detected {TotalChanges} changes: " +
        $"{UsersAdded} users added, {UsersModified} modified, {UsersDeleted} deleted; " +
        $"{GroupsAdded} groups added, {GroupsModified} modified, {GroupsDeleted} deleted; " +
        $"{ConflictReports.Count} conflicts";
}
