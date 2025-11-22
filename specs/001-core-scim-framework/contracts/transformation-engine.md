# Transformation Engine Contract

**Version**: 1.0.0  
**Phase**: Phase 1 Design  
**Status**: Final Design  
**Date**: 2025-11-22

---

## 1. Overview

This document defines the contract for the Transformation Engine, which maps SCIM Groups to provider-specific entitlements (roles, permissions, organizational units) and handles conflict resolution.

**Purpose**: Provide a flexible, rule-based system to transform SCIM Group memberships into provider-specific access models.

**Key Capabilities**:
- Pattern matching (exact, regex, hierarchical, conditional)
- Bidirectional transformation (SCIM → Provider, Provider → SCIM)
- Conflict resolution strategies (union, first-match, highest-privilege, manual)
- Rule prioritization and composition
- Extensible pattern types

---

## 2. Core Interface

### 2.1 ITransformationEngine Interface

```csharp
public interface ITransformationEngine
{
    /// <summary>
    /// Transform SCIM Group to provider entitlements (forward transformation).
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Target provider ID (e.g., "salesforce-prod")</param>
    /// <param name="scimGroupDisplayName">SCIM Group displayName</param>
    /// <returns>List of mapped entitlements (roles, permissions, etc.)</returns>
    Task<List<Entitlement>> TransformGroupToEntitlementsAsync(
        string tenantId,
        string providerId,
        string scimGroupDisplayName
    );

    /// <summary>
    /// Reverse transform provider entitlement to SCIM Groups (reverse transformation).
    /// Used for drift detection and pull-sync.
    /// </summary>
    /// <param name="tenantId">Tenant ID for isolation</param>
    /// <param name="providerId">Source provider ID</param>
    /// <param name="providerEntitlementId">Provider-specific entitlement ID</param>
    /// <param name="providerEntitlementType">Type (role, permission, org, etc.)</param>
    /// <returns>List of matching SCIM Groups</returns>
    Task<List<string>> TransformEntitlementToGroupsAsync(
        string tenantId,
        string providerId,
        string providerEntitlementId,
        string providerEntitlementType
    );

    /// <summary>
    /// Get all transformation rules for a provider (ordered by priority).
    /// </summary>
    Task<List<TransformationRule>> GetRulesAsync(string tenantId, string providerId);

    /// <summary>
    /// Create a new transformation rule.
    /// </summary>
    Task<TransformationRule> CreateRuleAsync(string tenantId, TransformationRule rule);

    /// <summary>
    /// Update an existing transformation rule.
    /// </summary>
    Task<TransformationRule> UpdateRuleAsync(string tenantId, string ruleId, TransformationRule rule);

    /// <summary>
    /// Delete a transformation rule.
    /// </summary>
    Task DeleteRuleAsync(string tenantId, string ruleId);

    /// <summary>
    /// Test a transformation rule against example inputs (validation).
    /// </summary>
    Task<TransformationTestResult> TestRuleAsync(
        TransformationRule rule,
        List<string> testInputs
    );

    /// <summary>
    /// Resolve conflicts when multiple rules match the same group.
    /// </summary>
    Task<List<Entitlement>> ResolveConflictsAsync(
        string tenantId,
        string providerId,
        string scimGroupDisplayName,
        List<Entitlement> conflictingEntitlements,
        ConflictResolutionStrategy strategy
    );
}
```

---

## 3. Supporting Types

### 3.1 TransformationRule

