// ==========================================================================
// T031: FilterParser - SCIM Filter Expression Parser
// ==========================================================================
// Parses SCIM filter expressions per RFC 7644 Section 3.4.2.2
// Supports all 11 operators: eq, ne, co, sw, ew, pr, gt, ge, lt, le, not
// Plus logical operators: and, or
// ==========================================================================

using System.Text.RegularExpressions;

namespace SCIMGateway.Core.Filtering;

/// <summary>
/// Interface for SCIM filter parsing.
/// </summary>
public interface IFilterParser
{
    /// <summary>
    /// Parses a SCIM filter expression string into a filter expression tree.
    /// </summary>
    /// <param name="filter">The filter string to parse.</param>
    /// <returns>Parsed filter expression.</returns>
    FilterExpression Parse(string filter);
}

/// <summary>
/// Represents a parsed SCIM filter expression.
/// </summary>
public class FilterExpression
{
    /// <summary>
    /// Type of expression (comparison, logical, etc.).
    /// </summary>
    public FilterExpressionType ExpressionType { get; set; }

    /// <summary>
    /// Attribute path for comparison expressions.
    /// </summary>
    public string? AttributePath { get; set; }

    /// <summary>
    /// Alias for AttributePath.
    /// </summary>
    public string? Attribute
    {
        get => AttributePath;
        set => AttributePath = value;
    }

    /// <summary>
    /// Comparison operator.
    /// </summary>
    public FilterOperator? Operator { get; set; }

    /// <summary>
    /// Alias for Operator.
    /// </summary>
    public FilterOperator? ComparisonOperator
    {
        get => Operator;
        set => Operator = value;
    }

    /// <summary>
    /// Comparison value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Alias for Value.
    /// </summary>
    public object? ComparisonValue
    {
        get => Value;
        set => Value = value;
    }

    /// <summary>
    /// Left operand for logical expressions.
    /// </summary>
    public FilterExpression? Left { get; set; }

    /// <summary>
    /// Right operand for logical expressions.
    /// </summary>
    public FilterExpression? Right { get; set; }

    /// <summary>
    /// Logical operator for logical expressions.
    /// </summary>
    public LogicalOperator? LogicalOperator { get; set; }

    /// <summary>
    /// Inner expression for NOT expressions.
    /// </summary>
    public FilterExpression? Inner { get; set; }

    /// <summary>
    /// Sub-filter for multi-valued attribute filtering.
    /// e.g., emails[type eq "work"]
    /// </summary>
    public FilterExpression? SubFilter { get; set; }
}

/// <summary>
/// Type of filter expression.
/// </summary>
public enum FilterExpressionType
{
    /// <summary>
    /// Comparison expression (e.g., userName eq "john").
    /// </summary>
    Comparison,

    /// <summary>
    /// Logical AND/OR expression.
    /// </summary>
    Logical,

    /// <summary>
    /// NOT expression.
    /// </summary>
    Not,

    /// <summary>
    /// Presence expression (e.g., emails pr).
    /// </summary>
    Presence,

    /// <summary>
    /// Grouped expression (parentheses).
    /// </summary>
    Group,

    /// <summary>
    /// Multi-valued attribute filter.
    /// </summary>
    ValuePath
}

/// <summary>
/// SCIM filter operators per RFC 7644.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Equal (eq).
    /// </summary>
    Equal,

    /// <summary>
    /// Not equal (ne).
    /// </summary>
    NotEqual,

    /// <summary>
    /// Contains (co).
    /// </summary>
    Contains,

    /// <summary>
    /// Starts with (sw).
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with (ew).
    /// </summary>
    EndsWith,

    /// <summary>
    /// Present (pr).
    /// </summary>
    Present,

    /// <summary>
    /// Greater than (gt).
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal (ge).
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than (lt).
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal (le).
    /// </summary>
    LessThanOrEqual
}

/// <summary>
/// Logical operators for combining filter expressions.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// Logical AND.
    /// </summary>
    And,

    /// <summary>
    /// Logical OR.
    /// </summary>
    Or
}

/// <summary>
/// SCIM filter parser implementation.
/// </summary>
public partial class FilterParser : IFilterParser
{
    // Token patterns
    private static readonly string[] Operators = ["eq", "ne", "co", "sw", "ew", "pr", "gt", "ge", "lt", "le"];
    private static readonly string[] LogicalOps = ["and", "or"];

    /// <inheritdoc />
    public FilterExpression Parse(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            throw new ArgumentException("Filter cannot be null or empty", nameof(filter));
        }

