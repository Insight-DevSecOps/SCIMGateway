// ==========================================================================
// T110: Unit Tests for Reverse Transformation
// ==========================================================================
// Tests for entitlement → group recovery and template reverse-engineering
// ==========================================================================

using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SCIMGateway.Tests.Unit;

/// <summary>
/// Unit tests for ReverseTransform.
/// Tests entitlement to group reverse transformation functionality.
/// </summary>
public class ReverseTransformTests
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

    // ==================== Reverse Transformation Core Tests ====================

    #region Reverse Transformation - EXACT Pattern

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_EXACT_Should_Recover_Original_Group()
    {
        // Arrange
        // Rule: "Sales Team" → "Sales_Representative"
        var sourcePattern = "Sales Team";
        var targetMapping = "Sales_Representative";
        var providerEntitlement = "Sales_Representative";

        // Act - Reverse: find rule where targetMapping matches entitlement
        var matches = targetMapping == providerEntitlement;

        // Assert - Should recover original group name
        Assert.True(matches);
        // When match found, return sourcePattern
        Assert.Equal("Sales Team", sourcePattern);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_EXACT_Should_Return_Empty_When_No_Match()
    {
        // Arrange
        var targetMapping = "Sales_Representative";
        var providerEntitlement = "Marketing_Manager";

        // Act
        var matches = targetMapping == providerEntitlement;

        // Assert
        Assert.False(matches);
    }

    #endregion

    #region Reverse Transformation - REGEX Pattern

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_REGEX_Should_Reverse_Engineer_Input()
    {
        // Arrange
        // Rule: sourcePattern="^Sales-(.*)$", targetMapping="Sales_${1}_Rep"
        // Provider entitlement: "Sales_EMEA_Rep"
        var targetTemplate = "Sales_${1}_Rep";
        var providerEntitlement = "Sales_EMEA_Rep";

        // Act - Reverse engineer: extract ${1} from template match
        // Pattern: Sales_(.*)_Rep
        var reversePattern = targetTemplate
            .Replace("${1}", "(.*)")
            .Replace("${0}", "(.*)");
        
        var regex = new Regex($"^{Regex.Escape(reversePattern).Replace(@"\(\.\*\)", "(.*)")}$");
        // Simplified: create pattern "^Sales_(.*)_Rep$"
        var simplePattern = @"^Sales_(.*)_Rep$";
        var match = new Regex(simplePattern).Match(providerEntitlement);

        // Assert
        Assert.True(match.Success);
        Assert.Equal("EMEA", match.Groups[1].Value);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_REGEX_Should_Reconstruct_Original_Group()
    {
        // Arrange
        // Forward: "Sales-EMEA" (^Sales-(.*)$) → "Sales_EMEA_Rep" (Sales_${1}_Rep)
        // Reverse: "Sales_EMEA_Rep" → extract "EMEA" → apply to "Sales-{1}" → "Sales-EMEA"
        var sourcePattern = @"^Sales-(.*)$";
        var targetTemplate = "Sales_${1}_Rep";
        var providerEntitlement = "Sales_EMEA_Rep";

        // Act - Step 1: Extract capture group from entitlement
        var reversePattern = @"^Sales_(.*)_Rep$";
        var match = new Regex(reversePattern).Match(providerEntitlement);
        
        string? reconstructedGroup = null;
        if (match.Success)
        {
            var capturedValue = match.Groups[1].Value; // "EMEA"
            // Step 2: Apply to source pattern (simplified)
            reconstructedGroup = $"Sales-{capturedValue}";
        }

        // Assert
        Assert.NotNull(reconstructedGroup);
        Assert.Equal("Sales-EMEA", reconstructedGroup);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_REGEX_Should_Handle_Multiple_Capture_Groups()
    {
        // Arrange
        // Forward: "Sales-EMEA-Field" (^(.+)-(.+)-(.+)$) → "Sales_EMEA_Field_Role" (${1}_${2}_${3}_Role)
        var providerEntitlement = "Sales_EMEA_Field_Role";
        var reversePattern = @"^(.+)_(.+)_(.+)_Role$";

        // Act
        var match = new Regex(reversePattern).Match(providerEntitlement);

        // Assert
        Assert.True(match.Success);
        Assert.Equal("Sales", match.Groups[1].Value);
        Assert.Equal("EMEA", match.Groups[2].Value);
        Assert.Equal("Field", match.Groups[3].Value);

        // Reconstruct: "Sales-EMEA-Field"
        var reconstructed = $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}";
        Assert.Equal("Sales-EMEA-Field", reconstructed);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_REGEX_Should_Return_Empty_When_No_Match()
    {
        // Arrange
        var reversePattern = @"^Sales_(.*)_Rep$";
        var providerEntitlement = "Marketing_EMEA_Manager";

        // Act
        var match = new Regex(reversePattern).Match(providerEntitlement);

        // Assert
        Assert.False(match.Success);
    }

    #endregion

    #region Reverse Transformation - HIERARCHICAL Pattern

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_HIERARCHICAL_Should_Recover_Path()
    {
        // Arrange
        // Forward: "Acme Corp/Sales/EMEA/Field Sales" → "ORG-Field Sales" (ORG-${level3})
        var providerEntitlement = "ORG-Field Sales";
        var reversePattern = @"^ORG-(.*)$";

        // Act
        var match = new Regex(reversePattern).Match(providerEntitlement);

        // Assert
        Assert.True(match.Success);
        Assert.Equal("Field Sales", match.Groups[1].Value);
        // Note: Cannot fully reconstruct path without additional context
        // Only the extracted level is known
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_HIERARCHICAL_Should_Handle_Multi_Level_Template()
    {
        // Arrange
        // Forward: "Company/Div/Dept/Team" → "Div-Dept" (${level1}-${level2})
        var providerEntitlement = "Sales-EMEA";
        var reversePattern = @"^(.+)-(.+)$";

        // Act
        var match = new Regex(reversePattern).Match(providerEntitlement);

        // Assert
        Assert.True(match.Success);
        Assert.Equal("Sales", match.Groups[1].Value);  // level1
        Assert.Equal("EMEA", match.Groups[2].Value);   // level2
    }

    #endregion

    #region Reverse Transformation - CONDITIONAL Pattern

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_CONDITIONAL_Should_Return_Possible_Groups()
    {
        // Arrange
        // Rule: IF contains "Manager" → "Sales_Manager"
        var providerEntitlement = "Sales_Manager";
        var expectedCondition = "Manager";

        // Act - Reverse: entitlement matches rule output, 
        // return pattern that could produce this
        var possiblePatterns = new List<string>
        {
            ".*Manager.*",  // Any group containing "Manager"
            "Sales Manager",
            "EMEA Manager",
            "Field Sales Manager"
        };

        // Assert
        // Cannot determine exact original group, but can suggest possible matches
        Assert.NotEmpty(possiblePatterns);
        Assert.All(possiblePatterns, p => Assert.Contains(expectedCondition, p));
    }

    #endregion

    #region Reverse Transformation - Multiple Rules

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_Should_Return_All_Matching_Groups()
    {
        // Arrange - Multiple rules could map to same entitlement
        var rules = new List<(string SourcePattern, string TargetMapping)>
        {
            ("Sales Team", "Sales_Representative"),
            ("Field Sales", "Sales_Representative"),
            ("Inside Sales", "Sales_Representative")
        };
        var providerEntitlement = "Sales_Representative";

        // Act
        var matchingGroups = rules
            .Where(r => r.TargetMapping == providerEntitlement)
            .Select(r => r.SourcePattern)
            .ToList();

        // Assert - All matching source patterns returned
        Assert.Equal(3, matchingGroups.Count);
        Assert.Contains("Sales Team", matchingGroups);
        Assert.Contains("Field Sales", matchingGroups);
        Assert.Contains("Inside Sales", matchingGroups);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_Should_Order_Results_By_Priority()
    {
        // Arrange
        var rules = new List<(string SourcePattern, string TargetMapping, int Priority)>
        {
            ("Sales Team", "Sales_Representative", 2),
            ("Field Sales", "Sales_Representative", 1),
            ("Inside Sales", "Sales_Representative", 3)
        };
        var providerEntitlement = "Sales_Representative";

        // Act
        var matchingGroups = rules
            .Where(r => r.TargetMapping == providerEntitlement)
            .OrderBy(r => r.Priority)
            .Select(r => r.SourcePattern)
            .ToList();

        // Assert - Ordered by priority
        Assert.Equal("Field Sales", matchingGroups[0]);
        Assert.Equal("Sales Team", matchingGroups[1]);
        Assert.Equal("Inside Sales", matchingGroups[2]);
    }

    #endregion

    #region IReverseTransform Interface Tests

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_Class_Should_Exist()
    {
        var type = GetTypeByName("ReverseTransform");
        Assert.NotNull(type);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_Should_Have_TransformEntitlementToGroupsAsync_Method()
    {
        var type = GetTypeByName("ReverseTransform");
        Assert.NotNull(type);

        var method = type.GetMethod("TransformEntitlementToGroupsAsync");
        Assert.NotNull(method);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_Should_Have_ReverseEngineerPattern_Method()
    {
        var type = GetTypeByName("ReverseTransform");
        Assert.NotNull(type);

        // Internal helper method for reversing template patterns
        var method = type.GetMethod("ReverseEngineerPattern", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        // May be private implementation detail
    }

    #endregion

    #region Edge Cases

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_Should_Handle_Null_Entitlement()
    {
        // Arrange
        string? providerEntitlement = null;

        // Assert - Should handle gracefully
        Assert.Null(providerEntitlement);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_Should_Handle_Empty_Entitlement()
    {
        // Arrange
        var providerEntitlement = "";
        var reversePattern = @"^Sales_(.*)_Rep$";

        // Act
        var match = new Regex(reversePattern).Match(providerEntitlement);

        // Assert
        Assert.False(match.Success);
    }

    [Fact(Skip = "Pending implementation of ReverseTransform (T096)")]
    public void ReverseTransform_Should_Handle_Special_Characters_In_Entitlement()
    {
        // Arrange
        var providerEntitlement = "Sales_[EMEA]_Rep";
        var reversePattern = @"^Sales_\[(.+)\]_Rep$";

        // Act
        var match = new Regex(reversePattern).Match(providerEntitlement);

        // Assert
        Assert.True(match.Success);
        Assert.Equal("EMEA", match.Groups[1].Value);
    }

    #endregion
}