```csharp
public class TransformationRule
{
    /// <summary>
    /// Unique rule ID (GUID)
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Tenant ID (for isolation)
    /// </summary>
    public string TenantId { get; set; }

    /// <summary>
    /// Provider ID (e.g., "salesforce-prod", "workday-prod")
    /// </summary>
    public string ProviderId { get; set; }

    /// <summary>
    /// Provider name (human-readable, e.g., "Salesforce")
    /// </summary>
    public string ProviderName { get; set; }

    /// <summary>
    /// Rule type (EXACT, REGEX, HIERARCHICAL, CONDITIONAL)
    /// </summary>
    public RuleType RuleType { get; set; }

    /// <summary>
    /// Source pattern (SCIM Group displayName pattern)
    /// </summary>
    public string SourcePattern { get; set; }

    /// <summary>
    /// Source type (always "SCIM_GROUP" for now)
    /// </summary>
    public string SourceType { get; set; }

    /// <summary>
    /// Target type (SALESFORCE_ROLE, WORKDAY_ORG, SERVICENOW_GROUP, etc.)
    /// </summary>
    public string TargetType { get; set; }

    /// <summary>
    /// Target mapping (provider-specific entitlement identifier or template)
    /// </summary>
    public string TargetMapping { get; set; }

    /// <summary>
    /// Rule priority (lower = higher priority, 1 is highest)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether rule is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Conflict resolution strategy if this rule conflicts with others
    /// </summary>
    public ConflictResolutionStrategy ConflictResolution { get; set; }

    /// <summary>
    /// Additional metadata (JSON object for custom config)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; }

    /// <summary>
    /// Example inputs/outputs for testing
    /// </summary>
    public List<TransformationExample> Examples { get; set; }

    /// <summary>
    /// Audit fields
    /// </summary>
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

### 3.2 RuleType Enum

```csharp
public enum RuleType
{
    /// <summary>
    /// Exact string match (e.g., "Sales Team" → "Sales_Representative")
    /// </summary>
    EXACT,

    /// <summary>
    /// Regex pattern match (e.g., "^Sales-(.*)$" → "Sales_${1}_Rep")
    /// </summary>
    REGEX,

    /// <summary>
    /// Hierarchical path mapping (e.g., "Company/Division/Department" → Workday org)
    /// </summary>
    HIERARCHICAL,

    /// <summary>
    /// Conditional logic (e.g., if group contains "Manager" → higher-privilege role)
    /// </summary>
    CONDITIONAL
}
```

### 3.3 Entitlement

```csharp
public class Entitlement
{
    /// <summary>
    /// Provider-specific entitlement ID (e.g., Salesforce role ID)
    /// </summary>
    public string ProviderEntitlementId { get; set; }

    /// <summary>
    /// Entitlement display name (e.g., "Sales_Representative")
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Entitlement type (ROLE, PERMISSION, ORG_UNIT, GROUP, etc.)
    /// </summary>
    public EntitlementType Type { get; set; }

    /// <summary>
    /// Source SCIM groups that mapped to this entitlement
    /// </summary>
    public List<string> MappedGroups { get; set; }

    /// <summary>
    /// Priority (from transformation rule)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Additional metadata (provider-specific attributes)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; }
}
```

### 3.4 EntitlementType Enum

```csharp
public enum EntitlementType
{
    ROLE,                  // Salesforce Role
    PERMISSION_SET,        // Salesforce Permission Set
    PROFILE,               // Salesforce Profile (immutable)
    ORG_UNIT,              // Workday Organization Unit
    GROUP,                 // ServiceNow Group (sys_user_group)
    PERMISSION,            // Generic permission
    CUSTOM                 // Provider-specific custom type
}
```

### 3.5 ConflictResolutionStrategy Enum

```csharp
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// Assign all conflicting entitlements (union)
    /// </summary>
    UNION,

    /// <summary>
    /// Assign only the first matching rule (by priority)
    /// </summary>
    FIRST_MATCH,

    /// <summary>
    /// Assign highest-privilege entitlement (requires privilege ranking)
    /// </summary>
    HIGHEST_PRIVILEGE,

    /// <summary>
    /// Require manual review/approval
    /// </summary>
    MANUAL_REVIEW,

    /// <summary>
    /// Fail with error (alert admin)
    /// </summary>
    ERROR
}
```

### 3.6 TransformationExample

```csharp
public class TransformationExample
{
    /// <summary>
    /// Example input (SCIM Group displayName)
    /// </summary>
    public string Input { get; set; }

    /// <summary>
    /// Expected output (entitlement name or ID)
    /// </summary>
    public string ExpectedOutput { get; set; }

    /// <summary>
    /// Whether this example passed last test
    /// </summary>
    public bool? Passed { get; set; }
}
```

### 3.7 TransformationTestResult

```csharp
public class TransformationTestResult
{
    /// <summary>
    /// Whether all test examples passed
    /// </summary>
    public bool AllPassed { get; set; }

    /// <summary>
    /// Test results per example
    /// </summary>
    public List<TestCaseResult> Results { get; set; }
}

