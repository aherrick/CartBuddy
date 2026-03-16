namespace CartBuddy.Data.Models;

public class KrogerProduct
{
    public string Query { get; set; }
    public string ProductId { get; set; }
    public string Upc { get; set; }
    public string Description { get; set; }
    public string Brand { get; set; }
    public string Size { get; set; }
    public string ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal RegularPrice { get; set; }
    public bool HasPromo { get; set; }
    public string PromoEndDate { get; set; }
    public bool SoldByWeight { get; set; }
}

public class KrogerProductSearchPage
{
    public List<KrogerProduct> Results { get; set; }
    public int TotalCount { get; set; }
    public string RawRequest { get; set; }
    public string RawResponse { get; set; }
}

public class KrogerLocation
{
    public string LocationId { get; set; }
    public string Name { get; set; }
    public KrogerAddress Address { get; set; }
}

public class KrogerAddress
{
    public string AddressLine1 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}

public class KrogerCartItem
{
    public string Upc { get; set; }
    public int Quantity { get; set; }
}