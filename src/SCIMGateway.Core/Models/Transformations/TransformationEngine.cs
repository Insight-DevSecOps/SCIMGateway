// ==========================================================================
// T091-T098: TransformationEngine Implementation
// ==========================================================================
// Full implementation of the Transformation Engine including:
// - T091: Base engine (load rules, match, apply, handle conflicts)
// - T092: EXACT pattern matching
// - T093: REGEX pattern matching with capture groups
// - T094: HIERARCHICAL pattern matching with path levels
// - T095: CONDITIONAL pattern matching
// - T096: Reverse transformation (entitlement → group)
// - T097: Conflict resolution strategies
// - T098: Rule validation
// ==========================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SCIMGateway.Core.Models.Transformations;

/// <summary>
/// Implementation of the Transformation Engine that maps SCIM Groups to
/// provider-specific entitlements using configurable rules.
/// </summary>
public class TransformationEngine : ITransformationEngine
{
    private readonly ITransformationRuleStorage _ruleStorage;
    private readonly IMemoryCache _ruleCache;
    private readonly ILogger<TransformationEngine> _logger;

    // Cache for compiled regex patterns (T093 performance optimization)
    private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

    // Cache expiration for rules (5 minutes as per contract)
    private static readonly TimeSpan RuleCacheExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Creates a new TransformationEngine instance.
    /// </summary>
    public TransformationEngine(
        ITransformationRuleStorage ruleStorage,
        IMemoryCache ruleCache,
        ILogger<TransformationEngine> logger)
    {
        _ruleStorage = ruleStorage ?? throw new ArgumentNullException(nameof(ruleStorage));
        _ruleCache = ruleCache ?? throw new ArgumentNullException(nameof(ruleCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region T091: Core Transformation (Forward)

    /// <inheritdoc />
    public async Task<List<Entitlement>> TransformGroupToEntitlementsAsync(
        string tenantId,
        string providerId,
        string scimGroupDisplayName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scimGroupDisplayName);

        _logger.LogDebug(
            "Transforming group '{GroupName}' for tenant '{TenantId}' provider '{ProviderId}'",
            scimGroupDisplayName, tenantId, providerId);

        // Get rules from cache or storage
        var rules = await GetRulesAsync(tenantId, providerId, cancellationToken);
        var matchedEntitlements = new List<Entitlement>();

        // Apply rules in priority order (lower = higher priority)
        foreach (var rule in rules.Where(r => r.Enabled).OrderBy(r => r.Priority))
        {
            var matchResult = TryMatchRule(rule, scimGroupDisplayName);
            if (matchResult.IsMatch)
            {
                var entitlement = CreateEntitlementFromMatch(rule, scimGroupDisplayName, matchResult);
                matchedEntitlements.Add(entitlement);

                _logger.LogDebug(
                    "Rule '{RuleId}' matched group '{GroupName}' -> entitlement '{EntitlementName}'",
                    rule.Id, scimGroupDisplayName, entitlement.Name);
            }
        }

        // Handle conflicts if multiple entitlements matched
        if (matchedEntitlements.Count > 1)
        {
            var strategy = matchedEntitlements.First().Metadata?.ContainsKey("conflictResolution") == true
                ? Enum.Parse<ConflictResolutionStrategy>(matchedEntitlements.First().Metadata!["conflictResolution"].ToString()!)
                : rules.FirstOrDefault()?.ConflictResolution ?? ConflictResolutionStrategy.UNION;

            matchedEntitlements = await ResolveConflictsAsync(
                tenantId, providerId, scimGroupDisplayName,
                matchedEntitlements, strategy, cancellationToken);
        }

        _logger.LogInformation(
            "Transformed group '{GroupName}' to {Count} entitlements",
            scimGroupDisplayName, matchedEntitlements.Count);

        return matchedEntitlements;
    }

    #endregion

    #region T092-T095: Pattern Matching

    /// <summary>
    /// Tries to match a rule against a group display name.
    /// </summary>
    private RuleMatchResult TryMatchRule(TransformationRule rule, string groupDisplayName)
    {
        return rule.RuleType switch
        {
            RuleType.EXACT => MatchExact(rule, groupDisplayName),
            RuleType.REGEX => MatchRegex(rule, groupDisplayName),
            RuleType.HIERARCHICAL => MatchHierarchical(rule, groupDisplayName),
            RuleType.CONDITIONAL => MatchConditional(rule, groupDisplayName),
            _ => new RuleMatchResult { IsMatch = false }
        };
    }

    /// <summary>
    /// T092: EXACT pattern matching - case-sensitive string match.
    /// </summary>
    private static RuleMatchResult MatchExact(TransformationRule rule, string groupDisplayName)
    {
        // Exact match is case-sensitive
        var isMatch = rule.SourcePattern == groupDisplayName;

        return new RuleMatchResult
        {
            IsMatch = isMatch,
            TransformedValue = isMatch ? rule.TargetMapping : null,
            CaptureGroups = new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// T093: REGEX pattern matching with capture groups and template substitution.
    /// </summary>
    private RuleMatchResult MatchRegex(TransformationRule rule, string groupDisplayName)
    {
        try
        {
            var regex = GetOrCompileRegex(rule.SourcePattern);
            var match = regex.Match(groupDisplayName);

            if (!match.Success)
            {
                return new RuleMatchResult { IsMatch = false };
            }

            // Extract capture groups
            var captureGroups = new Dictionary<string, string>();
            for (int i = 0; i < match.Groups.Count; i++)
            {
                captureGroups[$"${{{i}}}"] = match.Groups[i].Value;
            }

            // Apply template substitution ${1}, ${2}, etc.
            var transformedValue = ApplyTemplateSubstitution(rule.TargetMapping, captureGroups);

            return new RuleMatchResult
            {
                IsMatch = true,
                TransformedValue = transformedValue,
                CaptureGroups = captureGroups
            };
        }
        catch (RegexMatchTimeoutException ex)
        {
            _logger.LogWarning(ex, "Regex timeout for pattern '{Pattern}'", rule.SourcePattern);
            return new RuleMatchResult { IsMatch = false };
        }
    }

    /// <summary>
    /// T094: HIERARCHICAL pattern matching - path parsing with level extraction.
    /// Parses paths like "Company/Division/Department/Team" into ${level0}, ${level1}, etc.
    /// </summary>
    private static RuleMatchResult MatchHierarchical(TransformationRule rule, string groupDisplayName)
    {
        // Split the group name by path separator
        var pathSeparator = '/';
        var levels = groupDisplayName.Split(pathSeparator);

        // Check if we have enough levels based on the pattern
        var patternLevels = rule.SourcePattern.Split(pathSeparator);
        if (levels.Length < patternLevels.Length)
        {
            return new RuleMatchResult { IsMatch = false };
        }

        // Build capture groups for each level
        var captureGroups = new Dictionary<string, string>();
        for (int i = 0; i < levels.Length; i++)
        {
            captureGroups[$"${{level{i}}}"] = levels[i].Trim();
        }

        // Also add numbered groups for compatibility
        for (int i = 0; i < levels.Length; i++)
        {
            captureGroups[$"${{{i}}}"] = levels[i].Trim();
        }

        // Apply template substitution
        var transformedValue = ApplyTemplateSubstitution(rule.TargetMapping, captureGroups);

        return new RuleMatchResult
        {
            IsMatch = true,
            TransformedValue = transformedValue,
            CaptureGroups = captureGroups
        };
    }

    /// <summary>
    /// T095: CONDITIONAL pattern matching - logic-based evaluation.
    /// Supports conditions like "CONTAINS", "STARTS_WITH", "ENDS_WITH", "MATCHES".
    /// </summary>
    private RuleMatchResult MatchConditional(TransformationRule rule, string groupDisplayName)
    {
        // Parse condition from metadata or source pattern
        var condition = rule.Metadata?.GetValueOrDefault("condition")?.ToString()
            ?? rule.SourcePattern;

        var isMatch = EvaluateCondition(condition, groupDisplayName);

        if (!isMatch)
        {
            return new RuleMatchResult { IsMatch = false };
        }

        // Apply fallback if defined
        var targetValue = rule.TargetMapping;
        if (rule.Metadata?.ContainsKey("fallback") == true && string.IsNullOrEmpty(targetValue))
        {
            targetValue = rule.Metadata["fallback"].ToString();
        }

        return new RuleMatchResult
        {
            IsMatch = true,
            TransformedValue = targetValue,
            CaptureGroups = new Dictionary<string, string>
            {
                { "${0}", groupDisplayName }
            }
        };
    }

    /// <summary>
    /// Evaluates a conditional expression against a group name.
    /// </summary>
    private bool EvaluateCondition(string condition, string groupDisplayName)
    {
        // Parse simple conditions: "CONTAINS 'Manager'", "STARTS_WITH 'Sales'", etc.
        var parts = condition.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            // Treat as regex pattern
            try
            {
                var regex = GetOrCompileRegex(condition);
                return regex.IsMatch(groupDisplayName);
            }
            catch
            {
                return false;
            }
        }

        var op = parts[0].ToUpperInvariant();
        var value = parts[1].Trim('\'', '"');

        return op switch
        {
            "CONTAINS" => groupDisplayName.Contains(value, StringComparison.OrdinalIgnoreCase),
            "STARTS_WITH" => groupDisplayName.StartsWith(value, StringComparison.OrdinalIgnoreCase),
            "ENDS_WITH" => groupDisplayName.EndsWith(value, StringComparison.OrdinalIgnoreCase),
            "EQUALS" => groupDisplayName.Equals(value, StringComparison.OrdinalIgnoreCase),
            "MATCHES" => GetOrCompileRegex(value).IsMatch(groupDisplayName),
            _ => false
        };
    }

    #endregion

    #region T096: Reverse Transformation

    /// <inheritdoc />
    public async Task<List<string>> TransformEntitlementToGroupsAsync(
        string tenantId,
        string providerId,
        string providerEntitlementId,
        string providerEntitlementType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEntitlementId);

        _logger.LogDebug(
            "Reverse transforming entitlement '{EntitlementId}' type '{Type}' for tenant '{TenantId}'",
            providerEntitlementId, providerEntitlementType, tenantId);

        var rules = await GetRulesAsync(tenantId, providerId, cancellationToken);
        var matchedGroups = new List<string>();

        foreach (var rule in rules.Where(r => r.Enabled))
        {
            var reversedGroup = TryReverseTransform(rule, providerEntitlementId);
            if (reversedGroup != null)
            {
                matchedGroups.Add(reversedGroup);
            }
        }

        return matchedGroups.Distinct().ToList();
    }

    /// <summary>
    /// Attempts to reverse-engineer the source group from an entitlement.
    /// </summary>
    private string? TryReverseTransform(TransformationRule rule, string entitlementName)
    {
        return rule.RuleType switch
        {
            RuleType.EXACT => ReverseExact(rule, entitlementName),
            RuleType.REGEX => ReverseRegex(rule, entitlementName),
            RuleType.HIERARCHICAL => ReverseHierarchical(rule, entitlementName),
            RuleType.CONDITIONAL => null, // Conditional rules cannot be easily reversed
            _ => null
        };
    }

    /// <summary>
    /// Reverse EXACT transformation - simple lookup.
    /// </summary>
    private static string? ReverseExact(TransformationRule rule, string entitlementName)
    {
        return rule.TargetMapping == entitlementName ? rule.SourcePattern : null;
    }

    /// <summary>
    /// Reverse REGEX transformation - extract values from target and reconstruct source.
    /// Example: "Sales_EMEA_Rep" with pattern "Sales_${1}_Rep" → extract "EMEA"
    ///          Then apply to source "^Sales-(.*)$" → "Sales-EMEA"
    /// </summary>
    private string? ReverseRegex(TransformationRule rule, string entitlementName)
    {
        try
        {
            // Convert target template to regex pattern
            // "Sales_${1}_Rep" → "Sales_(.*)_Rep"
            var targetPattern = ConvertTemplateToRegex(rule.TargetMapping);
            var targetRegex = GetOrCompileRegex(targetPattern);
            var match = targetRegex.Match(entitlementName);

            if (!match.Success)
            {
                return null;
            }

            // Extract capture groups from the entitlement
            var extractedGroups = new Dictionary<string, string>();
            for (int i = 1; i < match.Groups.Count; i++)
            {
                extractedGroups[$"${{{i}}}"] = match.Groups[i].Value;
            }

            // Try to reconstruct the source pattern
            // For simple patterns like "^Sales-(.*)$", we can reconstruct "Sales-{captured}"
            return ReconstructSourceFromPattern(rule.SourcePattern, extractedGroups);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to reverse regex for rule '{RuleId}'", rule.Id);
            return null;
        }
    }

    /// <summary>
    /// Converts a template like "Sales_${1}_Rep" to a regex pattern "Sales_(.*)_Rep".
    /// </summary>
    private static string ConvertTemplateToRegex(string template)
    {
        // Escape regex special chars except our placeholders
        var escaped = Regex.Escape(template);
        // Replace escaped placeholders with capture groups
        return Regex.Replace(escaped, @"\\\$\\\{(\d+)\\\}", "(.*)");
    }

    /// <summary>
    /// Reconstructs the source group name from a regex pattern and extracted values.
    /// </summary>
    private static string? ReconstructSourceFromPattern(string sourcePattern, Dictionary<string, string> capturedGroups)
    {
        // Remove regex anchors and escape sequences
        var simplified = sourcePattern
            .TrimStart('^')
            .TrimEnd('$')
            .Replace("\\", "");

        // Replace capture group patterns with extracted values
        // e.g., "Sales-(.*)" with captured "EMEA" → "Sales-EMEA"
        var result = Regex.Replace(simplified, @"\([^)]+\)", match =>
        {
            var groupIndex = 1; // Start from group 1
            foreach (var kvp in capturedGroups)
            {
                if (kvp.Key == $"${{{groupIndex}}}")
                {
                    return kvp.Value;
                }
                groupIndex++;
            }
            return match.Value;
        });

        // Verify the result is not still a pattern
        if (result.Contains(".*") || result.Contains("(."))
        {
            return null;
        }

        return result;
    }

    /// <summary>
    /// Reverse HIERARCHICAL transformation.
    /// </summary>
    private static string? ReverseHierarchical(TransformationRule rule, string entitlementName)
    {
        // Extract level from entitlement
        // e.g., "ORG-EMEA" with template "ORG-${level2}" → extract "EMEA" as level2
        var templatePattern = ConvertTemplateToRegex(rule.TargetMapping);
        var regex = new Regex(templatePattern);
        var match = regex.Match(entitlementName);

        if (!match.Success)
        {
            return null;
        }

        // For hierarchical, we need the full path which isn't easily recoverable
        // Return the extracted value as a hint
        if (match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    #endregion

    #region T097: Conflict Resolution

    /// <inheritdoc />
    public Task<List<Entitlement>> ResolveConflictsAsync(
        string tenantId,
        string providerId,
        string scimGroupDisplayName,
        List<Entitlement> conflictingEntitlements,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Resolving {Count} conflicts for group '{GroupName}' using strategy '{Strategy}'",
            conflictingEntitlements.Count, scimGroupDisplayName, strategy);

        List<Entitlement> resolved = strategy switch
        {
            ConflictResolutionStrategy.UNION => ResolveUnion(conflictingEntitlements),
            ConflictResolutionStrategy.FIRST_MATCH => ResolveFirstMatch(conflictingEntitlements),
            ConflictResolutionStrategy.HIGHEST_PRIVILEGE => ResolveHighestPrivilege(conflictingEntitlements),
            ConflictResolutionStrategy.MANUAL_REVIEW => ResolveManualReview(conflictingEntitlements, scimGroupDisplayName),
            ConflictResolutionStrategy.ERROR => throw new TransformationConflictException(
                $"Multiple rules matched group '{scimGroupDisplayName}' and conflict resolution is set to ERROR",
                scimGroupDisplayName,
                conflictingEntitlements.Select(e => e.Name).ToList()),
            _ => ResolveUnion(conflictingEntitlements)
        };

        return Task.FromResult(resolved);
    }

    /// <summary>
    /// UNION strategy: Return all matched entitlements.
    /// </summary>
    private static List<Entitlement> ResolveUnion(List<Entitlement> entitlements)
    {
        return entitlements.ToList();
    }

    /// <summary>
    /// FIRST_MATCH strategy: Return only the highest priority (lowest number) entitlement.
    /// </summary>
    private static List<Entitlement> ResolveFirstMatch(List<Entitlement> entitlements)
    {
        var first = entitlements.OrderBy(e => e.Priority).FirstOrDefault();
        return first != null ? [first] : [];
    }

    /// <summary>
    /// HIGHEST_PRIVILEGE strategy: Return the entitlement with highest privilege level.
    /// Privilege level should be stored in metadata["privilegeLevel"].
    /// </summary>
    private List<Entitlement> ResolveHighestPrivilege(List<Entitlement> entitlements)
    {
        var highest = entitlements
            .OrderByDescending(e =>
            {
                if (e.Metadata?.TryGetValue("privilegeLevel", out var level) == true)
                {
                    if (int.TryParse(level.ToString(), out var intLevel))
                    {
                        return intLevel;
                    }
                }
                return 0;
            })
            .FirstOrDefault();

        if (highest == null)
        {
            _logger.LogWarning("HIGHEST_PRIVILEGE resolution failed - no privilege levels defined");
            // Fall back to first match
            return ResolveFirstMatch(entitlements);
        }

        return [highest];
    }

    /// <summary>
    /// MANUAL_REVIEW strategy: Return empty list and log for admin review.
    /// </summary>
    private List<Entitlement> ResolveManualReview(List<Entitlement> entitlements, string groupName)
    {
        _logger.LogWarning(
            "Conflict requires manual review for group '{GroupName}'. Conflicting entitlements: {Entitlements}",
            groupName,
            string.Join(", ", entitlements.Select(e => e.Name)));

        // In a real implementation, this would create a conflict log entry
        // and notify admins. For now, return empty to prevent auto-assignment.
        return [];
    }

    #endregion

    #region T098: Rule Validation

    /// <inheritdoc />
    public Task<RuleValidationResult> ValidateRuleAsync(
        TransformationRule rule,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Required fields
        if (string.IsNullOrWhiteSpace(rule.TenantId))
            errors.Add("TenantId is required");

        if (string.IsNullOrWhiteSpace(rule.ProviderId))
            errors.Add("ProviderId is required");

        if (string.IsNullOrWhiteSpace(rule.SourcePattern))
            errors.Add("SourcePattern is required");

        if (string.IsNullOrWhiteSpace(rule.TargetMapping))
            errors.Add("TargetMapping is required");

        if (string.IsNullOrWhiteSpace(rule.TargetType))
            errors.Add("TargetType is required");

        // Pattern-specific validation
        switch (rule.RuleType)
        {
            case RuleType.REGEX:
                ValidateRegexPattern(rule.SourcePattern, errors, warnings);
                ValidateTemplateVariables(rule.SourcePattern, rule.TargetMapping, errors, warnings);
                break;

            case RuleType.HIERARCHICAL:
                ValidateHierarchicalPattern(rule.SourcePattern, rule.TargetMapping, errors, warnings);
                break;

            case RuleType.CONDITIONAL:
                ValidateConditionalPattern(rule.SourcePattern, rule.Metadata, errors, warnings);
                break;
        }

        // Priority validation
        if (rule.Priority < 1)
            warnings.Add("Priority should be 1 or greater (lower = higher priority)");

        // Examples validation
        if (rule.Examples.Count == 0)
            warnings.Add("No examples provided - consider adding test cases");

        var result = new RuleValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };

        return Task.FromResult(result);
    }

    /// <summary>
    /// Validates a regex pattern for syntax errors.
    /// </summary>
    private void ValidateRegexPattern(string pattern, List<string> errors, List<string> warnings)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            _regexCache.TryAdd(pattern, regex);
        }
        catch (RegexParseException ex)
        {
            errors.Add($"Invalid regex pattern: {ex.Message}");
        }

        // Check for common issues
        if (!pattern.StartsWith('^') || !pattern.EndsWith('$'))
            warnings.Add("Pattern should typically start with ^ and end with $ for exact matching");

        if (pattern.Contains(".*") && !pattern.Contains("("))
            warnings.Add("Consider using capture groups (.*)  to enable template substitution");
    }

