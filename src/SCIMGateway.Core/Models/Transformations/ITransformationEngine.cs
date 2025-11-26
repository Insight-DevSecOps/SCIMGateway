// ==========================================================================
// T090: ITransformationEngine Interface
// ==========================================================================
// Interface for the Transformation Engine that maps SCIM Groups to
// provider-specific entitlements and handles conflict resolution.
// ==========================================================================

namespace SCIMGateway.Core.Models.Transformations;

/// <summary>
/// Interface for the Transformation Engine that maps SCIM Groups to
/// provider-specific entitlements (roles, permissions, organizational units).
/// </summary>
public interface ITransformationEngine
{
    /// <summary>
    /// Transform SCIM Group to provider entitlements (forward transformation).
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Target provider ID (e.g., "salesforce-prod")</param>
    /// <param name="scimGroupDisplayName">SCIM Group displayName</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of mapped entitlements (roles, permissions, etc.)</returns>
    Task<List<Entitlement>> TransformGroupToEntitlementsAsync(
        string tenantId,
        string providerId,
        string scimGroupDisplayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverse transform provider entitlement to SCIM Groups (reverse transformation).
    /// Used for drift detection and pull-sync.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Source provider ID</param>
    /// <param name="providerEntitlementId">Provider-specific entitlement ID</param>
    /// <param name="providerEntitlementType">Type (role, permission, org, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching SCIM Group displayNames</returns>
    Task<List<string>> TransformEntitlementToGroupsAsync(
        string tenantId,
        string providerId,
        string providerEntitlementId,
        string providerEntitlementType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all transformation rules for a provider (ordered by priority).
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Provider ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transformation rules ordered by priority (ascending)</returns>
    Task<List<TransformationRule>> GetRulesAsync(
        string tenantId,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new transformation rule.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="rule">The rule to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created rule with generated ID</returns>
    Task<TransformationRule> CreateRuleAsync(
        string tenantId,
        TransformationRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing transformation rule.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="ruleId">ID of the rule to update</param>
    /// <param name="rule">The updated rule data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated rule</returns>
    Task<TransformationRule> UpdateRuleAsync(
        string tenantId,
        string ruleId,
        TransformationRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a transformation rule.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="ruleId">ID of the rule to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRuleAsync(
        string tenantId,
        string ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test a transformation rule against example inputs (validation).
    /// </summary>
    /// <param name="rule">The rule to test</param>
    /// <param name="testInputs">List of test input strings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results for each input</returns>
    Task<TransformationTestResult> TestRuleAsync(
        TransformationRule rule,
        List<string> testInputs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve conflicts when multiple rules match the same group.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Provider ID</param>
    /// <param name="scimGroupDisplayName">The group that caused conflicts</param>
    /// <param name="conflictingEntitlements">List of conflicting entitlements</param>
    /// <param name="strategy">Conflict resolution strategy to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resolved list of entitlements</returns>
    Task<List<Entitlement>> ResolveConflictsAsync(
        string tenantId,
        string providerId,
        string scimGroupDisplayName,
        List<Entitlement> conflictingEntitlements,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a transformation rule (syntax, patterns, conflicts).
    /// </summary>
    /// <param name="rule">The rule to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any errors</returns>
    Task<RuleValidationResult> ValidateRuleAsync(
        TransformationRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview a transformation without persisting (for UI/testing).
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Provider ID</param>
    /// <param name="scimGroupDisplayName">Group name to transform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview result with matched rules and entitlements</returns>
    Task<TransformationPreviewResult> PreviewTransformationAsync(
        string tenantId,
        string providerId,
        string scimGroupDisplayName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of validating a transformation rule.
/// </summary>
public class RuleValidationResult
{
    /// <summary>
    /// Whether the rule is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors (if any).
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// List of warnings (non-fatal issues).
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static RuleValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static RuleValidationResult Failure(params string[] errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}
