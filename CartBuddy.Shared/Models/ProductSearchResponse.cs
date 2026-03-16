namespace CartBuddy.Shared.Models;

public class ProductSearchRequest
{
    public string LocationId { get; set; }
    public CategoryItem Item { get; set; }
    public int Start { get; set; }
    public int Limit { get; set; }
}

public class ProductSearchResponse
{
    public List<ProductSearchResult> Results { get; set; }
    public int Total { get; set; }
}