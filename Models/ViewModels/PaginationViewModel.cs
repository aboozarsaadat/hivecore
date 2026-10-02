namespace HiveCore.Models.ViewModels;

/// <summary>
/// Generic pagination view model used by the _Pagination partial.
/// </summary>
public class PaginationViewModel
{
    /// <summary>Current 1-based page number.</summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>Total number of pages.</summary>
    public int TotalPages { get; set; } = 1;

    /// <summary>Total number of items across all pages (optional).</summary>
    public int TotalItems { get; set; }

    /// <summary>Page size (optional).</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>Base URL to which <c>?page=N</c> will be appended (may already contain a query string).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Optional CSS class applied to the container.</summary>
    public string? CssClass { get; set; }

    /// <summary>Optional label to render near the pagination control.</summary>
    public string? Label { get; set; }

    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
    public bool IsFirst => CurrentPage == 1;
    public bool IsLast => CurrentPage == TotalPages;

    /// <summary>Builds the URL for a specific page, respecting an existing query string.</summary>
    public string UrlFor(int page)
    {
        var sep = BaseUrl.Contains('?') ? '&' : '?';
        return $"{BaseUrl}{sep}page={page}";
    }
}