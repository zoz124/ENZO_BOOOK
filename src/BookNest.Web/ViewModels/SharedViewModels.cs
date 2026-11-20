namespace BookNest.Web.ViewModels;

public sealed class PaginationInfo
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public string Action { get; set; } = "Index";
    public string? Controller { get; set; }
    public string? Area { get; set; }
    public Dictionary<string, string> RouteValues { get; set; } = new();
}
