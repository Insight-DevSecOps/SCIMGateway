// ==========================================================================
// T100: CosmosTransformationRuleStorage - Cosmos DB Storage for Rules
// ==========================================================================
// Implements ITransformationRuleStorage using Azure Cosmos DB
// Provides CRUD operations with tenantId/providerId filtering
// ==========================================================================

using Microsoft.Extensions.Logging;
using SCIMGateway.Core.Data;

namespace SCIMGateway.Core.Models.Transformations;

/// <summary>
/// Cosmos DB implementation of transformation rule storage.
/// Uses the transformation-rules container with tenantId as partition key.
/// </summary>
public class CosmosTransformationRuleStorage : ITransformationRuleStorage
{
    private readonly ICosmosDbClient _cosmosClient;
    private readonly ILogger<CosmosTransformationRuleStorage> _logger;

    /// <summary>
    /// Creates a new CosmosTransformationRuleStorage instance.
    /// </summary>
    public CosmosTransformationRuleStorage(
        ICosmosDbClient cosmosClient,
        ILogger<CosmosTransformationRuleStorage> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<TransformationRule>> GetRulesAsync(
        string tenantId,
        string providerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        _logger.LogDebug(
            "Fetching transformation rules for tenant '{TenantId}' provider '{ProviderId}'",
            tenantId, providerId);

        var container = _cosmosClient.GetTransformationRulesContainer();

        // Query by tenantId (partition key) and providerId, ordered by priority
        var query = $@"
            SELECT * FROM c 
            WHERE c.tenantId = '{EscapeString(tenantId)}' 
              AND c.providerId = '{EscapeString(providerId)}'
            ORDER BY c.priority ASC";

        var rules = await _cosmosClient.QueryItemsAsync<TransformationRule>(
            container,
            query,
            tenantId);

        _logger.LogDebug(
            "Retrieved {Count} transformation rules for tenant '{TenantId}' provider '{ProviderId}'",
            rules.Count, tenantId, providerId);

        return rules;
    }

    /// <inheritdoc />
    public async Task<TransformationRule?> GetRuleByIdAsync(
        string tenantId,
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

        _logger.LogDebug(
            "Fetching transformation rule '{RuleId}' for tenant '{TenantId}'",
            ruleId, tenantId);

        var container = _cosmosClient.GetTransformationRulesContainer();

        var rule = await _cosmosClient.ReadItemAsync<TransformationRule>(
            container,
            ruleId,
            tenantId);

        if (rule == null)
        {
            _logger.LogDebug(
                "Transformation rule '{RuleId}' not found for tenant '{TenantId}'",
                ruleId, tenantId);
        }

        return rule;
    }

    /// <inheritdoc />
    public async Task<TransformationRule> CreateRuleAsync(
        TransformationRule rule,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(rule.TenantId);

        // Ensure ID is set
        if (string.IsNullOrEmpty(rule.Id))
        {
            rule.Id = Guid.NewGuid().ToString();
        }

        // Set timestamps
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;

        _logger.LogDebug(
            "Creating transformation rule '{RuleId}' for tenant '{TenantId}' provider '{ProviderId}'",
            rule.Id, rule.TenantId, rule.ProviderId);

        var container = _cosmosClient.GetTransformationRulesContainer();

        var created = await _cosmosClient.CreateItemAsync(
            container,
            rule,
            rule.TenantId);

        _logger.LogInformation(
            "Created transformation rule '{RuleId}' for tenant '{TenantId}' provider '{ProviderId}'",
            created.Id, created.TenantId, created.ProviderId);

        return created;
    }

    /// <inheritdoc />
    public async Task<TransformationRule> UpdateRuleAsync(
        TransformationRule rule,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(rule.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rule.Id);

        // Update timestamp
        rule.UpdatedAt = DateTime.UtcNow;

        _logger.LogDebug(
            "Updating transformation rule '{RuleId}' for tenant '{TenantId}'",
            rule.Id, rule.TenantId);

        var container = _cosmosClient.GetTransformationRulesContainer();

        var updated = await _cosmosClient.UpsertItemAsync(
            container,
            rule,
            rule.TenantId);

        _logger.LogInformation(
            "Updated transformation rule '{RuleId}' for tenant '{TenantId}'",
            updated.Id, updated.TenantId);

        return updated;
    }

    /// <inheritdoc />
    public async Task DeleteRuleAsync(
        string tenantId,
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);

        _logger.LogDebug(
            "Deleting transformation rule '{RuleId}' for tenant '{TenantId}'",
            ruleId, tenantId);

        var container = _cosmosClient.GetTransformationRulesContainer();

        await _cosmosClient.DeleteItemAsync(
            container,
            ruleId,
            tenantId);

        _logger.LogInformation(
            "Deleted transformation rule '{RuleId}' for tenant '{TenantId}'",
            ruleId, tenantId);
    }

