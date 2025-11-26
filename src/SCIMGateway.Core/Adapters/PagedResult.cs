// ==========================================================================
// T069: PagedResult<T> Type
// ==========================================================================
// Generic paged result for adapter list operations per contracts/adapter-interface.md
// ==========================================================================

namespace SCIMGateway.Core.Adapters;

/// <summary>
/// Generic paged result for list operations.
/// Follows SCIM ListResponse schema pattern.
/// </summary>
/// <typeparam name="T">Type of resources in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// List of resources in this page.
    /// </summary>
    public List<T> Resources { get; set; } = [];

    /// <summary>
    /// Total number of results available across all pages.
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// 1-based index of the first result in this page.
    /// </summary>
    public int StartIndex { get; set; } = 1;

    /// <summary>
    /// Number of results returned in this page.
    /// </summary>
    public int ItemsPerPage { get; set; }

    /// <summary>
    /// Whether there are more results available.
    /// </summary>
    public bool HasMore => StartIndex + ItemsPerPage <= TotalResults;

    /// <summary>
    /// The start index for the next page (if HasMore is true).
    /// </summary>
    public int NextStartIndex => StartIndex + ItemsPerPage;

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    public static PagedResult<T> Empty() => new()
    {
        Resources = [],
        TotalResults = 0,
        StartIndex = 1,
        ItemsPerPage = 0
    };

    /// <summary>
    /// Creates a paged result from a list of resources.
    /// </summary>
    /// <param name="resources">List of resources</param>
    /// <param name="totalResults">Total available results</param>
    /// <param name="startIndex">Start index of this page</param>
    public static PagedResult<T> FromList(
        List<T> resources,
        int totalResults,
        int startIndex = 1)
    {
        return new PagedResult<T>
        {
            Resources = resources,
            TotalResults = totalResults,
            StartIndex = startIndex,
            ItemsPerPage = resources.Count
        };
    }

    /// <summary>
    /// Creates a paged result from a query filter and full list.
    /// Applies pagination to the list.
    /// </summary>
    /// <param name="allResources">All available resources</param>
    /// <param name="filter">Query filter with pagination</param>
    public static PagedResult<T> FromFilter(
        IEnumerable<T> allResources,
        QueryFilter filter)
    {
        var resourceList = allResources.ToList();
        var totalResults = resourceList.Count;

        // Apply pagination (StartIndex is 1-based)
        var skip = Math.Max(0, filter.StartIndex - 1);
        var take = Math.Min(filter.Count, Math.Max(0, totalResults - skip));

        var pagedResources = resourceList
            .Skip(skip)
            .Take(take)
            .ToList();

        return new PagedResult<T>
        {
            Resources = pagedResources,
            TotalResults = totalResults,
            StartIndex = filter.StartIndex,
            ItemsPerPage = pagedResources.Count
        };
    }

    /// <summary>
    /// Maps the resources to a new type.
    /// </summary>
    /// <typeparam name="TResult">Target type</typeparam>
    /// <param name="selector">Mapping function</param>
    /// <returns>PagedResult with mapped resources</returns>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        return new PagedResult<TResult>
        {
            Resources = Resources.Select(selector).ToList(),
            TotalResults = TotalResults,
            StartIndex = StartIndex,
            ItemsPerPage = ItemsPerPage
        };
    }
}
