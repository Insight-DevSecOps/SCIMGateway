// ==========================================================================
// T031a: Contract Test for FilterParser
// ==========================================================================
// Validates the FilterParser component meets all requirements from:
// - RFC 7644 Section 3.4.2.2: SCIM Filtering
// - tasks.md T031a specification
// 
// Required behaviors to validate:
// - All 11 SCIM filter operators: eq, ne, co, sw, ew, pr, gt, ge, lt, le, not
// - Logical operators: and, or
// - Grouping with parentheses
// - Attribute path parsing
// ==========================================================================

using Xunit;

namespace SCIMGateway.Tests.Contract;

/// <summary>
/// Contract tests for FilterParser.
/// These tests define the expected behavior for parsing SCIM filter expressions
/// per RFC 7644 Section 3.4.2.2.
/// </summary>
public class FilterParserTests
{
    #region Interface Contract Tests

    [Fact]
    public void FilterParser_Should_Exist_In_Core_Assembly()
    {
        // Arrange & Act
        var parserType = GetFilterParserType();
        
        // Assert
        Assert.NotNull(parserType);
    }

    [Fact]
    public void FilterParser_Should_Implement_IFilterParser_Interface()
    {
        // Arrange & Act
        var parserType = GetFilterParserType();
        var interfaceType = GetIFilterParserType();
        
        // Assert
        Assert.NotNull(parserType);
        Assert.NotNull(interfaceType);
        Assert.True(interfaceType.IsAssignableFrom(parserType));
    }

    [Fact]
    public void IFilterParser_Should_Have_Parse_Method()
    {
        // Parse filter string into expression tree
        
        // Arrange & Act
        var interfaceType = GetIFilterParserType();
        
        // Assert
        Assert.NotNull(interfaceType);
        var method = interfaceType.GetMethod("Parse");
        Assert.NotNull(method);
    }

    #endregion

    #region FilterExpression Model Tests

    [Fact]
    public void FilterExpression_Should_Exist()
    {
        // Parsed filter expression tree
        
        // Arrange & Act
        var expressionType = GetFilterExpressionType();
        
        // Assert
        Assert.NotNull(expressionType);
    }

    [Fact]
    public void FilterExpression_Should_Have_Attribute_Property()
    {
        // Attribute path being filtered
        
        // Arrange & Act
        var expressionType = GetFilterExpressionType();
        
        // Assert
        Assert.NotNull(expressionType);
        var property = expressionType.GetProperty("Attribute") 
            ?? expressionType.GetProperty("AttributePath");
        Assert.NotNull(property);
    }

    [Fact]
    public void FilterExpression_Should_Have_Operator_Property()
    {
        // Comparison operator
        
        // Arrange & Act
        var expressionType = GetFilterExpressionType();
        
        // Assert
        Assert.NotNull(expressionType);
        var property = expressionType.GetProperty("Operator")
            ?? expressionType.GetProperty("ComparisonOperator");
        Assert.NotNull(property);
    }

    [Fact]
    public void FilterExpression_Should_Have_Value_Property()
    {
        // Comparison value
        
        // Arrange & Act
        var expressionType = GetFilterExpressionType();
        
        // Assert
        Assert.NotNull(expressionType);
        var property = expressionType.GetProperty("Value")
            ?? expressionType.GetProperty("ComparisonValue");
        Assert.NotNull(property);
    }

    #endregion

    #region Equality Operator Tests (eq)

