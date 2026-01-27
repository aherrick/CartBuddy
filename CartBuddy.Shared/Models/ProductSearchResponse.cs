namespace CartBuddy.Shared.Models;

public class ProductSearchResponse
{
    public List<ProductSearchResult> Results { get; set; }
    public int Total { get; set; }
}
