// ==========================================================================
// T104: TransformationService - Integration with Request Handlers
// ==========================================================================
// Service that integrates the transformation engine with adapter calls
// Applies transformations before calling adapter CreateGroupAsync/AddUserToGroupAsync
// ==========================================================================

using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Adapters;
using SCIMGateway.Core.Auditing;
using SCIMGateway.Core.Models;
using SCIMGateway.Core.Models.Transformations;

namespace SCIMGateway.Core.Handlers;

/// <summary>
/// Interface for transformation service that bridges SCIM operations with adapters.
/// </summary>
public interface ITransformationService
{
    /// <summary>
    /// Transforms a SCIM group to provider entitlements and applies them.
    /// </summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="providerId">Provider ID.</param>
    /// <param name="group">SCIM Group to transform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformation result with applied entitlements.</returns>
    Task<TransformationApplicationResult> ApplyGroupTransformationAsync(
        string tenantId,
        string providerId,
        ScimGroup group,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforms and assigns entitlements when adding a user to a group.
    /// </summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="providerId">Provider ID.</param>
    /// <param name="userId">User ID to add.</param>
    /// <param name="groupDisplayName">Group display name to transform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformation result with applied entitlements.</returns>
    Task<TransformationApplicationResult> ApplyUserToGroupTransformationAsync(
        string tenantId,
        string providerId,
        string userId,
        string groupDisplayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes entitlements when removing a user from a group.
    /// </summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="providerId">Provider ID.</param>
    /// <param name="userId">User ID to remove.</param>
    /// <param name="groupDisplayName">Group display name to reverse transform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformation result with revoked entitlements.</returns>
    Task<TransformationApplicationResult> RevokeUserFromGroupTransformationAsync(
        string tenantId,
        string providerId,
        string userId,
        string groupDisplayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Previews transformation without applying (for validation).
    /// </summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="providerId">Provider ID.</param>
    /// <param name="groupDisplayName">Group display name to preview.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preview result with potential entitlements.</returns>
    Task<TransformationPreviewResult> PreviewTransformationAsync(
        string tenantId,
        string providerId,
        string groupDisplayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs reverse transformation for drift detection.
    /// </summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="providerId">Provider ID.</param>
    /// <param name="entitlementId">Provider entitlement ID.</param>
    /// <param name="entitlementType">Type of entitlement.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching SCIM group names.</returns>
    Task<List<string>> ReverseTransformAsync(
        string tenantId,
        string providerId,
        string entitlementId,
        string entitlementType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of applying a transformation to an adapter.
/// </summary>
public class TransformationApplicationResult
{
    /// <summary>
    /// Whether the transformation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Source group display name.
    /// </summary>
    public string GroupDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Entitlements that were applied.
    /// </summary>
    public List<Entitlement> AppliedEntitlements { get; set; } = [];

    /// <summary>
    /// Entitlements that failed to apply.
    /// </summary>
    public List<EntitlementError> FailedEntitlements { get; set; } = [];

    /// <summary>
    /// Conflicts that were detected.
    /// </summary>
    public List<TransformationConflict> Conflicts { get; set; } = [];

    /// <summary>
    /// Rules that matched the input.
    /// </summary>
    public List<string> MatchedRuleIds { get; set; } = [];

    /// <summary>
    /// When the transformation was applied.
    /// </summary>
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if transformation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Error information for a failed entitlement application.
/// </summary>
public class EntitlementError
{
    /// <summary>
    /// The entitlement that failed.
    /// </summary>
    public Entitlement Entitlement { get; set; } = new();

    /// <summary>
    /// Error code from the provider.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code if applicable.
    /// </summary>
    public int? HttpStatusCode { get; set; }
}

/// <summary>
/// Implementation of transformation service.
/// </summary>
public class TransformationService : ITransformationService
{
    private readonly ITransformationEngine _transformationEngine;
    private readonly IAdapterRegistry? _adapterRegistry;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<TransformationService> _logger;

    public TransformationService(
        ITransformationEngine transformationEngine,
        IAuditLogger auditLogger,
        ILogger<TransformationService> logger,
        IAdapterRegistry? adapterRegistry = null)
    {
        _transformationEngine = transformationEngine ?? throw new ArgumentNullException(nameof(transformationEngine));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adapterRegistry = adapterRegistry;
    }

    /// <inheritdoc />
    public async Task<TransformationApplicationResult> ApplyGroupTransformationAsync(
        string tenantId,
        string providerId,
        ScimGroup group,
        CancellationToken cancellationToken = default)
    {
        var result = new TransformationApplicationResult
        {
            GroupDisplayName = group.DisplayName
        };

        try
        {
            _logger.LogInformation(
                "Applying group transformation for {GroupName} to provider {ProviderId} in tenant {TenantId}",
                group.DisplayName, providerId, tenantId);

            // Get entitlements from transformation engine
            var entitlements = await _transformationEngine.TransformGroupToEntitlementsAsync(
                tenantId, providerId, group.DisplayName);

            if (entitlements.Count == 0)
            {
                _logger.LogInformation(
                    "No transformation rules matched for group {GroupName}", group.DisplayName);
                result.Success = true;
                return result;
            }

            // Track matched rule IDs
            result.MatchedRuleIds = entitlements
                .Where(e => !string.IsNullOrEmpty(e.SourceRuleId))
                .Select(e => e.SourceRuleId!)
                .Distinct()
                .ToList();

            // Get adapter if available
            var adapter = GetAdapter(tenantId, providerId);
            if (adapter == null)
            {
                // No adapter - just return the transformation result
                result.AppliedEntitlements = entitlements;
                result.Success = true;
                
                _logger.LogInformation(
                    "Transformed {GroupName} to {EntitlementCount} entitlements (no adapter to apply)",
                    group.DisplayName, entitlements.Count);
                
                return result;
            }

            // Apply each entitlement via adapter
            foreach (var entitlement in entitlements)
            {
                try
                {
                    // Create group/role in provider if it doesn't exist
                    var providerGroup = await adapter.CreateGroupAsync(
                        new ScimGroup { DisplayName = entitlement.Name },
                        cancellationToken);

                    entitlement.ProviderEntitlementId = providerGroup.Id ?? entitlement.Name;
                    entitlement.AppliedAt = DateTime.UtcNow;
                    result.AppliedEntitlements.Add(entitlement);

                    _logger.LogDebug(
                        "Applied entitlement {EntitlementName} for group {GroupName}",
                        entitlement.Name, group.DisplayName);
                }
                catch (Exception ex)
                {
                    result.FailedEntitlements.Add(new EntitlementError
                    {
                        Entitlement = entitlement,
                        ErrorCode = ex.GetType().Name,
                        ErrorMessage = ex.Message
                    });

                    _logger.LogWarning(ex,
                        "Failed to apply entitlement {EntitlementName} for group {GroupName}",
                        entitlement.Name, group.DisplayName);
                }
            }

            result.Success = result.FailedEntitlements.Count == 0;

            // Log transformation operation
            await LogTransformationOperationAsync(
                tenantId, providerId, group.DisplayName, 
                OperationType.Transform, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error applying group transformation for {GroupName}", group.DisplayName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<TransformationApplicationResult> ApplyUserToGroupTransformationAsync(
        string tenantId,
        string providerId,
        string userId,
        string groupDisplayName,
        CancellationToken cancellationToken = default)
    {
        var result = new TransformationApplicationResult
        {
            GroupDisplayName = groupDisplayName
        };

        try
        {
            _logger.LogInformation(
                "Applying user-to-group transformation: adding user {UserId} to {GroupName} in provider {ProviderId}",
                userId, groupDisplayName, providerId);

            // Get entitlements from transformation engine
            var entitlements = await _transformationEngine.TransformGroupToEntitlementsAsync(
                tenantId, providerId, groupDisplayName);

            if (entitlements.Count == 0)
            {
                _logger.LogInformation(
                    "No transformation rules matched for group {GroupName}", groupDisplayName);
                result.Success = true;
                return result;
            }

            result.MatchedRuleIds = entitlements
                .Where(e => !string.IsNullOrEmpty(e.SourceRuleId))
                .Select(e => e.SourceRuleId!)
                .Distinct()
                .ToList();

            // Get adapter
            var adapter = GetAdapter(tenantId, providerId);
            if (adapter == null)
            {
                result.AppliedEntitlements = entitlements;
                result.Success = true;
                return result;
            }

            // Add user to each entitlement/group
            foreach (var entitlement in entitlements)
            {
                try
                {
                    await adapter.AddUserToGroupAsync(
                        entitlement.ProviderEntitlementId ?? entitlement.Name,
                        userId,
                        cancellationToken);

                    entitlement.AppliedAt = DateTime.UtcNow;
                    result.AppliedEntitlements.Add(entitlement);

                    _logger.LogDebug(
                        "Added user {UserId} to entitlement {EntitlementName}",
                        userId, entitlement.Name);
                }
                catch (Exception ex)
                {
                    result.FailedEntitlements.Add(new EntitlementError
                    {
                        Entitlement = entitlement,
                        ErrorCode = ex.GetType().Name,
                        ErrorMessage = ex.Message
                    });

                    _logger.LogWarning(ex,
                        "Failed to add user {UserId} to entitlement {EntitlementName}",
                        userId, entitlement.Name);
                }
            }

            result.Success = result.FailedEntitlements.Count == 0;

            // Log transformation operation
            await LogTransformationOperationAsync(
                tenantId, providerId, groupDisplayName,
                OperationType.AddMember, result, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error applying user-to-group transformation for user {UserId} to {GroupName}",
                userId, groupDisplayName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<TransformationApplicationResult> RevokeUserFromGroupTransformationAsync(
        string tenantId,
        string providerId,
        string userId,
        string groupDisplayName,
        CancellationToken cancellationToken = default)
    {
        var result = new TransformationApplicationResult
        {
            GroupDisplayName = groupDisplayName
        };

        try
        {
            _logger.LogInformation(
                "Revoking user-from-group transformation: removing user {UserId} from {GroupName} in provider {ProviderId}",
                userId, groupDisplayName, providerId);

            // Get entitlements from transformation engine
            var entitlements = await _transformationEngine.TransformGroupToEntitlementsAsync(
                tenantId, providerId, groupDisplayName);

            if (entitlements.Count == 0)
            {
                _logger.LogInformation(
                    "No transformation rules matched for group {GroupName}", groupDisplayName);
                result.Success = true;
                return result;
            }

            result.MatchedRuleIds = entitlements
                .Where(e => !string.IsNullOrEmpty(e.SourceRuleId))
                .Select(e => e.SourceRuleId!)
                .Distinct()
                .ToList();

            // Get adapter
            var adapter = GetAdapter(tenantId, providerId);
            if (adapter == null)
            {
                result.AppliedEntitlements = entitlements;
                result.Success = true;
                return result;
            }

            // Remove user from each entitlement/group
            foreach (var entitlement in entitlements)
            {
                try
                {
                    await adapter.RemoveUserFromGroupAsync(
                        entitlement.ProviderEntitlementId ?? entitlement.Name,
                        userId,
                        cancellationToken);

                    entitlement.AppliedAt = DateTime.UtcNow;
                    result.AppliedEntitlements.Add(entitlement);

                    _logger.LogDebug(
                        "Removed user {UserId} from entitlement {EntitlementName}",
                        userId, entitlement.Name);
                }
                catch (Exception ex)
                {
                    result.FailedEntitlements.Add(new EntitlementError
                    {
                        Entitlement = entitlement,
                        ErrorCode = ex.GetType().Name,
                        ErrorMessage = ex.Message
                    });

                    _logger.LogWarning(ex,
                        "Failed to remove user {UserId} from entitlement {EntitlementName}",
                        userId, entitlement.Name);
                }
            }

            result.Success = result.FailedEntitlements.Count == 0;

            // Log transformation operation
            await LogTransformationOperationAsync(
                tenantId, providerId, groupDisplayName,
                OperationType.RemoveMember, result, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error revoking user-from-group transformation for user {UserId} from {GroupName}",
                userId, groupDisplayName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<TransformationPreviewResult> PreviewTransformationAsync(
        string tenantId,
        string providerId,
        string groupDisplayName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Previewing transformation for {GroupName} to provider {ProviderId}",
            groupDisplayName, providerId);

        return await _transformationEngine.PreviewTransformationAsync(
            tenantId, providerId, groupDisplayName);
    }

    /// <inheritdoc />
    public async Task<List<string>> ReverseTransformAsync(
        string tenantId,
        string providerId,
        string entitlementId,
        string entitlementType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Reverse transforming entitlement {EntitlementId} of type {Type} from provider {ProviderId}",
            entitlementId, entitlementType, providerId);

        return await _transformationEngine.TransformEntitlementToGroupsAsync(
            tenantId, providerId, entitlementId, entitlementType);
    }

    /// <summary>
    /// Gets the adapter for the tenant/provider.
    /// </summary>
    private IAdapter? GetAdapter(string tenantId, string providerId)
    {
        if (_adapterRegistry == null) return null;

        try
        {
            return _adapterRegistry.GetAdapterForTenant(tenantId, providerId);
        }
        catch (AdapterNotFoundException)
        {
            _logger.LogDebug("No adapter registered for tenant {TenantId}, provider {ProviderId}",
                tenantId, providerId);
            return null;
        }
    }

    /// <summary>
    /// Logs a transformation operation to the audit log.
    /// </summary>
    private async Task LogTransformationOperationAsync(
        string tenantId,
        string providerId,
        string groupDisplayName,
        OperationType operationType,
        TransformationApplicationResult result,
        string? userId = null)
    {
        var metadata = new Dictionary<string, string>
        {
            ["providerId"] = providerId,
            ["groupDisplayName"] = groupDisplayName,
            ["matchedRules"] = string.Join(",", result.MatchedRuleIds),
            ["appliedEntitlements"] = string.Join(",", result.AppliedEntitlements.Select(e => e.Name)),
            ["success"] = result.Success.ToString()
        };

        if (!string.IsNullOrEmpty(userId))
        {
            metadata["userId"] = userId;
        }

        if (result.FailedEntitlements.Count > 0)
        {
            metadata["failedEntitlements"] = string.Join(",", 
                result.FailedEntitlements.Select(e => $"{e.Entitlement.Name}:{e.ErrorCode}"));
        }

        if (result.Conflicts.Count > 0)
        {
            metadata["conflicts"] = string.Join(",", 
                result.Conflicts.Select(c => c.GroupName));
        }

        await _auditLogger.LogAsync(new AuditLogEntry
        {
            TenantId = tenantId,
            OperationType = operationType,
            ResourceType = "TransformationRule",
            ResourceId = groupDisplayName,
            ActorId = "system",
            ActorType = ActorType.System,
            HttpStatus = result.Success ? 200 : 500,
            ErrorMessage = result.ErrorMessage,
            Metadata = metadata
        });
    }
}