    /// <summary>
    /// Validates that template variables in target match capture groups in source.
    /// </summary>
    private static void ValidateTemplateVariables(string sourcePattern, string targetMapping, List<string> errors, List<string> warnings)
    {
        // Count capture groups in source pattern
        var captureGroupCount = Regex.Matches(sourcePattern, @"\([^)]+\)").Count;

        // Find template variables in target
        var templateVars = Regex.Matches(targetMapping, @"\$\{(\d+)\}");

        foreach (Match match in templateVars)
        {
            if (int.TryParse(match.Groups[1].Value, out var index))
            {
                if (index > captureGroupCount)
                {
                    errors.Add($"Template variable ${{{index}}} references non-existent capture group (only {captureGroupCount} groups defined)");
                }
            }
        }
    }

    /// <summary>
    /// Validates a hierarchical pattern.
    /// </summary>
    private static void ValidateHierarchicalPattern(string sourcePattern, string targetMapping, List<string> errors, List<string> warnings)
    {
        var levels = sourcePattern.Split('/');

        if (levels.Length < 2)
            warnings.Add("Hierarchical pattern should contain at least 2 levels separated by /");

        // Check level references in target
        var levelRefs = Regex.Matches(targetMapping, @"\$\{level(\d+)\}");
        foreach (Match match in levelRefs)
        {
            if (int.TryParse(match.Groups[1].Value, out var level))
            {
                if (level >= levels.Length)
                {
                    warnings.Add($"Template references ${{level{level}}} but pattern only defines {levels.Length} levels (0-{levels.Length - 1})");
                }
            }
        }
    }

