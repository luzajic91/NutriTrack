namespace NutriTrack.Shared.Models.Common;

/// <summary>A page of results returned by paginated API endpoints.</summary>
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
