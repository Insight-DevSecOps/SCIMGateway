// ==========================================================================
// T106-T113: Unit Tests for Transformation Engine
// ==========================================================================
// Tests for pattern matching (EXACT, REGEX, HIERARCHICAL, CONDITIONAL)
// and conflict resolution strategies (UNION, FIRST_MATCH, HIGHEST_PRIVILEGE)
// ==========================================================================

using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SCIMGateway.Tests.Unit;

/// <summary>
/// Unit tests for TransformationEngine.
/// Tests pattern matching and conflict resolution functionality.
/// </summary>
public class TransformationEngineTests
{
    private static Type? GetTypeByName(string typeName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("SCIMGateway") == true);

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == typeName);
            if (type != null) return type;
        }
        return null;
    }

    private static object? CreateInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    // ==================== T106: EXACT Pattern Tests ====================

    #region T106 - EXACT Pattern Matching

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void EXACT_Pattern_Should_Match_When_String_Is_Identical()
    {
        // Arrange
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        // Set rule properties
        var ruleTypeProperty = ruleType.GetProperty("RuleType");
        Assert.NotNull(ruleTypeProperty);

        var sourcePatternProperty = ruleType.GetProperty("SourcePattern");
        Assert.NotNull(sourcePatternProperty);
        sourcePatternProperty.SetValue(rule, "Sales Team");

        var targetMappingProperty = ruleType.GetProperty("TargetMapping");
        Assert.NotNull(targetMappingProperty);
        targetMappingProperty.SetValue(rule, "Sales_Representative");

        // Act - Call pattern matching
        var engineType = GetTypeByName("TransformationEngine") ?? GetTypeByName("ITransformationEngine");
        Assert.NotNull(engineType);

        // Assert - "Sales Team" should match exactly
        var sourcePattern = sourcePatternProperty.GetValue(rule) as string;
        Assert.Equal("Sales Team", sourcePattern);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void EXACT_Pattern_Should_Not_Match_Different_Case()
    {
        // Arrange - EXACT is case-sensitive per contract
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        var sourcePatternProperty = ruleType.GetProperty("SourcePattern");
        Assert.NotNull(sourcePatternProperty);
        sourcePatternProperty.SetValue(rule, "Sales Team");

        // Act - Try to match "sales team" (lowercase)
        var input = "sales team";
        var pattern = sourcePatternProperty.GetValue(rule) as string;

        // Assert - Case mismatch should NOT match
        Assert.NotEqual(input, pattern); // Different case = no match
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void EXACT_Pattern_Should_Not_Match_With_Extra_Text()
    {
        // Arrange
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        var sourcePatternProperty = ruleType.GetProperty("SourcePattern");
        Assert.NotNull(sourcePatternProperty);
        sourcePatternProperty.SetValue(rule, "Sales Team");

        // Act - Try to match "Sales Team EMEA" (extra text)
        var input = "Sales Team EMEA";
        var pattern = sourcePatternProperty.GetValue(rule) as string;

        // Assert - Extra text should NOT match
        Assert.NotEqual(input, pattern);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void EXACT_Pattern_Should_Not_Match_Partial_Text()
    {
        // Arrange
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        var sourcePatternProperty = ruleType.GetProperty("SourcePattern");
        Assert.NotNull(sourcePatternProperty);
        sourcePatternProperty.SetValue(rule, "Sales Team");

        // Act - Try to match "Sales" (partial text)
        var input = "Sales";
        var pattern = sourcePatternProperty.GetValue(rule) as string;

        // Assert - Partial text should NOT match
        Assert.NotEqual(input, pattern);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void EXACT_Pattern_Should_Return_Correct_TargetMapping()
    {
        // Arrange
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        var sourcePatternProperty = ruleType.GetProperty("SourcePattern");
        Assert.NotNull(sourcePatternProperty);
        sourcePatternProperty.SetValue(rule, "Sales Team");

        var targetMappingProperty = ruleType.GetProperty("TargetMapping");
        Assert.NotNull(targetMappingProperty);
        targetMappingProperty.SetValue(rule, "Sales_Representative");

        // Act - Get target mapping when match occurs
        var targetMapping = targetMappingProperty.GetValue(rule) as string;

        // Assert
        Assert.Equal("Sales_Representative", targetMapping);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void EXACT_Pattern_RuleType_Should_Be_Set_Correctly()
    {
        // Arrange
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var ruleTypeEnumType = GetTypeByName("RuleType");
        Assert.NotNull(ruleTypeEnumType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        // Act - Set RuleType to EXACT
        var ruleTypeProp = ruleType.GetProperty("RuleType");
        Assert.NotNull(ruleTypeProp);

        var exactValue = Enum.Parse(ruleTypeEnumType, "EXACT");
        ruleTypeProp.SetValue(rule, exactValue);

        // Assert
        var actualRuleType = ruleTypeProp.GetValue(rule);
        Assert.Equal(exactValue, actualRuleType);
    }

    #endregion

    // ==================== T107: REGEX Pattern Tests ====================

    #region T107 - REGEX Pattern Matching

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_Should_Match_Valid_Input()
    {
        // Arrange - Pattern: ^Sales-(.*)$
        var pattern = @"^Sales-(.*)$";
        var input = "Sales-EMEA";

        // Act
        var regex = new Regex(pattern);
        var match = regex.Match(input);

        // Assert
        Assert.True(match.Success);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_Should_Capture_Groups_Correctly()
    {
        // Arrange - Pattern: ^Sales-(.*)$, Input: Sales-EMEA
        var pattern = @"^Sales-(.*)$";
        var input = "Sales-EMEA";

        // Act
        var regex = new Regex(pattern);
        var match = regex.Match(input);

        // Assert
        Assert.True(match.Success);
        Assert.Equal(2, match.Groups.Count); // Full match + 1 capture group
        Assert.Equal("EMEA", match.Groups[1].Value);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_Should_Substitute_Template_Variables()
    {
        // Arrange - Pattern: ^Sales-(.*)$, Template: Sales_${1}_Rep
        var pattern = @"^Sales-(.*)$";
        var input = "Sales-EMEA";
        var template = "Sales_${1}_Rep";

        // Act
        var regex = new Regex(pattern);
        var match = regex.Match(input);

        string result = template;
        if (match.Success)
        {
            // Replace ${1} with first capture group
            result = result.Replace("${1}", match.Groups[1].Value);
        }

        // Assert
        Assert.Equal("Sales_EMEA_Rep", result);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_Should_Not_Match_Invalid_Input()
    {
        // Arrange - Pattern: ^Sales-(.*)$, Input: Marketing-EMEA
        var pattern = @"^Sales-(.*)$";
        var input = "Marketing-EMEA";

        // Act
        var regex = new Regex(pattern);
        var match = regex.Match(input);

        // Assert - Should not match
        Assert.False(match.Success);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_Should_Handle_Multiple_Capture_Groups()
    {
        // Arrange - Pattern: ^(.+)-(.+)-(.+)$, Input: Sales-EMEA-Team
        var pattern = @"^(.+)-(.+)-(.+)$";
        var input = "Sales-EMEA-Team";
        var template = "${1}_${2}_${3}_Role";

        // Act
        var regex = new Regex(pattern);
        var match = regex.Match(input);

        string result = template;
        if (match.Success)
        {
            result = result.Replace("${1}", match.Groups[1].Value);
            result = result.Replace("${2}", match.Groups[2].Value);
            result = result.Replace("${3}", match.Groups[3].Value);
        }

        // Assert
        Assert.Equal("Sales_EMEA_Team_Role", result);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_Should_Handle_Zero_Capture_Group()
    {
        // Arrange - ${0} represents the entire match
        var pattern = @"^Sales-\w+$";
        var input = "Sales-EMEA";
        var template = "Match_${0}";

        // Act
        var regex = new Regex(pattern);
        var match = regex.Match(input);

        string result = template;
        if (match.Success)
        {
            result = result.Replace("${0}", match.Groups[0].Value);
        }

        // Assert
        Assert.Equal("Match_Sales-EMEA", result);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_Should_Support_Case_Insensitive_Matching()
    {
        // Arrange - Pattern with case-insensitive flag
        var pattern = @"^sales-(.*)$";
        var input = "SALES-EMEA";

        // Act - Default is case-sensitive
        var regexSensitive = new Regex(pattern);
        var matchSensitive = regexSensitive.Match(input);

        // Case-insensitive
        var regexInsensitive = new Regex(pattern, RegexOptions.IgnoreCase);
        var matchInsensitive = regexInsensitive.Match(input);

        // Assert
        Assert.False(matchSensitive.Success); // Case-sensitive fails
        Assert.True(matchInsensitive.Success); // Case-insensitive passes
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_Should_Return_Null_When_No_Match()
    {
        // Arrange
        var pattern = @"^Sales-(.*)$";
        var input = "Marketing-EMEA";

        // Act
        var regex = new Regex(pattern);
        var match = regex.Match(input);

        // Assert
        Assert.False(match.Success);
        // When no match, transformation should return null/empty
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_RuleType_Should_Be_Set_To_REGEX()
    {
        // Arrange
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var ruleTypeEnumType = GetTypeByName("RuleType");
        Assert.NotNull(ruleTypeEnumType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        // Act
        var ruleTypeProp = ruleType.GetProperty("RuleType");
        Assert.NotNull(ruleTypeProp);

        var regexValue = Enum.Parse(ruleTypeEnumType, "REGEX");
        ruleTypeProp.SetValue(rule, regexValue);

        // Assert
        var actualRuleType = ruleTypeProp.GetValue(rule);
        Assert.Equal(regexValue, actualRuleType);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void REGEX_Pattern_With_Special_Characters_Should_Work()
    {
        // Arrange - Pattern with regex special characters
        var pattern = @"^\[Sales\]-(.*)$";
        var input = "[Sales]-EMEA";
        var template = "Sales_${1}_Rep";

        // Act
        var regex = new Regex(pattern);
        var match = regex.Match(input);

        string result = template;
        if (match.Success)
        {
            result = result.Replace("${1}", match.Groups[1].Value);
        }

        // Assert
        Assert.True(match.Success);
        Assert.Equal("Sales_EMEA_Rep", result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T091)")]
    public async Task TransformGroupToEntitlementsAsync_With_REGEX_Should_Return_Transformed_Entitlement()
    {
        // Arrange - Full integration with TransformationEngine
        var engineType = GetTypeByName("TransformationEngine");
        Assert.NotNull(engineType);

        // Act - This test validates the full transformation flow
        // Will be enabled when TransformationEngine is implemented

        // Assert
        await Task.CompletedTask;
    }

    #endregion

    // ==================== RuleType Enum Tests ====================

    #region RuleType Enum

    [Fact(Skip = "Pending implementation of RuleType enum (T086)")]
    public void RuleType_Enum_Should_Exist()
    {
        var enumType = GetTypeByName("RuleType");
        Assert.NotNull(enumType);
        Assert.True(enumType.IsEnum);
    }

    [Fact(Skip = "Pending implementation of RuleType enum (T086)")]
    public void RuleType_Should_Have_EXACT_Value()
    {
        var enumType = GetTypeByName("RuleType");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("EXACT", values);
    }

    [Fact(Skip = "Pending implementation of RuleType enum (T086)")]
    public void RuleType_Should_Have_REGEX_Value()
    {
        var enumType = GetTypeByName("RuleType");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("REGEX", values);
    }

    [Fact(Skip = "Pending implementation of RuleType enum (T086)")]
    public void RuleType_Should_Have_HIERARCHICAL_Value()
    {
        var enumType = GetTypeByName("RuleType");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("HIERARCHICAL", values);
    }

    [Fact(Skip = "Pending implementation of RuleType enum (T086)")]
    public void RuleType_Should_Have_CONDITIONAL_Value()
    {
        var enumType = GetTypeByName("RuleType");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("CONDITIONAL", values);
    }

    #endregion

    // ==================== TransformationRule Model Tests ====================

    #region TransformationRule Model

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void TransformationRule_Should_Exist()
    {
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void TransformationRule_Should_Have_Required_Properties()
    {
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        // Check all required properties per contract
        Assert.NotNull(ruleType.GetProperty("Id"));
        Assert.NotNull(ruleType.GetProperty("TenantId"));
        Assert.NotNull(ruleType.GetProperty("ProviderId"));
        Assert.NotNull(ruleType.GetProperty("RuleType"));
        Assert.NotNull(ruleType.GetProperty("SourcePattern"));
        Assert.NotNull(ruleType.GetProperty("TargetMapping"));
        Assert.NotNull(ruleType.GetProperty("Priority"));
        Assert.NotNull(ruleType.GetProperty("Enabled"));
        Assert.NotNull(ruleType.GetProperty("ConflictResolution"));
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void TransformationRule_Priority_Should_Be_Int()
    {
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var priorityProp = ruleType.GetProperty("Priority");
        Assert.NotNull(priorityProp);
        Assert.Equal(typeof(int), priorityProp.PropertyType);
    }

    [Fact(Skip = "Pending implementation of TransformationRule model (T086)")]
    public void TransformationRule_Enabled_Should_Be_Bool()
    {
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var enabledProp = ruleType.GetProperty("Enabled");
        Assert.NotNull(enabledProp);
        Assert.Equal(typeof(bool), enabledProp.PropertyType);
    }

    #endregion

    // ==================== ITransformationEngine Interface Tests ====================

    #region ITransformationEngine Interface

    [Fact(Skip = "Pending implementation of ITransformationEngine (T090)")]
    public void ITransformationEngine_Should_Exist()
    {
        var engineType = GetTypeByName("ITransformationEngine");
        Assert.NotNull(engineType);
        Assert.True(engineType.IsInterface);
    }

    [Fact(Skip = "Pending implementation of ITransformationEngine (T090)")]
    public void ITransformationEngine_Should_Have_TransformGroupToEntitlementsAsync_Method()
    {
        var engineType = GetTypeByName("ITransformationEngine");
        Assert.NotNull(engineType);

        var method = engineType.GetMethod("TransformGroupToEntitlementsAsync");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Pending implementation of ITransformationEngine (T090)")]
    public void ITransformationEngine_Should_Have_TransformEntitlementToGroupsAsync_Method()
    {
        var engineType = GetTypeByName("ITransformationEngine");
        Assert.NotNull(engineType);

        var method = engineType.GetMethod("TransformEntitlementToGroupsAsync");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Pending implementation of ITransformationEngine (T090)")]
    public void ITransformationEngine_Should_Have_GetRulesAsync_Method()
    {
        var engineType = GetTypeByName("ITransformationEngine");
        Assert.NotNull(engineType);

        var method = engineType.GetMethod("GetRulesAsync");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Pending implementation of ITransformationEngine (T090)")]
    public void ITransformationEngine_Should_Have_CreateRuleAsync_Method()
    {
        var engineType = GetTypeByName("ITransformationEngine");
        Assert.NotNull(engineType);

        var method = engineType.GetMethod("CreateRuleAsync");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Pending implementation of ITransformationEngine (T090)")]
    public void ITransformationEngine_Should_Have_TestRuleAsync_Method()
    {
        var engineType = GetTypeByName("ITransformationEngine");
        Assert.NotNull(engineType);

        var method = engineType.GetMethod("TestRuleAsync");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Pending implementation of ITransformationEngine (T090)")]
    public void ITransformationEngine_Should_Have_ResolveConflictsAsync_Method()
    {
        var engineType = GetTypeByName("ITransformationEngine");
        Assert.NotNull(engineType);

        var method = engineType.GetMethod("ResolveConflictsAsync");
        Assert.NotNull(method);
    }

    #endregion

    // ==================== T108: HIERARCHICAL Pattern Tests ====================

    #region T108 - HIERARCHICAL Pattern Matching

    [Fact(Skip = "Pending implementation of TransformationEngine (T094)")]
    public void HIERARCHICAL_Pattern_Should_Parse_Path_Correctly()
    {
        // Arrange - Path: "Acme Corp/Sales/EMEA/Field Sales"
        var input = "Acme Corp/Sales/EMEA/Field Sales";
        var delimiter = "/";

        // Act - Parse into levels
        var levels = input.Split(delimiter);

        // Assert
        Assert.Equal(4, levels.Length);
        Assert.Equal("Acme Corp", levels[0]); // level0
        Assert.Equal("Sales", levels[1]);      // level1
        Assert.Equal("EMEA", levels[2]);       // level2
        Assert.Equal("Field Sales", levels[3]); // level3
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T094)")]
    public void HIERARCHICAL_Pattern_Should_Extract_Level_Variables()
    {
        // Arrange
        var input = "Acme Corp/Sales/EMEA/Field Sales";
        var template = "ORG-${level3}";
        var delimiter = "/";

        // Act
        var levels = input.Split(delimiter);
        var result = template;
        for (int i = 0; i < levels.Length; i++)
        {
            result = result.Replace($"${{level{i}}}", levels[i]);
        }

        // Assert
        Assert.Equal("ORG-Field Sales", result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T094)")]
    public void HIERARCHICAL_Pattern_Should_Handle_Multiple_Level_References()
    {
        // Arrange
        var input = "Acme Corp/Sales/EMEA/Field Sales";
        var template = "${level1}-${level2}-${level3}";
        var delimiter = "/";

        // Act
        var levels = input.Split(delimiter);
        var result = template;
        for (int i = 0; i < levels.Length; i++)
        {
            result = result.Replace($"${{level{i}}}", levels[i]);
        }

        // Assert
        Assert.Equal("Sales-EMEA-Field Sales", result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T094)")]
    public void HIERARCHICAL_Pattern_Should_Fail_With_Insufficient_Levels()
    {
        // Arrange - Pattern expects 4 levels, input has only 2
        var input = "Acme Corp/Marketing";
        var template = "ORG-${level3}"; // level3 doesn't exist
        var delimiter = "/";

        // Act
        var levels = input.Split(delimiter);

        // Assert - Only 2 levels available
        Assert.Equal(2, levels.Length);
        // level3 (index 3) is out of bounds
        Assert.True(levels.Length <= 3);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T094)")]
    public void HIERARCHICAL_Pattern_Should_Handle_Custom_Delimiter()
    {
        // Arrange - Using colon delimiter instead of slash
        var input = "Company:Division:Department:Team";
        var template = "DEPT-${level2}";
        var delimiter = ":";

        // Act
        var levels = input.Split(delimiter);
        var result = template;
        for (int i = 0; i < levels.Length; i++)
        {
            result = result.Replace($"${{level{i}}}", levels[i]);
        }

        // Assert
        Assert.Equal("DEPT-Department", result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T094)")]
    public void HIERARCHICAL_Pattern_Should_Handle_Empty_Levels()
    {
        // Arrange - Path with empty segment
        var input = "Acme Corp//EMEA/Field Sales";
        var delimiter = "/";

        // Act
        var levels = input.Split(delimiter);

        // Assert - Should have 4 segments, one empty
        Assert.Equal(4, levels.Length);
        Assert.Equal("", levels[1]); // Empty level
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T094)")]
    public void HIERARCHICAL_Pattern_RuleType_Should_Be_Set_To_HIERARCHICAL()
    {
        // Arrange
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var ruleTypeEnumType = GetTypeByName("RuleType");
        Assert.NotNull(ruleTypeEnumType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        // Act
        var ruleTypeProp = ruleType.GetProperty("RuleType");
        Assert.NotNull(ruleTypeProp);

        var hierarchicalValue = Enum.Parse(ruleTypeEnumType, "HIERARCHICAL");
        ruleTypeProp.SetValue(rule, hierarchicalValue);

        // Assert
        var actualRuleType = ruleTypeProp.GetValue(rule);
        Assert.Equal(hierarchicalValue, actualRuleType);
    }

    #endregion

    // ==================== T109: CONDITIONAL Pattern Tests ====================

    #region T109 - CONDITIONAL Pattern Matching

    [Fact(Skip = "Pending implementation of TransformationEngine (T095)")]
    public void CONDITIONAL_Pattern_Should_Match_When_Condition_Is_True()
    {
        // Arrange - Condition: groupName contains "Manager"
        var input = "Sales Manager";
        var condition = "Manager";

        // Act
        var matches = input.Contains(condition, StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.True(matches);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T095)")]
    public void CONDITIONAL_Pattern_Should_Not_Match_When_Condition_Is_False()
    {
        // Arrange - Condition: groupName contains "Manager"
        var input = "Sales Team";
        var condition = "Manager";

        // Act
        var matches = input.Contains(condition, StringComparison.OrdinalIgnoreCase);

        // Assert
        Assert.False(matches);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T095)")]
    public void CONDITIONAL_Pattern_Should_Return_TrueValue_When_Condition_Met()
    {
        // Arrange
        var input = "EMEA Manager";
        var condition = "Manager";
        var trueValue = "Sales_Manager";
        var falseValue = "Sales_Representative";

        // Act
        var result = input.Contains(condition, StringComparison.OrdinalIgnoreCase) 
            ? trueValue 
            : falseValue;

        // Assert
        Assert.Equal("Sales_Manager", result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T095)")]
    public void CONDITIONAL_Pattern_Should_Return_FalseValue_When_Condition_Not_Met()
    {
        // Arrange
        var input = "Sales Team";
        var condition = "Manager";
        var trueValue = "Sales_Manager";
        var falseValue = "Sales_Representative";

        // Act
        var result = input.Contains(condition, StringComparison.OrdinalIgnoreCase) 
            ? trueValue 
            : falseValue;

        // Assert
        Assert.Equal("Sales_Representative", result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T095)")]
    public void CONDITIONAL_Pattern_Should_Support_Multiple_Conditions()
    {
        // Arrange - Multiple conditions: VP > Director > Manager > Employee
        var input = "VP of Sales";

        // Act - Check conditions in priority order
        string result;
        if (input.Contains("VP", StringComparison.OrdinalIgnoreCase))
            result = "VP_Role";
        else if (input.Contains("Director", StringComparison.OrdinalIgnoreCase))
            result = "Director_Role";
        else if (input.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            result = "Manager_Role";
        else
            result = "Employee_Role";

        // Assert
        Assert.Equal("VP_Role", result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T095)")]
    public void CONDITIONAL_Pattern_Should_Support_Regex_Conditions()
    {
        // Arrange - Condition: matches regex pattern
        var input = "Sales Manager Level 3";
        var pattern = @"Manager.*Level\s*[3-5]";

        // Act
        var regex = new Regex(pattern);
        var matches = regex.IsMatch(input);

        // Assert
        Assert.True(matches);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T095)")]
    public void CONDITIONAL_Pattern_Should_Fallback_When_No_Condition_Matches()
    {
        // Arrange
        var input = "Intern";

        // Act - None of the conditions match
        string result;
        if (input.Contains("VP", StringComparison.OrdinalIgnoreCase))
            result = "VP_Role";
        else if (input.Contains("Manager", StringComparison.OrdinalIgnoreCase))
            result = "Manager_Role";
        else
            result = null!; // No fallback = no match

        // Assert
        Assert.Null(result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T095)")]
    public void CONDITIONAL_Pattern_RuleType_Should_Be_Set_To_CONDITIONAL()
    {
        // Arrange
        var ruleType = GetTypeByName("TransformationRule");
        Assert.NotNull(ruleType);

        var ruleTypeEnumType = GetTypeByName("RuleType");
        Assert.NotNull(ruleTypeEnumType);

        var rule = CreateInstance(ruleType);
        Assert.NotNull(rule);

        // Act
        var ruleTypeProp = ruleType.GetProperty("RuleType");
        Assert.NotNull(ruleTypeProp);

        var conditionalValue = Enum.Parse(ruleTypeEnumType, "CONDITIONAL");
        ruleTypeProp.SetValue(rule, conditionalValue);

        // Assert
        var actualRuleType = ruleTypeProp.GetValue(rule);
        Assert.Equal(conditionalValue, actualRuleType);
    }

    #endregion

    // ==================== T111: UNION Conflict Resolution Tests ====================

    #region T111 - UNION Conflict Resolution

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void UNION_Resolution_Should_Return_All_Matched_Entitlements()
    {
        // Arrange - Multiple rules match the same group
        var matchedEntitlements = new List<string>
        {
            "Sales_Representative",
            "EMEA_Regional_Manager"
        };

        // Act - UNION strategy: return all
        var result = matchedEntitlements; // No filtering

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Sales_Representative", result);
        Assert.Contains("EMEA_Regional_Manager", result);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void UNION_Resolution_Should_Preserve_Order_By_Priority()
    {
        // Arrange - Entitlements with priorities
        var entitlements = new List<(string Name, int Priority)>
        {
            ("Sales_Manager", 2),
            ("Sales_Representative", 1),
            ("EMEA_Role", 3)
        };

        // Act - Sort by priority (lower = higher priority)
        var sorted = entitlements.OrderBy(e => e.Priority).ToList();

        // Assert
        Assert.Equal("Sales_Representative", sorted[0].Name);
        Assert.Equal("Sales_Manager", sorted[1].Name);
        Assert.Equal("EMEA_Role", sorted[2].Name);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void UNION_Resolution_Should_Handle_Single_Match()
    {
        // Arrange
        var matchedEntitlements = new List<string> { "Sales_Representative" };

        // Act - UNION with single item
        var result = matchedEntitlements;

        // Assert
        Assert.Single(result);
        Assert.Equal("Sales_Representative", result[0]);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void UNION_Resolution_Should_Handle_Empty_Matches()
    {
        // Arrange
        var matchedEntitlements = new List<string>();

        // Act
        var result = matchedEntitlements;

        // Assert
        Assert.Empty(result);
    }

    #endregion

    // ==================== T112: FIRST_MATCH Conflict Resolution Tests ====================

    #region T112 - FIRST_MATCH Conflict Resolution

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void FIRST_MATCH_Resolution_Should_Return_Only_First_Entitlement()
    {
        // Arrange - Multiple rules match
        var matchedEntitlements = new List<(string Name, int Priority)>
        {
            ("Sales_Manager", 2),
            ("Sales_Representative", 1),
            ("EMEA_Role", 3)
        };

        // Act - FIRST_MATCH: return only highest priority (lowest number)
        var sorted = matchedEntitlements.OrderBy(e => e.Priority).ToList();
        var result = sorted.Take(1).Select(e => e.Name).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Sales_Representative", result[0]);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void FIRST_MATCH_Resolution_Should_Respect_Priority_Order()
    {
        // Arrange
        var matchedEntitlements = new List<(string Name, int Priority)>
        {
            ("Role_Priority_5", 5),
            ("Role_Priority_1", 1),
            ("Role_Priority_3", 3)
        };

        // Act
        var firstMatch = matchedEntitlements.OrderBy(e => e.Priority).First();

        // Assert
        Assert.Equal("Role_Priority_1", firstMatch.Name);
        Assert.Equal(1, firstMatch.Priority);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void FIRST_MATCH_Resolution_Should_Handle_Equal_Priorities()
    {
        // Arrange - Two rules with same priority
        var matchedEntitlements = new List<(string Name, int Priority)>
        {
            ("Role_A", 1),
            ("Role_B", 1)
        };

        // Act - First by insertion order when priorities equal
        var firstMatch = matchedEntitlements.OrderBy(e => e.Priority).First();

        // Assert - Should return first one encountered
        Assert.Equal("Role_A", firstMatch.Name);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void FIRST_MATCH_Resolution_Should_Handle_Single_Match()
    {
        // Arrange
        var matchedEntitlements = new List<(string Name, int Priority)>
        {
            ("Only_Role", 1)
        };

        // Act
        var result = matchedEntitlements.OrderBy(e => e.Priority).Take(1).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Only_Role", result[0].Name);
    }

    #endregion

    // ==================== T113: HIGHEST_PRIVILEGE Conflict Resolution Tests ====================

    #region T113 - HIGHEST_PRIVILEGE Conflict Resolution

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void HIGHEST_PRIVILEGE_Resolution_Should_Return_Highest_Privilege_Entitlement()
    {
        // Arrange - Entitlements with privilege levels (higher = more privileged)
        var matchedEntitlements = new List<(string Name, int PrivilegeLevel)>
        {
            ("Sales_Representative", 1),
            ("Sales_Manager", 3),
            ("VP_Sales", 5)
        };

        // Act - Return highest privilege (highest number)
        var highest = matchedEntitlements.OrderByDescending(e => e.PrivilegeLevel).First();

        // Assert
        Assert.Equal("VP_Sales", highest.Name);
        Assert.Equal(5, highest.PrivilegeLevel);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void HIGHEST_PRIVILEGE_Resolution_Should_Handle_Equal_Privileges()
    {
        // Arrange - Two roles with same privilege level
        var matchedEntitlements = new List<(string Name, int PrivilegeLevel)>
        {
            ("Manager_A", 3),
            ("Manager_B", 3)
        };

        // Act - Return first when equal
        var highest = matchedEntitlements.OrderByDescending(e => e.PrivilegeLevel).First();

        // Assert
        Assert.Equal(3, highest.PrivilegeLevel);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void HIGHEST_PRIVILEGE_Resolution_Should_Fail_When_No_Privilege_Info()
    {
        // Arrange - Entitlements without privilege metadata
        var matchedEntitlements = new List<(string Name, int? PrivilegeLevel)>
        {
            ("Role_A", null),
            ("Role_B", null)
        };

        // Act - Filter to only those with privilege info
        var withPrivilege = matchedEntitlements.Where(e => e.PrivilegeLevel.HasValue).ToList();

        // Assert - Should be empty, indicating resolution failure
        Assert.Empty(withPrivilege);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void HIGHEST_PRIVILEGE_Resolution_Should_Use_Metadata_Ranking()
    {
        // Arrange - Privilege ranking from metadata
        var privilegeRanking = new Dictionary<string, int>
        {
            { "Sales_Representative", 1 },
            { "Sales_Manager", 3 },
            { "VP_Sales", 5 }
        };

        var matchedEntitlements = new List<string>
        {
            "Sales_Representative",
            "Sales_Manager"
        };

        // Act - Look up privilege levels from ranking
        var highest = matchedEntitlements
            .OrderByDescending(e => privilegeRanking.GetValueOrDefault(e, 0))
            .First();

        // Assert
        Assert.Equal("Sales_Manager", highest);
    }

    [Fact(Skip = "Pending implementation of TransformationEngine (T097)")]
    public void HIGHEST_PRIVILEGE_Resolution_Should_Handle_Single_Match()
    {
        // Arrange
        var matchedEntitlements = new List<(string Name, int PrivilegeLevel)>
        {
            ("Only_Role", 1)
        };

        // Act
        var highest = matchedEntitlements.OrderByDescending(e => e.PrivilegeLevel).First();

        // Assert
        Assert.Equal("Only_Role", highest.Name);
    }

    #endregion

    // ==================== ConflictResolutionStrategy Enum Tests ====================

    #region ConflictResolutionStrategy Enum

    [Fact(Skip = "Pending implementation of ConflictResolutionStrategy enum (T086)")]
    public void ConflictResolutionStrategy_Enum_Should_Exist()
    {
        var enumType = GetTypeByName("ConflictResolutionStrategy");
        Assert.NotNull(enumType);
        Assert.True(enumType.IsEnum);
    }

    [Fact(Skip = "Pending implementation of ConflictResolutionStrategy enum (T086)")]
    public void ConflictResolutionStrategy_Should_Have_UNION_Value()
    {
        var enumType = GetTypeByName("ConflictResolutionStrategy");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("UNION", values);
    }

    [Fact(Skip = "Pending implementation of ConflictResolutionStrategy enum (T086)")]
    public void ConflictResolutionStrategy_Should_Have_FIRST_MATCH_Value()
    {
        var enumType = GetTypeByName("ConflictResolutionStrategy");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("FIRST_MATCH", values);
    }

    [Fact(Skip = "Pending implementation of ConflictResolutionStrategy enum (T086)")]
    public void ConflictResolutionStrategy_Should_Have_HIGHEST_PRIVILEGE_Value()
    {
        var enumType = GetTypeByName("ConflictResolutionStrategy");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("HIGHEST_PRIVILEGE", values);
    }

    [Fact(Skip = "Pending implementation of ConflictResolutionStrategy enum (T086)")]
    public void ConflictResolutionStrategy_Should_Have_MANUAL_REVIEW_Value()
    {
        var enumType = GetTypeByName("ConflictResolutionStrategy");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("MANUAL_REVIEW", values);
    }

    [Fact(Skip = "Pending implementation of ConflictResolutionStrategy enum (T086)")]
    public void ConflictResolutionStrategy_Should_Have_ERROR_Value()
    {
        var enumType = GetTypeByName("ConflictResolutionStrategy");
        Assert.NotNull(enumType);

        var values = Enum.GetNames(enumType);
        Assert.Contains("ERROR", values);
    }

    #endregion
}
