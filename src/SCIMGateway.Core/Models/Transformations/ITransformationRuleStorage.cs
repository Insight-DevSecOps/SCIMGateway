// ==========================================================================
// ITransformationRuleStorage Interface
// ==========================================================================
// Interface for transformation rule storage operations.
// Implemented by Cosmos DB storage in T100.
// ==========================================================================

namespace SCIMGateway.Core.Models.Transformations;

/// <summary>
/// Interface for transformation rule storage operations.
/// </summary>
public interface ITransformationRuleStorage
{
    /// <summary>
    /// Gets all transformation rules for a tenant and provider.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Provider ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transformation rules ordered by priority</returns>
    Task<List<TransformationRule>> GetRulesAsync(
        string tenantId,
        string providerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific rule by ID.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rule if found, null otherwise</returns>
    Task<TransformationRule?> GetRuleByIdAsync(
        string tenantId,
        string ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new transformation rule.
    /// </summary>
    /// <param name="rule">The rule to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created rule</returns>
    Task<TransformationRule> CreateRuleAsync(
        TransformationRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transformation rule.
    /// </summary>
    /// <param name="rule">The rule with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated rule</returns>
    Task<TransformationRule> UpdateRuleAsync(
        TransformationRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a transformation rule.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="ruleId">Rule ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRuleAsync(
        string tenantId,
        string ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rules for a tenant (across all providers).
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all transformation rules for the tenant</returns>
    Task<List<TransformationRule>> GetAllRulesForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of ITransformationRuleStorage for testing.
/// </summary>
public class InMemoryTransformationRuleStorage : ITransformationRuleStorage
{
    private readonly List<TransformationRule> _rules = [];
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<List<TransformationRule>> GetRulesAsync(
        string tenantId,
        string providerId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var rules = _rules
                .Where(r => r.TenantId == tenantId && r.ProviderId == providerId)
                .OrderBy(r => r.Priority)
                .ToList();
            return Task.FromResult(rules);
        }
    }

    /// <inheritdoc />
    public Task<TransformationRule?> GetRuleByIdAsync(
        string tenantId,
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var rule = _rules.FirstOrDefault(r => r.TenantId == tenantId && r.Id == ruleId);
            return Task.FromResult(rule);
        }
    }

    /// <inheritdoc />
    public Task<TransformationRule> CreateRuleAsync(
        TransformationRule rule,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(rule.Id))
            {
                rule.Id = Guid.NewGuid().ToString();
            }
            rule.CreatedAt = DateTime.UtcNow;
            rule.UpdatedAt = DateTime.UtcNow;
            _rules.Add(rule);
            return Task.FromResult(rule);
        }
    }

    /// <inheritdoc />
    public Task<TransformationRule> UpdateRuleAsync(
        TransformationRule rule,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var existing = _rules.FirstOrDefault(r => r.TenantId == rule.TenantId && r.Id == rule.Id);
            if (existing != null)
            {
                _rules.Remove(existing);
            }
            rule.UpdatedAt = DateTime.UtcNow;
            _rules.Add(rule);
            return Task.FromResult(rule);
        }
    }

    /// <inheritdoc />
    public Task DeleteRuleAsync(
        string tenantId,
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var rule = _rules.FirstOrDefault(r => r.TenantId == tenantId && r.Id == ruleId);
            if (rule != null)
            {
                _rules.Remove(rule);
            }
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<List<TransformationRule>> GetAllRulesForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var rules = _rules
                .Where(r => r.TenantId == tenantId)
                .OrderBy(r => r.ProviderId)
                .ThenBy(r => r.Priority)
                .ToList();
            return Task.FromResult(rules);
        }
    }

    /// <summary>
    /// Adds a rule directly (for testing).
    /// </summary>
    public void AddRule(TransformationRule rule)
    {
        lock (_lock)
        {
            _rules.Add(rule);
        }
    }

    /// <summary>
    /// Clears all rules (for testing).
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _rules.Clear();
        }
    }
}