public class TestCaseResult
{
    public string Input { get; set; }
    public string ExpectedOutput { get; set; }
    public string ActualOutput { get; set; }
    public bool Passed { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

## 4. Pattern Types & Examples

### 4.1 EXACT Pattern

**Description**: Exact string match (case-sensitive).

**Example Rule**:
```json
{
  "ruleType": "EXACT",
  "sourcePattern": "Sales Team",
  "targetMapping": "Sales_Representative",
  "targetType": "SALESFORCE_ROLE"
}
```

**Matching**:
- ✅ "Sales Team" → "Sales_Representative"
- ❌ "sales team" (case mismatch)
- ❌ "Sales Team EMEA" (extra text)

---

### 4.2 REGEX Pattern

**Description**: Regular expression pattern matching with capture groups.

**Example Rule**:
```json
{
  "ruleType": "REGEX",
  "sourcePattern": "^Sales-(.*)$",
  "targetMapping": "Sales_${1}_Rep",
  "targetType": "SALESFORCE_ROLE"
}
```

**Matching**:
- ✅ "Sales-EMEA" → "Sales_EMEA_Rep"
- ✅ "Sales-APAC" → "Sales_APAC_Rep"
- ❌ "Marketing-EMEA" (pattern mismatch)

**Template Variables**:
- `${1}`, `${2}`, etc. for capture groups
- `${0}` for entire match

---

### 4.3 HIERARCHICAL Pattern

**Description**: Hierarchical path mapping (for Workday-style organizational structures).

**Example Rule**:
```json
{
  "ruleType": "HIERARCHICAL",
  "sourcePattern": "Company/Division/Department/Team",
  "targetMapping": "ORG-${level3}",
  "targetType": "WORKDAY_ORG"
}
```

**Path Parsing**:
```
Input: "Acme Corp/Sales/EMEA/Field Sales"
Parsed:
  level0 = "Acme Corp"
  level1 = "Sales"
  level2 = "EMEA"
  level3 = "Field Sales"

Output: "ORG-Field Sales"
```

**Matching**:
- ✅ "Acme Corp/Sales/EMEA/Field Sales" → "ORG-Field Sales"
- ❌ "Acme Corp/Marketing" (insufficient levels)

---

### 4.4 CONDITIONAL Pattern

**Description**: Conditional logic based on group attributes or name patterns.

**Example Rule**:
```json
{
  "ruleType": "CONDITIONAL",
  "sourcePattern": ".*Manager.*",
  "targetMapping": "Sales_Manager",
  "targetType": "SALESFORCE_ROLE",
  "metadata": {
    "condition": "IF groupName CONTAINS 'Manager' THEN 'Sales_Manager' ELSE 'Sales_Representative'"
  }
}
```

**Matching**:
- ✅ "Sales Manager" → "Sales_Manager"
- ✅ "EMEA Manager" → "Sales_Manager"
- ❌ "Sales Team" → (no match, fallback to other rules)

---

## 5. Transformation Algorithm

### 5.1 Forward Transformation (SCIM → Provider)

**Input**: SCIM Group displayName = "Sales-EMEA"  
**Output**: List of provider entitlements

**Algorithm**:
```
1. Fetch all enabled transformation rules for providerId (ordered by priority ASC)
2. For each rule:
   a. Check if sourcePattern matches groupDisplayName
      - EXACT: Exact string comparison
      - REGEX: Regex.IsMatch(groupDisplayName, sourcePattern)
      - HIERARCHICAL: Parse path and match levels
      - CONDITIONAL: Evaluate condition
   b. If match:
      - Apply targetMapping template (replace variables)
      - Create Entitlement object
      - Add to matchedEntitlements list
3. If multiple entitlements matched:
   a. Check for conflicts (same targetType, different IDs)
   b. Apply conflict resolution strategy
4. Return final entitlements list
```

**Pseudocode**:
```csharp
public async Task<List<Entitlement>> TransformGroupToEntitlementsAsync(
    string tenantId, 
    string providerId, 
    string scimGroupDisplayName)
{
    var rules = await GetRulesAsync(tenantId, providerId);
    var matchedEntitlements = new List<Entitlement>();

    foreach (var rule in rules.Where(r => r.Enabled).OrderBy(r => r.Priority))
    {
        if (RuleMatches(rule, scimGroupDisplayName))
        {
            var entitlement = ApplyRuleMapping(rule, scimGroupDisplayName);
            matchedEntitlements.Add(entitlement);
        }
    }

    // Conflict resolution
    if (matchedEntitlements.Count > 1)
    {
        var conflictStrategy = rules.First().ConflictResolution;
        matchedEntitlements = await ResolveConflictsAsync(
            tenantId, 
            providerId, 
            scimGroupDisplayName, 
            matchedEntitlements, 
            conflictStrategy
        );
    }

    return matchedEntitlements;
}
```

---

### 5.2 Reverse Transformation (Provider → SCIM)

**Input**: Provider entitlement = "Sales_Representative"  
**Output**: List of SCIM Group displayNames

**Algorithm**:
```
1. Fetch all enabled transformation rules for providerId
2. For each rule:
   a. Check if targetMapping matches provider entitlement
      - For templates (e.g., "Sales_${1}_Rep"), reverse-engineer input
      - For exact values, direct match
   b. If match:
      - Reverse-apply sourcePattern to generate SCIM group name
      - Add to matchedGroups list
3. Return unique group names
```

**Example**:
```
Rule: sourcePattern="^Sales-(.*)$", targetMapping="Sales_${1}_Rep"
Provider Entitlement: "Sales_EMEA_Rep"

Reverse Engineering:
  1. Match "Sales_EMEA_Rep" against "Sales_${1}_Rep"
  2. Extract ${1} = "EMEA"
  3. Apply to sourcePattern: "Sales-EMEA"

Output: ["Sales-EMEA"]
```

---

## 6. Conflict Resolution

### 6.1 Conflict Scenarios

**Scenario 1: Multiple roles for same user**
- Group: "Sales-EMEA"
- Rule 1 (priority 1): "Sales-EMEA" → "Sales_Representative"
- Rule 2 (priority 2): ".*EMEA.*" → "EMEA_Regional_Manager"
- **Conflict**: User should have 2 roles?

**Scenario 2: Overlapping regex patterns**
- Group: "Sales Manager"
- Rule 1: "^Sales.*$" → "Sales_Representative"
- Rule 2: ".*Manager.*$" → "Sales_Manager"
- **Conflict**: Which role takes precedence?

### 6.2 Resolution Strategies

#### UNION (Default)
**Logic**: Assign all matched entitlements.

**Example**:
```
Input: "Sales Manager"
Matched: ["Sales_Representative", "Sales_Manager"]
Output: ["Sales_Representative", "Sales_Manager"]
```

**Use Case**: Provider supports multiple roles per user (Salesforce, ServiceNow).

---

#### FIRST_MATCH
**Logic**: Assign only the first matched entitlement (by priority).

**Example**:
```
Input: "Sales Manager"
Rule 1 (priority 1): "^Sales.*$" → "Sales_Representative"
Rule 2 (priority 2): ".*Manager.*$" → "Sales_Manager"
Output: ["Sales_Representative"]
```

**Use Case**: Provider restricts to single role (Workday).

---

#### HIGHEST_PRIVILEGE
**Logic**: Assign the highest-privilege entitlement (requires privilege ranking).

**Example**:
```
Input: "Sales Manager"
Matched: ["Sales_Representative" (privilege=1), "Sales_Manager" (privilege=3)]
Output: ["Sales_Manager"]
```

**Use Case**: Security-sensitive providers with privilege hierarchy.

**Privilege Ranking** (stored in metadata):
```json
{
  "Sales_Representative": {"privilegeLevel": 1},
  "Sales_Manager": {"privilegeLevel": 3},
  "VP_Sales": {"privilegeLevel": 5}
}
```

---

#### MANUAL_REVIEW
**Logic**: Flag conflict for admin review, do not auto-assign.

**Example**:
```
Input: "Sales Manager"
Matched: ["Sales_Representative", "Sales_Manager"]
Output: [] (no assignment)
Action: Create conflict log entry, notify admin
```

**Conflict Log**:
```json
{
  "conflictId": "conflict-001",
  "timestamp": "2025-11-22T10:00:00.000Z",
  "groupName": "Sales Manager",
  "conflictingEntitlements": ["Sales_Representative", "Sales_Manager"],
  "status": "PENDING_REVIEW"
}
```

---

#### ERROR
**Logic**: Fail operation with error, alert admin.

**Example**:
```
Input: "Sales Manager"
Matched: ["Sales_Representative", "Sales_Manager"]
Output: TransformationException("Multiple rules matched, conflict resolution=ERROR")
```

**Use Case**: Strict governance, no automatic conflict resolution allowed.

---

## 7. Rule Management

### 7.1 Create Rule

**HTTP API**: `POST /api/transformation-rules`

**Request Body**:
```json
{
  "tenantId": "tenant-123",
  "providerId": "salesforce-prod",
  "providerName": "Salesforce",
  "ruleType": "REGEX",
  "sourcePattern": "^Sales-(.*)$",
  "sourceType": "SCIM_GROUP",
  "targetType": "SALESFORCE_ROLE",
  "targetMapping": "Sales_${1}_Rep",
  "priority": 1,
  "enabled": true,
  "conflictResolution": "UNION",
  "examples": [
    {"input": "Sales-EMEA", "expectedOutput": "Sales_EMEA_Rep"},
    {"input": "Sales-APAC", "expectedOutput": "Sales_APAC_Rep"}
  ]
}
```

**Response** (201 Created):
```json
{
  "id": "rule-001",
  "tenantId": "tenant-123",
  "providerId": "salesforce-prod",
  "ruleType": "REGEX",
  "sourcePattern": "^Sales-(.*)$",
  "targetMapping": "Sales_${1}_Rep",
  "priority": 1,
  "enabled": true,
  "createdAt": "2025-11-22T10:00:00.000Z"
}
```

---

### 7.2 Test Rule

**HTTP API**: `POST /api/transformation-rules/test`

**Request Body**:
```json
{
  "rule": {
    "ruleType": "REGEX",
    "sourcePattern": "^Sales-(.*)$",
    "targetMapping": "Sales_${1}_Rep"
  },
  "testInputs": ["Sales-EMEA", "Sales-APAC", "Marketing-EMEA"]
}
```

**Response** (200 OK):
```json
{
  "allPassed": false,
  "results": [
    {
      "input": "Sales-EMEA",
      "expectedOutput": "Sales_EMEA_Rep",
      "actualOutput": "Sales_EMEA_Rep",
      "passed": true
    },
    {
      "input": "Sales-APAC",
      "expectedOutput": "Sales_APAC_Rep",
      "actualOutput": "Sales_APAC_Rep",
      "passed": true
    },
    {
      "input": "Marketing-EMEA",
      "expectedOutput": null,
      "actualOutput": null,
      "passed": true,
      "errorMessage": "No match (expected)"
    }
  ]
}
```

---

### 7.3 List Rules

**HTTP API**: `GET /api/transformation-rules?tenantId=tenant-123&providerId=salesforce-prod`

**Response** (200 OK):
```json
[
  {
    "id": "rule-001",
    "providerId": "salesforce-prod",
    "ruleType": "REGEX",
    "sourcePattern": "^Sales-(.*)$",
    "targetMapping": "Sales_${1}_Rep",
    "priority": 1,
    "enabled": true
  },
  {
    "id": "rule-002",
    "providerId": "salesforce-prod",
    "ruleType": "EXACT",
    "sourcePattern": "Marketing Team",
    "targetMapping": "Marketing_Manager",
    "priority": 2,
    "enabled": true
  }
]
```

---

## 8. Error Handling

### 8.1 TransformationException

```csharp
public class TransformationException : Exception
{
    public string RuleId { get; set; }
    public string GroupName { get; set; }
    public TransformationErrorType ErrorType { get; set; }

    public TransformationException(
        string message, 
        TransformationErrorType errorType
    ) : base(message)
    {
        ErrorType = errorType;
    }
}

public enum TransformationErrorType
{
    RULE_NOT_FOUND,
    PATTERN_MATCH_FAILED,
    CONFLICT_RESOLUTION_FAILED,
    INVALID_REGEX,
    CIRCULAR_REFERENCE,
    PROVIDER_API_ERROR
}
```

### 8.2 Error Scenarios

**Error 1: No matching rules**
```
Input: "Unknown Group"
Rules: (none match)
Result: Return empty list [] (not an error, user has no entitlements)
```

**Error 2: Invalid regex pattern**
```
Rule: sourcePattern="^Sales-[" (invalid regex)
Result: Throw TransformationException(INVALID_REGEX)
```

**Error 3: Conflict resolution failure**
```
Strategy: HIGHEST_PRIVILEGE
Matched: ["Role_A" (no privilege), "Role_B" (no privilege)]
Result: Throw TransformationException(CONFLICT_RESOLUTION_FAILED)
```

---

## 9. Performance Considerations

### 9.1 Rule Caching

**Problem**: Fetching rules from Cosmos DB on every transformation adds latency.

**Solution**: Cache rules in-memory (refresh every 5 minutes).

```csharp
private static readonly MemoryCache _ruleCache = new MemoryCache(new MemoryCacheOptions());

public async Task<List<TransformationRule>> GetRulesAsync(string tenantId, string providerId)
{
    var cacheKey = $"rules:{tenantId}:{providerId}";
    
    if (_ruleCache.TryGetValue(cacheKey, out List<TransformationRule> cachedRules))
    {
        return cachedRules;
    }

    var rules = await _cosmosService.GetRulesAsync(tenantId, providerId);
    
    _ruleCache.Set(cacheKey, rules, TimeSpan.FromMinutes(5));
    
    return rules;
}
```

### 9.2 Regex Compilation

**Problem**: Compiling regex patterns on every match is expensive.

**Solution**: Pre-compile regex patterns and cache.

```csharp
private static readonly Dictionary<string, Regex> _regexCache = new();

private bool RegexMatches(string pattern, string input)
{
    if (!_regexCache.TryGetValue(pattern, out var regex))
    {
        regex = new Regex(pattern, RegexOptions.Compiled);
        _regexCache[pattern] = regex;
    }

    return regex.IsMatch(input);
}
```

---

## 10. Testing Strategy

### 10.1 Unit Tests

**Test 1: EXACT pattern matching**
```csharp
[Fact]
public async Task Transform_ExactMatch_ReturnsCorrectEntitlement()
{
    var rule = new TransformationRule
    {
        RuleType = RuleType.EXACT,
        SourcePattern = "Sales Team",
        TargetMapping = "Sales_Representative"
    };

    var result = await _engine.TransformGroupToEntitlementsAsync(
        "tenant-123", 
        "salesforce-prod", 
        "Sales Team"
    );

    Assert.Single(result);
    Assert.Equal("Sales_Representative", result[0].Name);
}
```

**Test 2: REGEX pattern with capture groups**
```csharp
[Fact]
public async Task Transform_RegexWithCaptureGroup_ReturnsCorrectEntitlement()
{
    var rule = new TransformationRule
    {
        RuleType = RuleType.REGEX,
        SourcePattern = "^Sales-(.*)$",
        TargetMapping = "Sales_${1}_Rep"
    };

    var result = await _engine.TransformGroupToEntitlementsAsync(
        "tenant-123", 
        "salesforce-prod", 
        "Sales-EMEA"
    );

    Assert.Single(result);
    Assert.Equal("Sales_EMEA_Rep", result[0].Name);
}
```

### 10.2 Integration Tests

**Test: End-to-end transformation with Salesforce adapter**
```csharp
[Fact]
public async Task Transform_EndToEnd_AssignsRoleInSalesforce()
{
    // Create transformation rule
    var rule = await _engine.CreateRuleAsync("tenant-123", new TransformationRule
    {
        ProviderId = "salesforce-prod",
        RuleType = RuleType.EXACT,
        SourcePattern = "Sales Team",
        TargetMapping = "Sales_Representative"
    });

    // Create SCIM group
    var group = await _scimService.CreateGroupAsync(new Group
    {
        DisplayName = "Sales Team"
    });

    // Add user to group
    await _scimService.AddUserToGroupAsync(group.Id, "user-123");

    // Verify Salesforce role assignment
    var salesforceUser = await _salesforceAdapter.GetUserAsync("user-123");
    Assert.Contains("Sales_Representative", salesforceUser.Roles);
}
```

---

**Version**: 1.0.0 | **Status**: Final Design | **Date**: 2025-11-22
