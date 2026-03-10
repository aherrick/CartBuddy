using System.Text.Json.Serialization;

namespace CartBuddy.Models;

public class KrogerTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class KrogerProductSearchResponse
{
    [JsonPropertyName("data")]
    public List<KrogerProduct> Data { get; set; }
}

public class KrogerProduct
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; }

    [JsonPropertyName("upc")]
    public string Upc { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("images")]
    public List<KrogerImage> Images { get; set; }

    [JsonPropertyName("items")]
    public List<KrogerProductItem> Items { get; set; }
}

public class KrogerImage
{
    [JsonPropertyName("sizes")]
    public List<KrogerImageSize> Sizes { get; set; }
}

public class KrogerImageSize
{
    [JsonPropertyName("size")]
    public string Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class KrogerProductItem
{
    [JsonPropertyName("price")]
    public KrogerPrice Price { get; set; }
}

public class KrogerPrice
{
    [JsonPropertyName("regular")]
    public decimal Regular { get; set; }
}

public class KrogerLocationSearchResponse
{
    [JsonPropertyName("data")]
    public List<KrogerLocation> Data { get; set; }
}

public class KrogerLocation
{
    [JsonPropertyName("locationId")]
    public string LocationId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("address")]
    public KrogerAddress Address { get; set; }
}

public class KrogerAddress
{
    [JsonPropertyName("addressLine1")]
    public string AddressLine1 { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("zipCode")]
    public string ZipCode { get; set; }
}