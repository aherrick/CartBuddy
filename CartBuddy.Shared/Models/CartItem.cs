namespace CartBuddy.Shared.Models;

public class CartItem
{
    public string Upc { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ImageUrl { get; set; }
}