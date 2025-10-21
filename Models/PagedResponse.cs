namespace nekohub_core.Models;

public class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
