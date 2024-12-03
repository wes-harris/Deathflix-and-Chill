// PaginationParameters.cs
public class PaginationParameters
{
    private int maxPageSize = 50;
    private int pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => pageSize;
        set => pageSize = value > maxPageSize ? maxPageSize : value;
    }
}

public class SortingParameters
{
    public string SortBy { get; set; } = "Name";  // default sort field
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

public class ActorParameters
{
    public PaginationParameters Pagination { get; set; } = new();
    public SortingParameters Sorting { get; set; } = new();
}