    /// <summary>
    /// Validates a conditional pattern.
    /// </summary>
    private static void ValidateConditionalPattern(string sourcePattern, Dictionary<string, object>? metadata, List<string> errors, List<string> warnings)
    {
        var validOperators = new[] { "CONTAINS", "STARTS_WITH", "ENDS_WITH", "EQUALS", "MATCHES" };

        var parts = sourcePattern.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            if (!validOperators.Contains(parts[0].ToUpperInvariant()))
            {
                warnings.Add($"Unknown condition operator '{parts[0]}'. Valid operators: {string.Join(", ", validOperators)}");
            }
        }
        else if (!sourcePattern.Contains(".*"))
        {
            // If not an operator format, should be a regex
            warnings.Add("Conditional pattern should be either 'OPERATOR value' or a regex pattern");
        }

        if (metadata?.ContainsKey("fallback") != true)
            warnings.Add("Consider adding a fallback value in metadata for when condition doesn't match");
    }

    #endregion

    #region Rule CRUD Operations

    /// <inheritdoc />
    public async Task<List<TransformationRule>> GetRulesAsync(
        string tenantId,
        string providerId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"rules:{tenantId}:{providerId}";

        if (_ruleCache.TryGetValue(cacheKey, out List<TransformationRule>? cachedRules) && cachedRules != null)
        {
            _logger.LogDebug("Returning cached rules for tenant '{TenantId}' provider '{ProviderId}'", tenantId, providerId);
            return cachedRules;
        }

        var rules = await _ruleStorage.GetRulesAsync(tenantId, providerId, cancellationToken);

        _ruleCache.Set(cacheKey, rules, RuleCacheExpiration);

        _logger.LogDebug("Cached {Count} rules for tenant '{TenantId}' provider '{ProviderId}'", rules.Count, tenantId, providerId);

        return rules;
    }

    /// <inheritdoc />
    public async Task<TransformationRule> CreateRuleAsync(
        string tenantId,
        TransformationRule rule,
        CancellationToken cancellationToken = default)
    {
        // Validate before creating
        var validation = await ValidateRuleAsync(rule, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ArgumentException($"Invalid rule: {string.Join(", ", validation.Errors)}");
        }

        rule.TenantId = tenantId;
        rule.Id = string.IsNullOrEmpty(rule.Id) ? Guid.NewGuid().ToString() : rule.Id;
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;

        var created = await _ruleStorage.CreateRuleAsync(rule, cancellationToken);

        // Invalidate cache
        InvalidateRuleCache(tenantId, rule.ProviderId);

        _logger.LogInformation("Created rule '{RuleId}' for tenant '{TenantId}'", created.Id, tenantId);

        return created;
    }

    /// <inheritdoc />
    public async Task<TransformationRule> UpdateRuleAsync(
        string tenantId,
        string ruleId,
        TransformationRule rule,
        CancellationToken cancellationToken = default)
    {
        // Validate before updating
        var validation = await ValidateRuleAsync(rule, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ArgumentException($"Invalid rule: {string.Join(", ", validation.Errors)}");
        }

        rule.TenantId = tenantId;
        rule.Id = ruleId;
        rule.UpdatedAt = DateTime.UtcNow;

        var updated = await _ruleStorage.UpdateRuleAsync(rule, cancellationToken);

        // Invalidate cache
        InvalidateRuleCache(tenantId, rule.ProviderId);

        _logger.LogInformation("Updated rule '{RuleId}' for tenant '{TenantId}'", ruleId, tenantId);

        return updated;
    }

    /// <inheritdoc />
    public async Task DeleteRuleAsync(
        string tenantId,
        string ruleId,
        CancellationToken cancellationToken = default)
    {
        var rule = await _ruleStorage.GetRuleByIdAsync(tenantId, ruleId, cancellationToken);

        await _ruleStorage.DeleteRuleAsync(tenantId, ruleId, cancellationToken);

        // Invalidate cache
        if (rule != null)
        {
            InvalidateRuleCache(tenantId, rule.ProviderId);
        }

        _logger.LogInformation("Deleted rule '{RuleId}' for tenant '{TenantId}'", ruleId, tenantId);
    }

    #endregion

    #region Test and Preview

    /// <inheritdoc />
    public Task<TransformationTestResult> TestRuleAsync(
        TransformationRule rule,
        List<string> testInputs,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TestCaseResult>();
        var stopwatch = new Stopwatch();

        foreach (var input in testInputs)
        {
            stopwatch.Restart();

            var matchResult = TryMatchRule(rule, input);

            stopwatch.Stop();

            // Find expected output from examples
            var example = rule.Examples.FirstOrDefault(e => e.Input == input);
            var expectedOutput = example?.ExpectedOutput;

            var passed = matchResult.IsMatch
                ? matchResult.TransformedValue == expectedOutput
                : expectedOutput == null;

            results.Add(new TestCaseResult
            {
                Input = input,
                ExpectedOutput = expectedOutput,
                ActualOutput = matchResult.TransformedValue,
                Passed = passed,
                ErrorMessage = passed ? null : $"Expected '{expectedOutput}' but got '{matchResult.TransformedValue}'",
                DurationMs = stopwatch.ElapsedMilliseconds
            });
        }

        return Task.FromResult(new TransformationTestResult
        {
            AllPassed = results.All(r => r.Passed),
            Results = results,
            ExecutedAt = DateTime.UtcNow,
            RuleId = rule.Id
        });
    }

    /// <inheritdoc />
    public async Task<TransformationPreviewResult> PreviewTransformationAsync(
        string tenantId,
        string providerId,
        string scimGroupDisplayName,
        CancellationToken cancellationToken = default)
    {
        var rules = await GetRulesAsync(tenantId, providerId, cancellationToken);
        var matchedEntitlements = new List<Entitlement>();
        var conflicts = new List<TransformationConflict>();
        string? matchedRuleId = null;

        foreach (var rule in rules.Where(r => r.Enabled).OrderBy(r => r.Priority))
        {
            var matchResult = TryMatchRule(rule, scimGroupDisplayName);
            if (matchResult.IsMatch)
            {
                matchedRuleId ??= rule.Id;
                var entitlement = CreateEntitlementFromMatch(rule, scimGroupDisplayName, matchResult);
                matchedEntitlements.Add(entitlement);
            }
        }

        // Detect conflicts
        if (matchedEntitlements.Count > 1)
        {
            conflicts.Add(new TransformationConflict
            {
                GroupName = scimGroupDisplayName,
                ConflictingRuleIds = matchedEntitlements.Select(e => e.SourceRuleId!).ToList(),
                ConflictingEntitlements = matchedEntitlements.Select(e => e.Name).ToList(),
                ResolutionStrategy = rules.FirstOrDefault()?.ConflictResolution ?? ConflictResolutionStrategy.UNION,
                Status = ConflictStatus.RESOLVED
            });
        }

        return new TransformationPreviewResult
        {
            InputGroupName = scimGroupDisplayName,
            MatchedRuleId = matchedRuleId,
            TransformedEntitlement = matchedEntitlements.FirstOrDefault(),
            AllEntitlements = matchedEntitlements,
            Conflicts = conflicts,
            AppliedAt = null, // Preview, not applied
            IsPreview = true
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets or compiles a regex pattern with caching.
    /// </summary>
    private static Regex GetOrCompileRegex(string pattern)
    {
        return _regexCache.GetOrAdd(pattern, p =>
            new Regex(p, RegexOptions.Compiled, TimeSpan.FromSeconds(5)));
    }

    /// <summary>
    /// Applies template variable substitution.
    /// </summary>
    private static string ApplyTemplateSubstitution(string template, Dictionary<string, string> captureGroups)
    {
        var result = template;
        foreach (var kvp in captureGroups)
        {
            result = result.Replace(kvp.Key, kvp.Value);
        }
        return result;
    }

    /// <summary>
    /// Creates an Entitlement from a successful rule match.
    /// </summary>
    private static Entitlement CreateEntitlementFromMatch(
        TransformationRule rule,
        string groupDisplayName,
        RuleMatchResult matchResult)
    {
        return new Entitlement
        {
            ProviderEntitlementId = matchResult.TransformedValue ?? rule.TargetMapping,
            Name = matchResult.TransformedValue ?? rule.TargetMapping,
            Type = ParseEntitlementType(rule.TargetType),
            MappedGroups = [groupDisplayName],
            Priority = rule.Priority,
            SourceRuleId = rule.Id,
            Metadata = rule.Metadata != null ? new Dictionary<string, object>(rule.Metadata) : null
        };
    }

    /// <summary>
    /// Parses entitlement type from string.
    /// </summary>
    private static EntitlementType ParseEntitlementType(string targetType)
    {
        return targetType.ToUpperInvariant() switch
        {
            "SALESFORCE_ROLE" or "ROLE" => EntitlementType.ROLE,
            "SALESFORCE_PERMISSION_SET" or "PERMISSION_SET" => EntitlementType.PERMISSION_SET,
            "SALESFORCE_PROFILE" or "PROFILE" => EntitlementType.PROFILE,
            "WORKDAY_ORG" or "ORG_UNIT" => EntitlementType.ORG_UNIT,
            "SERVICENOW_GROUP" or "GROUP" => EntitlementType.GROUP,
            "PERMISSION" => EntitlementType.PERMISSION,
            "LICENSE" => EntitlementType.LICENSE,
            _ => EntitlementType.CUSTOM
        };
    }

    /// <summary>
    /// Invalidates the rule cache for a tenant/provider.
    /// </summary>
    private void InvalidateRuleCache(string tenantId, string providerId)
    {
        var cacheKey = $"rules:{tenantId}:{providerId}";
        _ruleCache.Remove(cacheKey);
        _logger.LogDebug("Invalidated rule cache for tenant '{TenantId}' provider '{ProviderId}'", tenantId, providerId);
    }

    #endregion
}

/// <summary>
/// Result of attempting to match a rule against a group name.
/// </summary>
internal class RuleMatchResult
{
    public bool IsMatch { get; set; }
    public string? TransformedValue { get; set; }
    public Dictionary<string, string> CaptureGroups { get; set; } = new();
}

/// <summary>
/// Exception thrown when transformation conflicts cannot be resolved.
/// </summary>
public class TransformationConflictException : Exception
{
    public string GroupName { get; }
    public List<string> ConflictingEntitlements { get; }

    public TransformationConflictException(string message, string groupName, List<string> conflictingEntitlements)
        : base(message)
    {
        GroupName = groupName;
        ConflictingEntitlements = conflictingEntitlements;
    }
}