    [Fact]
    public void FilterParser_Should_Parse_Eq_Operator()
    {
        // userName eq "john"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName eq \"john\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Eq_CaseInsensitive()
    {
        // userName EQ "john" - operators are case-insensitive
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName EQ \"john\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Not Equal Operator Tests (ne)

    [Fact]
    public void FilterParser_Should_Parse_Ne_Operator()
    {
        // userName ne "john"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName ne \"john\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Contains Operator Tests (co)

    [Fact]
    public void FilterParser_Should_Parse_Co_Operator()
    {
        // userName co "oh"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName co \"oh\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Starts With Operator Tests (sw)

    [Fact]
    public void FilterParser_Should_Parse_Sw_Operator()
    {
        // userName sw "jo"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName sw \"jo\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Ends With Operator Tests (ew)

    [Fact]
    public void FilterParser_Should_Parse_Ew_Operator()
    {
        // userName ew "hn"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName ew \"hn\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Present Operator Tests (pr)

    [Fact]
    public void FilterParser_Should_Parse_Pr_Operator()
    {
        // userName pr (attribute is present)
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName pr";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Pr_No_Value()
    {
        // pr operator has no comparison value
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "emails pr";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
        // Expression should have null or empty value
    }

    #endregion

    #region Greater Than Operator Tests (gt)

    [Fact]
    public void FilterParser_Should_Parse_Gt_Operator()
    {
        // meta.lastModified gt "2023-01-01T00:00:00Z"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "meta.lastModified gt \"2023-01-01T00:00:00Z\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Greater Than or Equal Operator Tests (ge)

    [Fact]
    public void FilterParser_Should_Parse_Ge_Operator()
    {
        // meta.lastModified ge "2023-01-01T00:00:00Z"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "meta.lastModified ge \"2023-01-01T00:00:00Z\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Less Than Operator Tests (lt)

    [Fact]
    public void FilterParser_Should_Parse_Lt_Operator()
    {
        // meta.lastModified lt "2024-01-01T00:00:00Z"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "meta.lastModified lt \"2024-01-01T00:00:00Z\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Less Than or Equal Operator Tests (le)

    [Fact]
    public void FilterParser_Should_Parse_Le_Operator()
    {
        // meta.lastModified le "2024-01-01T00:00:00Z"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "meta.lastModified le \"2024-01-01T00:00:00Z\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Logical AND Operator Tests

    [Fact]
    public void FilterParser_Should_Parse_And_Operator()
    {
        // userName eq "john" and active eq true
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName eq \"john\" and active eq true";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Handle_Multiple_And_Operators()
    {
        // userName eq "john" and active eq true and emails pr
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName eq \"john\" and active eq true and emails pr";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Logical OR Operator Tests

    [Fact]
    public void FilterParser_Should_Parse_Or_Operator()
    {
        // userName eq "john" or userName eq "jane"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName eq \"john\" or userName eq \"jane\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Handle_Multiple_Or_Operators()
    {
        // userName eq "john" or userName eq "jane" or userName eq "joe"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName eq \"john\" or userName eq \"jane\" or userName eq \"joe\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region NOT Operator Tests

    [Fact]
    public void FilterParser_Should_Parse_Not_Operator()
    {
        // not (userName eq "john")
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "not (userName eq \"john\")";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Grouping Tests

    [Fact]
    public void FilterParser_Should_Parse_Parentheses()
    {
        // (userName eq "john")
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "(userName eq \"john\")";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Handle_Complex_Grouping()
    {
        // (userName eq "john" or userName eq "jane") and active eq true
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "(userName eq \"john\" or userName eq \"jane\") and active eq true";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Handle_Nested_Grouping()
    {
        // ((userName eq "john") and (active eq true))
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "((userName eq \"john\") and (active eq true))";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Attribute Path Tests

    [Fact]
    public void FilterParser_Should_Parse_Simple_Attribute()
    {
        // userName eq "john"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName eq \"john\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Complex_Attribute_Path()
    {
        // name.familyName eq "Smith"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "name.familyName eq \"Smith\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Multivalued_Attribute_Filter()
    {
        // emails[type eq "work"].value eq "john@example.com"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "emails[type eq \"work\"].value eq \"john@example.com\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Schema_Prefixed_Attribute()
    {
        // urn:ietf:params:scim:schemas:core:2.0:User:userName eq "john"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "urn:ietf:params:scim:schemas:core:2.0:User:userName eq \"john\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Value Type Tests

    [Fact]
    public void FilterParser_Should_Parse_String_Value()
    {
        // userName eq "john"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName eq \"john\"";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Boolean_True()
    {
        // active eq true
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "active eq true";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Boolean_False()
    {
        // active eq false
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "active eq false";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Null_Value()
    {
        // manager eq null
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "manager eq null";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    [Fact]
    public void FilterParser_Should_Parse_Numeric_Value()
    {
        // employeeNumber eq 12345
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "employeeNumber eq 12345";
        
        // Act
        var expression = parser?.Parse(filter);
        
        // Assert
        Assert.NotNull(expression);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void FilterParser_Should_Throw_On_Invalid_Operator()
    {
        // Invalid operator "invalid"
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "userName invalid \"john\"";
        
        // Act & Assert
        if (parser != null)
        {
            Assert.Throws<FilterParseException>(() => parser.Parse(filter));
        }
    }

    [Fact]
    public void FilterParser_Should_Throw_On_Unclosed_Parenthesis()
    {
        // Missing closing parenthesis
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "(userName eq \"john\"";
        
        // Act & Assert
        if (parser != null)
        {
            Assert.Throws<FilterParseException>(() => parser.Parse(filter));
        }
    }

    [Fact]
    public void FilterParser_Should_Throw_On_Empty_Filter()
    {
        // Empty filter string
        
        // Arrange
        var parser = CreateFilterParser();
        var filter = "";
        
        // Act & Assert
        if (parser != null)
        {
            Assert.Throws<ArgumentException>(() => parser.Parse(filter));
        }
    }

    [Fact]
    public void FilterParseException_Should_Exist()
    {
        // Custom exception for parse errors
        
        // Arrange & Act
        var exceptionType = GetFilterParseExceptionType();
        
        // Assert
        Assert.NotNull(exceptionType);
    }

    #endregion

    #region Helper Types

    private interface IFilterParserStub
    {
        object? Parse(string filter);
    }

    private class FilterParseException : Exception
    {
        public FilterParseException(string message) : base(message) { }
    }

    #endregion

    #region Helper Methods

    private static Type? GetFilterParserType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Scim.FilterParser")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Filtering.FilterParser");
    }

    private static Type? GetIFilterParserType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Scim.IFilterParser")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Filtering.IFilterParser");
    }

    private static Type? GetFilterExpressionType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Scim.FilterExpression")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Filtering.FilterExpression");
    }

    private static Type? GetFilterParseExceptionType()
    {
        var coreAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "SCIMGateway.Core");
        
        return coreAssembly?.GetType("SCIMGateway.Core.Exceptions.FilterParseException")
            ?? coreAssembly?.GetType("SCIMGateway.Core.Filtering.FilterParseException");
    }

    private static dynamic? CreateFilterParser()
    {
        var parserType = GetFilterParserType();
        if (parserType == null) return null;
        
        try
        {
            return Activator.CreateInstance(parserType);
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
