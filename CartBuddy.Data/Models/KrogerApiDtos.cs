using System.Text.Json.Serialization;

namespace CartBuddy.Data.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KrogerStockLevel
{
    [JsonStringEnumMemberName("HIGH")]
    High,
    [JsonStringEnumMemberName("LOW")]
    Low,
    [JsonStringEnumMemberName("TEMPORARILY_OUT_OF_STOCK")]
    TemporarilyOutOfStock,
}

public class KrogerProductResponse
{
    [JsonPropertyName("data")]
    public List<KrogerProductData> Data { get; set; }

    [JsonPropertyName("meta")]
    public KrogerMeta Meta { get; set; }
}

public class KrogerMeta
{
    [JsonPropertyName("pagination")]
    public KrogerPagination Pagination { get; set; }
}

public class KrogerPagination
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class KrogerProductData
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; }

    [JsonPropertyName("upc")]
    public string Upc { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("brand")]
    public string Brand { get; set; }

    [JsonPropertyName("images")]
    public List<KrogerImage> Images { get; set; }

    [JsonPropertyName("items")]
    public List<KrogerItemVariant> Items { get; set; }

    [JsonPropertyName("itemInformation")]
    public KrogerItemInformation ItemInformation { get; set; }
}

public class KrogerItemInformation
{
    [JsonPropertyName("averageWeightPerUnit")]
    public string AverageWeightPerUnit { get; set; }
}

public class KrogerImage
{
    [JsonPropertyName("perspective")]
    public string Perspective { get; set; }

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

public class KrogerItemVariant
{
    [JsonPropertyName("upc")]
    public string Upc { get; set; }

    [JsonPropertyName("size")]
    public string Size { get; set; }

    [JsonPropertyName("soldBy")]
    public string SoldBy { get; set; }

    [JsonPropertyName("price")]
    public KrogerPrice Price { get; set; }

    [JsonPropertyName("inventory")]
    public KrogerInventory Inventory { get; set; }
}

public class KrogerInventory
{
    [JsonPropertyName("stockLevel")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public KrogerStockLevel StockLevel { get; set; }
}

public class KrogerPrice
{
    [JsonPropertyName("regular")]
    public decimal Regular { get; set; }

    [JsonPropertyName("promo")]
    public decimal Promo { get; set; }

    [JsonPropertyName("expirationDate")]
    public KrogerExpirationDate ExpirationDate { get; set; }
}

public class KrogerExpirationDate
{
    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class KrogerLocationResponse
{
    [JsonPropertyName("data")]
    public List<KrogerLocationData> Data { get; set; }
}

public class KrogerLocationData
{
    [JsonPropertyName("locationId")]
    public string LocationId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("address")]
    public KrogerAddressData Address { get; set; }
}

public class KrogerAddressData
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