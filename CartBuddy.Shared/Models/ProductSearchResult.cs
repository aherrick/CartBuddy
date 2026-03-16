namespace CartBuddy.Shared.Models;

public class ProductSearchResult
{
    public string ProductId { get; set; }
    public string Upc { get; set; }
    public string Description { get; set; }
    public string Brand { get; set; }
    public string Size { get; set; }
    public decimal Price { get; set; }
    public decimal? RegularPrice { get; set; }
    public bool HasPromo { get; set; }
    public string PromoEndDate { get; set; }
    public string ImageUrl { get; set; }
    public bool SoldByWeight { get; set; }
}