    /// <inheritdoc />
    public async Task<List<TransformationRule>> GetAllRulesForTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        _logger.LogDebug(
            "Fetching all transformation rules for tenant '{TenantId}'",
            tenantId);

        var container = _cosmosClient.GetTransformationRulesContainer();

        // Query all rules for tenant, ordered by providerId then priority
        var query = $@"
            SELECT * FROM c 
            WHERE c.tenantId = '{EscapeString(tenantId)}'
            ORDER BY c.providerId ASC, c.priority ASC";

        var rules = await _cosmosClient.QueryItemsAsync<TransformationRule>(
            container,
            query,
            tenantId);

        _logger.LogDebug(
            "Retrieved {Count} transformation rules for tenant '{TenantId}'",
            rules.Count, tenantId);

        return rules;
    }

    /// <summary>
    /// Gets all enabled rules for a specific provider.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Provider ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of enabled rules ordered by priority</returns>
    public async Task<List<TransformationRule>> GetEnabledRulesAsync(
        string tenantId,
        string providerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        _logger.LogDebug(
            "Fetching enabled transformation rules for tenant '{TenantId}' provider '{ProviderId}'",
            tenantId, providerId);

        var container = _cosmosClient.GetTransformationRulesContainer();

        var query = $@"
            SELECT * FROM c 
            WHERE c.tenantId = '{EscapeString(tenantId)}' 
              AND c.providerId = '{EscapeString(providerId)}'
              AND c.enabled = true
            ORDER BY c.priority ASC";

        var rules = await _cosmosClient.QueryItemsAsync<TransformationRule>(
            container,
            query,
            tenantId);

        _logger.LogDebug(
            "Retrieved {Count} enabled transformation rules for tenant '{TenantId}' provider '{ProviderId}'",
            rules.Count, tenantId, providerId);

        return rules;
    }

    /// <summary>
    /// Gets rules by rule type.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Provider ID to filter by</param>
    /// <param name="ruleType">Rule type to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of rules matching the type</returns>
    public async Task<List<TransformationRule>> GetRulesByTypeAsync(
        string tenantId,
        string providerId,
        RuleType ruleType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        _logger.LogDebug(
            "Fetching transformation rules of type '{RuleType}' for tenant '{TenantId}' provider '{ProviderId}'",
            ruleType, tenantId, providerId);

        var container = _cosmosClient.GetTransformationRulesContainer();

        // RuleType is stored as integer in Cosmos DB
        var ruleTypeValue = (int)ruleType;
        var query = $@"
            SELECT * FROM c 
            WHERE c.tenantId = '{EscapeString(tenantId)}' 
              AND c.providerId = '{EscapeString(providerId)}'
              AND c.ruleType = {ruleTypeValue}
            ORDER BY c.priority ASC";

        var rules = await _cosmosClient.QueryItemsAsync<TransformationRule>(
            container,
            query,
            tenantId);

        _logger.LogDebug(
            "Retrieved {Count} transformation rules of type '{RuleType}'",
            rules.Count, ruleType);

        return rules;
    }

    /// <summary>
    /// Checks if a rule with the same source pattern exists.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Provider ID to filter by</param>
    /// <param name="sourcePattern">Source pattern to check</param>
    /// <param name="excludeRuleId">Optional rule ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a duplicate exists</returns>
    public async Task<bool> SourcePatternExistsAsync(
        string tenantId,
        string providerId,
        string sourcePattern,
        string? excludeRuleId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePattern);

        var container = _cosmosClient.GetTransformationRulesContainer();

        var excludeClause = string.IsNullOrEmpty(excludeRuleId)
            ? ""
            : $" AND c.id != '{EscapeString(excludeRuleId)}'";

        var query = $@"
            SELECT VALUE COUNT(1) FROM c 
            WHERE c.tenantId = '{EscapeString(tenantId)}' 
              AND c.providerId = '{EscapeString(providerId)}'
              AND c.sourcePattern = '{EscapeString(sourcePattern)}'
              {excludeClause}";

        var results = await _cosmosClient.QueryItemsAsync<int>(
            container,
            query,
            tenantId);

        return results.FirstOrDefault() > 0;
    }

    /// <summary>
    /// Escapes a string for use in Cosmos DB SQL queries.
    /// </summary>
    private static string EscapeString(string value)
    {
        return value.Replace("'", "''");
    }
}
