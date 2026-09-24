namespace ResQ.Application.Common.Models;

/// <summary>A page of query results and its pagination metadata.</summary>
/// <typeparam name="T">Type of items in the page.</typeparam>
public class PagedResult<T>
{
    /// <summary>Items included on this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];
    /// <summary>Total number of matching items across all pages.</summary>
    public int TotalCount { get; init; }
    /// <summary>One-based current page number.</summary>
    public int PageNumber { get; init; }
    /// <summary>Maximum number of items requested per page.</summary>
    public int PageSize { get; init; }
    /// <summary>Total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    /// <summary>Whether a later page exists.</summary>
    public bool HasNextPage => PageNumber < TotalPages;
    /// <summary>Whether an earlier page exists.</summary>
    public bool HasPreviousPage => PageNumber > 1;
}
