namespace CartBuddy.Shared.Models;

public class CheckoutRequest
{
    public string LocationId { get; set; }
    public List<CartItem> Items { get; set; }
    public string ReturnUri { get; set; }
}