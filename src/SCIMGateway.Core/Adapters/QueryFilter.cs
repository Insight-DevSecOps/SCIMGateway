// ==========================================================================
// T068: QueryFilter Type
// ==========================================================================
// Query filter for adapter list operations per contracts/adapter-interface.md
// ==========================================================================

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Query filter for list operations on adapters.
/// Supports SCIM filtering, pagination, and sorting.
/// </summary>
public class QueryFilter
{
    /// <summary>
    /// SCIM filter expression (e.g., "userName eq 'john@example.com'").
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Attributes to return (comma-separated).
    /// If null, all attributes are returned.
    /// </summary>
    public string? Attributes { get; set; }

    /// <summary>
    /// Attributes to exclude from response (comma-separated).
    /// </summary>
    public string? ExcludedAttributes { get; set; }

    /// <summary>
    /// Attribute name to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order (ascending or descending).
    /// </summary>
    public SortOrder SortOrder { get; set; } = SortOrder.Ascending;

    /// <summary>
    /// 1-based start index for pagination.
    /// </summary>
    public int StartIndex { get; set; } = 1;

    /// <summary>
    /// Maximum number of results per page.
    /// </summary>
    public int Count { get; set; } = 100;

    /// <summary>
    /// Creates a default query filter with no filtering.
    /// </summary>
    public static QueryFilter Default => new();

    /// <summary>
    /// Creates a query filter with a specific filter expression.
    /// </summary>
    /// <param name="filter">SCIM filter expression</param>
    /// <returns>QueryFilter instance</returns>
    public static QueryFilter WithFilter(string filter) => new() { Filter = filter };

    /// <summary>
    /// Creates a query filter for a specific page.
    /// </summary>
    /// <param name="startIndex">1-based start index</param>
    /// <param name="count">Number of results per page</param>
    /// <returns>QueryFilter instance</returns>
    public static QueryFilter ForPage(int startIndex, int count) => new()
    {
        StartIndex = startIndex,
        Count = count
    };

    /// <summary>
    /// Gets the attribute names to return as a list.
    /// </summary>
    public IReadOnlyList<string> GetAttributesList()
    {
        if (string.IsNullOrWhiteSpace(Attributes))
            return [];

        return Attributes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Gets the excluded attribute names as a list.
    /// </summary>
    public IReadOnlyList<string> GetExcludedAttributesList()
    {
        if (string.IsNullOrWhiteSpace(ExcludedAttributes))
            return [];

        return ExcludedAttributes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Validates the query filter parameters.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        if (StartIndex < 1)
            return false;

        if (Count < 1 || Count > 1000)
            return false;

        return true;
    }

    /// <summary>
    /// Creates a copy of this filter for the next page.
    /// </summary>
    /// <returns>QueryFilter for next page</returns>
    public QueryFilter NextPage()
    {
        return new QueryFilter
        {
            Filter = Filter,
            Attributes = Attributes,
            ExcludedAttributes = ExcludedAttributes,
            SortBy = SortBy,
            SortOrder = SortOrder,
            StartIndex = StartIndex + Count,
            Count = Count
        };
    }
}

/// <summary>
/// Sort order for query results.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Sort in ascending order (A-Z, 0-9).
    /// </summary>
    Ascending,

    /// <summary>
    /// Sort in descending order (Z-A, 9-0).
    /// </summary>
    Descending
}
