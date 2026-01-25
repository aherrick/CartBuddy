namespace CartBuddy.Server.Models;

public class PendingCheckout
{
    public string State { get; set; }
    public string LocationId { get; set; }
    public string Upc { get; set; }
    public int Quantity { get; set; }
    public string Modality { get; set; }
}