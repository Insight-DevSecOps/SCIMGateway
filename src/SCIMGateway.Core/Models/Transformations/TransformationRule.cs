// ==========================================================================
// T086: TransformationRule Model
// ==========================================================================
// Model for transformation rules that map SCIM Groups to provider entitlements
// Supports EXACT, REGEX, HIERARCHICAL, and CONDITIONAL pattern matching
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models.Transformations;

/// <summary>
/// Transformation rule for mapping SCIM Groups to provider-specific entitlements.
/// </summary>
public class TransformationRule
{
    /// <summary>
    /// Unique rule ID (GUID).
    /// </summary>
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant ID for multi-tenant isolation.
    /// </summary>
    [JsonPropertyName("tenantId")]
    [JsonProperty("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Provider ID (e.g., "salesforce-prod", "workday-prod").
    /// </summary>
    [JsonPropertyName("providerId")]
    [JsonProperty("providerId")]
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider name (human-readable, e.g., "Salesforce").
    /// </summary>
    [JsonPropertyName("providerName")]
    [JsonProperty("providerName")]
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Rule type (EXACT, REGEX, HIERARCHICAL, CONDITIONAL).
    /// </summary>
    [JsonPropertyName("ruleType")]
    [JsonProperty("ruleType")]
    public RuleType RuleType { get; set; } = RuleType.EXACT;

    /// <summary>
    /// Source pattern (SCIM Group displayName pattern).
    /// For EXACT: literal string match.
    /// For REGEX: regular expression with optional capture groups.
    /// For HIERARCHICAL: path pattern (e.g., "Company/Division/Department").
    /// For CONDITIONAL: condition expression.
    /// </summary>
    [JsonPropertyName("sourcePattern")]
    [JsonProperty("sourcePattern")]
    public string SourcePattern { get; set; } = string.Empty;

    /// <summary>
    /// Source type (always "SCIM_GROUP" for now).
    /// </summary>
    [JsonPropertyName("sourceType")]
    [JsonProperty("sourceType")]
    public string SourceType { get; set; } = "SCIM_GROUP";

    /// <summary>
    /// Target type (SALESFORCE_ROLE, WORKDAY_ORG, SERVICENOW_GROUP, etc.).
    /// </summary>
    [JsonPropertyName("targetType")]
    [JsonProperty("targetType")]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Target mapping (provider-specific entitlement identifier or template).
    /// Supports template variables: ${1}, ${2} for capture groups, ${level0}, ${level1} for hierarchical.
    /// </summary>
    [JsonPropertyName("targetMapping")]
    [JsonProperty("targetMapping")]
    public string TargetMapping { get; set; } = string.Empty;

    /// <summary>
    /// Rule priority (lower = higher priority, 1 is highest).
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonProperty("priority")]
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Whether rule is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Conflict resolution strategy if this rule conflicts with others.
    /// </summary>
    [JsonPropertyName("conflictResolution")]
    [JsonProperty("conflictResolution")]
    public ConflictResolutionStrategy ConflictResolution { get; set; } = ConflictResolutionStrategy.UNION;

    /// <summary>
    /// Additional metadata (JSON object for custom config).
    /// For CONDITIONAL rules, may contain condition expression.
    /// For HIGHEST_PRIVILEGE resolution, may contain privilege rankings.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonProperty("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Example inputs/outputs for testing.
    /// </summary>
    [JsonPropertyName("examples")]
    [JsonProperty("examples")]
    public List<TransformationExample> Examples { get; set; } = [];

    /// <summary>
    /// When this rule was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this rule was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created this rule (user ID or system).
    /// </summary>
    [JsonPropertyName("createdBy")]
    [JsonProperty("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Types of transformation rules.
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Exact string match (e.g., "Sales Team" → "Sales_Representative").
    /// Case-sensitive, no partial matching.
    /// </summary>
    EXACT,

    /// <summary>
    /// Regex pattern match (e.g., "^Sales-(.*)$" → "Sales_${1}_Rep").
    /// Supports capture groups for template substitution.
    /// </summary>
    REGEX,

    /// <summary>
    /// Hierarchical path mapping (e.g., "Company/Division/Department" → Workday org).
    /// Parses path levels for template variables ${level0}, ${level1}, etc.
    /// </summary>
    HIERARCHICAL,

    /// <summary>
    /// Conditional logic (e.g., if group contains "Manager" → higher-privilege role).
    /// Evaluates conditions stored in metadata.
    /// </summary>
    CONDITIONAL
}

/// <summary>
/// Conflict resolution strategies when multiple rules match the same group.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// Assign all conflicting entitlements (union).
    /// Default strategy for providers supporting multiple roles.
    /// </summary>
    UNION,

    /// <summary>
    /// Assign only the first matching rule (by priority).
    /// Use for providers restricted to single role.
    /// </summary>
    FIRST_MATCH,

    /// <summary>
    /// Assign highest-privilege entitlement (requires privilege ranking in metadata).
    /// Use for security-sensitive providers with privilege hierarchy.
    /// </summary>
    HIGHEST_PRIVILEGE,

    /// <summary>
    /// Require manual review/approval, do not auto-assign.
    /// Creates conflict log entry and notifies admin.
    /// </summary>
    MANUAL_REVIEW,

    /// <summary>
    /// Fail with error, alert admin.
    /// Use for strict governance, no automatic conflict resolution allowed.
    /// </summary>
    ERROR
}