        var tokens = Tokenize(filter.Trim());
        var index = 0;
        return ParseExpression(tokens, ref index);
    }

    private FilterExpression ParseExpression(List<Token> tokens, ref int index)
    {
        var left = ParseOrExpression(tokens, ref index);
        return left;
    }

    private FilterExpression ParseOrExpression(List<Token> tokens, ref int index)
    {
        var left = ParseAndExpression(tokens, ref index);

        while (index < tokens.Count && tokens[index].Type == TokenType.Or)
        {
            index++; // consume 'or'
            var right = ParseAndExpression(tokens, ref index);
            left = new FilterExpression
            {
                ExpressionType = FilterExpressionType.Logical,
                LogicalOperator = Filtering.LogicalOperator.Or,
                Left = left,
                Right = right
            };
        }

        return left;
    }

    private FilterExpression ParseAndExpression(List<Token> tokens, ref int index)
    {
        var left = ParseUnaryExpression(tokens, ref index);

        while (index < tokens.Count && tokens[index].Type == TokenType.And)
        {
            index++; // consume 'and'
            var right = ParseUnaryExpression(tokens, ref index);
            left = new FilterExpression
            {
                ExpressionType = FilterExpressionType.Logical,
                LogicalOperator = Filtering.LogicalOperator.And,
                Left = left,
                Right = right
            };
        }

        return left;
    }

    private FilterExpression ParseUnaryExpression(List<Token> tokens, ref int index)
    {
        if (index < tokens.Count && tokens[index].Type == TokenType.Not)
        {
            index++; // consume 'not'
            
            // Expect opening parenthesis after 'not'
            if (index >= tokens.Count || tokens[index].Type != TokenType.LeftParen)
            {
                throw new FilterParseException("Expected '(' after 'not'");
            }
            
            var inner = ParsePrimaryExpression(tokens, ref index);
            return new FilterExpression
            {
                ExpressionType = FilterExpressionType.Not,
                Inner = inner
            };
        }

        return ParsePrimaryExpression(tokens, ref index);
    }

    private FilterExpression ParsePrimaryExpression(List<Token> tokens, ref int index)
    {
        if (index >= tokens.Count)
        {
            throw new FilterParseException("Unexpected end of filter expression");
        }

        // Handle grouped expressions
        if (tokens[index].Type == TokenType.LeftParen)
        {
            index++; // consume '('
            var expr = ParseExpression(tokens, ref index);
            
            if (index >= tokens.Count || tokens[index].Type != TokenType.RightParen)
            {
                throw new FilterParseException("Missing closing parenthesis");
            }
            
            index++; // consume ')'
            return expr;
        }

        // Handle attribute expressions
        return ParseAttributeExpression(tokens, ref index);
    }

    private FilterExpression ParseAttributeExpression(List<Token> tokens, ref int index)
    {
        if (index >= tokens.Count || tokens[index].Type != TokenType.Attribute)
        {
            throw new FilterParseException($"Expected attribute name at position {index}");
        }

        var attributePath = tokens[index].Value;
        index++;

        // Check for value path filter (e.g., emails[type eq "work"])
        if (index < tokens.Count && tokens[index].Type == TokenType.LeftBracket)
        {
            index++; // consume '['
            var subFilter = ParseExpression(tokens, ref index);
            
            if (index >= tokens.Count || tokens[index].Type != TokenType.RightBracket)
            {
                throw new FilterParseException("Missing closing bracket in value path filter");
            }
            
            index++; // consume ']'

            // Check if there's a sub-attribute after the bracket
            string? subAttribute = null;
            if (index < tokens.Count && tokens[index].Type == TokenType.Dot)
            {
                index++; // consume '.'
                if (index >= tokens.Count || tokens[index].Type != TokenType.Attribute)
                {
                    throw new FilterParseException("Expected attribute name after '.'");
                }
                subAttribute = tokens[index].Value;
                index++;
            }

            // Now parse the comparison for the value path
            if (index < tokens.Count && tokens[index].Type == TokenType.Operator)
            {
                var op = ParseOperator(tokens[index].Value);
                index++;

                object? value = null;
                if (op != FilterOperator.Present && index < tokens.Count)
                {
                    value = ParseValue(tokens, ref index);
                }

                return new FilterExpression
                {
                    ExpressionType = FilterExpressionType.ValuePath,
                    AttributePath = subAttribute != null ? $"{attributePath}.{subAttribute}" : attributePath,
                    SubFilter = subFilter,
                    Operator = op,
                    Value = value
                };
            }

            return new FilterExpression
            {
                ExpressionType = FilterExpressionType.ValuePath,
                AttributePath = attributePath,
                SubFilter = subFilter
            };
        }

        // Regular comparison
        if (index >= tokens.Count || tokens[index].Type != TokenType.Operator)
        {
            throw new FilterParseException($"Expected operator after attribute '{attributePath}'");
        }

        var @operator = ParseOperator(tokens[index].Value);
        index++;

        // Handle presence operator (no value)
        if (@operator == FilterOperator.Present)
        {
            return new FilterExpression
            {
                ExpressionType = FilterExpressionType.Presence,
                AttributePath = attributePath,
                Operator = @operator
            };
        }

        // Parse value
        if (index >= tokens.Count)
        {
            throw new FilterParseException("Expected value after operator");
        }

        var comparisonValue = ParseValue(tokens, ref index);

        return new FilterExpression
        {
            ExpressionType = FilterExpressionType.Comparison,
            AttributePath = attributePath,
            Operator = @operator,
            Value = comparisonValue
        };
    }

    private static FilterOperator ParseOperator(string op)
    {
        return op.ToLowerInvariant() switch
        {
            "eq" => FilterOperator.Equal,
            "ne" => FilterOperator.NotEqual,
            "co" => FilterOperator.Contains,
            "sw" => FilterOperator.StartsWith,
            "ew" => FilterOperator.EndsWith,
            "pr" => FilterOperator.Present,
            "gt" => FilterOperator.GreaterThan,
            "ge" => FilterOperator.GreaterThanOrEqual,
            "lt" => FilterOperator.LessThan,
            "le" => FilterOperator.LessThanOrEqual,
            _ => throw new FilterParseException($"Invalid operator: {op}")
        };
    }

    private static object? ParseValue(List<Token> tokens, ref int index)
    {
        var token = tokens[index];
        index++;

        return token.Type switch
        {
            TokenType.String => token.Value,
            TokenType.Boolean => bool.Parse(token.Value),
            TokenType.Null => null,
            TokenType.Number => ParseNumber(token.Value),
            _ => throw new FilterParseException($"Unexpected token type for value: {token.Type}")
        };
    }

    private static object ParseNumber(string value)
    {
        if (int.TryParse(value, out var intVal))
            return intVal;
        if (long.TryParse(value, out var longVal))
            return longVal;
        if (double.TryParse(value, out var doubleVal))
            return doubleVal;
        return value;
    }

    private List<Token> Tokenize(string filter)
    {
        var tokens = new List<Token>();
        var i = 0;

        while (i < filter.Length)
        {
            // Skip whitespace
            while (i < filter.Length && char.IsWhiteSpace(filter[i]))
                i++;

            if (i >= filter.Length)
                break;

            var c = filter[i];

            // Parentheses and brackets
            if (c == '(')
            {
                tokens.Add(new Token(TokenType.LeftParen, "("));
                i++;
                continue;
            }
            if (c == ')')
            {
                tokens.Add(new Token(TokenType.RightParen, ")"));
                i++;
                continue;
            }
            if (c == '[')
            {
                tokens.Add(new Token(TokenType.LeftBracket, "["));
                i++;
                continue;
            }
            if (c == ']')
            {
                tokens.Add(new Token(TokenType.RightBracket, "]"));
                i++;
                continue;
            }
            if (c == '.')
            {
                tokens.Add(new Token(TokenType.Dot, "."));
                i++;
                continue;
            }

            // String literal
            if (c == '"')
            {
                var start = i + 1;
                i++;
                while (i < filter.Length && filter[i] != '"')
                {
                    if (filter[i] == '\\' && i + 1 < filter.Length)
                        i++; // Skip escaped character
                    i++;
                }
                var value = filter[start..i].Replace("\\\"", "\"");
                tokens.Add(new Token(TokenType.String, value));
                i++; // Skip closing quote
                continue;
            }

            // Number
            if (char.IsDigit(c) || (c == '-' && i + 1 < filter.Length && char.IsDigit(filter[i + 1])))
            {
                var start = i;
                if (c == '-') i++;
                while (i < filter.Length && (char.IsDigit(filter[i]) || filter[i] == '.'))
                    i++;
                var value = filter[start..i];
                tokens.Add(new Token(TokenType.Number, value));
                continue;
            }

            // Word (attribute, operator, keyword)
            if (char.IsLetter(c) || c == '_' || c == ':')
            {
                var start = i;
                while (i < filter.Length && (char.IsLetterOrDigit(filter[i]) || filter[i] == '_' || filter[i] == '.' || filter[i] == ':' || filter[i] == '-'))
                    i++;
                var word = filter[start..i];
                var lower = word.ToLowerInvariant();

                if (lower == "true" || lower == "false")
                {
                    tokens.Add(new Token(TokenType.Boolean, lower));
                }
                else if (lower == "null")
                {
                    tokens.Add(new Token(TokenType.Null, "null"));
                }
                else if (lower == "and")
                {
                    tokens.Add(new Token(TokenType.And, "and"));
                }
                else if (lower == "or")
                {
                    tokens.Add(new Token(TokenType.Or, "or"));
                }
                else if (lower == "not")
                {
                    tokens.Add(new Token(TokenType.Not, "not"));
                }
                else if (Operators.Contains(lower))
                {
                    tokens.Add(new Token(TokenType.Operator, lower));
                }
                else
                {
                    tokens.Add(new Token(TokenType.Attribute, word));
                }
                continue;
            }

            throw new FilterParseException($"Unexpected character '{c}' at position {i}");
        }

        return tokens;
    }

    private class Token
    {
        public TokenType Type { get; }
        public string Value { get; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }

    private enum TokenType
    {
        Attribute,
        Operator,
        String,
        Number,
        Boolean,
        Null,
        And,
        Or,
        Not,
        LeftParen,
        RightParen,
        LeftBracket,
        RightBracket,
        Dot
    }
}

/// <summary>
/// Exception thrown when filter parsing fails.
/// </summary>
public class FilterParseException : Exception
{
    public FilterParseException(string message) : base(message)
    {
    }

    public FilterParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
