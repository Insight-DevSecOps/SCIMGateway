// ==========================================================================
// T089: TransformationTestResult Model
// ==========================================================================
// Model for transformation rule test results
// Used by rule test harness and preview endpoint
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models.Transformations;

/// <summary>
/// Result of testing a transformation rule against example inputs.
/// </summary>
public class TransformationTestResult
{
    /// <summary>
    /// Whether all test examples passed.
    /// </summary>
    [JsonPropertyName("allPassed")]
    [JsonProperty("allPassed")]
    public bool AllPassed { get; set; }

    /// <summary>
    /// Test results per example.
    /// </summary>
    [JsonPropertyName("results")]
    [JsonProperty("results")]
    public List<TestCaseResult> Results { get; set; } = [];

    /// <summary>
    /// Total number of test cases.
    /// </summary>
    [JsonPropertyName("totalTests")]
    [JsonProperty("totalTests")]
    public int TotalTests => Results.Count;

    /// <summary>
    /// Number of passed test cases.
    /// </summary>
    [JsonPropertyName("passedTests")]
    [JsonProperty("passedTests")]
    public int PassedTests => Results.Count(r => r.Passed);

    /// <summary>
    /// Number of failed test cases.
    /// </summary>
    [JsonPropertyName("failedTests")]
    [JsonProperty("failedTests")]
    public int FailedTests => Results.Count(r => !r.Passed);

    /// <summary>
    /// When the test was executed.
    /// </summary>
    [JsonPropertyName("executedAt")]
    [JsonProperty("executedAt")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The rule that was tested (optional, for context).
    /// </summary>
    [JsonPropertyName("ruleId")]
    [JsonProperty("ruleId")]
    public string? RuleId { get; set; }
}

/// <summary>
/// Result of a single test case.
/// </summary>
public class TestCaseResult
{
    /// <summary>
    /// Input value (SCIM Group displayName).
    /// </summary>
    [JsonPropertyName("input")]
    [JsonProperty("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Expected output (entitlement name or ID).
    /// </summary>
    [JsonPropertyName("expectedOutput")]
    [JsonProperty("expectedOutput")]
    public string? ExpectedOutput { get; set; }

    /// <summary>
    /// Actual output from transformation.
    /// </summary>
    [JsonPropertyName("actualOutput")]
    [JsonProperty("actualOutput")]
    public string? ActualOutput { get; set; }

    /// <summary>
    /// Whether the test case passed.
    /// </summary>
    [JsonPropertyName("passed")]
    [JsonProperty("passed")]
    public bool Passed { get; set; }

    /// <summary>
    /// Error message if test failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    [JsonProperty("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Duration of this test case in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    [JsonProperty("durationMs")]
    public long? DurationMs { get; set; }
}

/// <summary>
/// Result of a transformation preview (non-persisting).
/// </summary>
public class TransformationPreviewResult
{
    /// <summary>
    /// Input group name that was transformed.
    /// </summary>
    [JsonPropertyName("inputGroupName")]
    [JsonProperty("inputGroupName")]
    public string InputGroupName { get; set; } = string.Empty;

    /// <summary>
    /// The rule ID that matched (null if no match).
    /// </summary>
    [JsonPropertyName("matchedRuleId")]
    [JsonProperty("matchedRuleId")]
    public string? MatchedRuleId { get; set; }

    /// <summary>
    /// The transformed entitlement (null if no match).
    /// </summary>
    [JsonPropertyName("transformedEntitlement")]
    [JsonProperty("transformedEntitlement")]
    public Entitlement? TransformedEntitlement { get; set; }

    /// <summary>
    /// All entitlements if multiple rules matched (for UNION strategy).
    /// </summary>
    [JsonPropertyName("allEntitlements")]
    [JsonProperty("allEntitlements")]
    public List<Entitlement> AllEntitlements { get; set; } = [];

    /// <summary>
    /// Any conflicts detected during transformation.
    /// </summary>
    [JsonPropertyName("conflicts")]
    [JsonProperty("conflicts")]
    public List<TransformationConflict> Conflicts { get; set; } = [];

    /// <summary>
    /// When the transformation was applied (null for preview, indicates not persisted).
    /// </summary>
    [JsonPropertyName("appliedAt")]
    [JsonProperty("appliedAt")]
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// Whether this was a preview (not persisted) or actual transformation.
    /// </summary>
    [JsonPropertyName("isPreview")]
    [JsonProperty("isPreview")]
    public bool IsPreview { get; set; } = true;
}

/// <summary>
/// Represents a conflict between transformation rules.
/// </summary>
public class TransformationConflict
{
    /// <summary>
    /// Unique conflict ID.
    /// </summary>
    [JsonPropertyName("conflictId")]
    [JsonProperty("conflictId")]
    public string ConflictId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Group name that caused the conflict.
    /// </summary>
    [JsonPropertyName("groupName")]
    [JsonProperty("groupName")]
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Rule IDs that conflicted.
    /// </summary>
    [JsonPropertyName("conflictingRuleIds")]
    [JsonProperty("conflictingRuleIds")]
    public List<string> ConflictingRuleIds { get; set; } = [];

    /// <summary>
    /// Entitlements that conflicted.
    /// </summary>
    [JsonPropertyName("conflictingEntitlements")]
    [JsonProperty("conflictingEntitlements")]
    public List<string> ConflictingEntitlements { get; set; } = [];

    /// <summary>
    /// Resolution strategy applied.
    /// </summary>
    [JsonPropertyName("resolutionStrategy")]
    [JsonProperty("resolutionStrategy")]
    public ConflictResolutionStrategy ResolutionStrategy { get; set; }

    /// <summary>
    /// Status of the conflict (RESOLVED, PENDING_REVIEW, ERROR).
    /// </summary>
    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public ConflictStatus Status { get; set; } = ConflictStatus.RESOLVED;

    /// <summary>
    /// When the conflict was detected.
    /// </summary>
    [JsonPropertyName("detectedAt")]
    [JsonProperty("detectedAt")]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Status of a transformation conflict.
/// </summary>
public enum ConflictStatus
{
    /// <summary>
    /// Conflict was automatically resolved.
    /// </summary>
    RESOLVED,

    /// <summary>
    /// Conflict is pending manual review.
    /// </summary>
    PENDING_REVIEW,

    /// <summary>
    /// Conflict caused an error (ERROR strategy).
    /// </summary>
    ERROR
}
