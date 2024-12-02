// PagedResponse.cs
public class PagedResponse<T>
{
    public PagedResponse()
    {
        Data = new List<T>();  // Initialize in constructor
    }

    public IEnumerable<T> Data { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}