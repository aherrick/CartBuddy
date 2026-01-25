using System.Text;
using System.Text.Json;
using CartBuddy.Shared.Models;

namespace CartBuddy.Server;

public class KrogerService(
    HttpClient httpClient,
    IConfiguration configuration,
    KrogerTokenCache tokenCache
)
{
    public async Task<List<LocationInfo>> GetLocationsByZip(string zipCode)
    {
        var token = await tokenCache.GetClientTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await httpClient.GetAsync(
            $"locations?filter.zipCode.near={zipCode}&filter.limit=5"
        );
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        var locations = new List<LocationInfo>();
        foreach (var loc in data.EnumerateArray())
        {
            var address = loc.GetProperty("address");
            locations.Add(
                new LocationInfo
                {
                    LocationId = loc.GetProperty("locationId").GetString(),
                    Name = loc.GetProperty("name").GetString(),
                    Address =
                        $"{address.GetProperty("addressLine1").GetString()}, {address.GetProperty("city").GetString()}, {address.GetProperty("state").GetString()}",
                }
            );
        }

        return locations;
    }

    public async Task<List<ProductSearchResult>> SearchProducts(
        string locationId,
        string term,
        int start = 0,
        int limit = 5
    )
    {
        var token = await tokenCache.GetClientTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await httpClient.GetAsync(
            $"products?filter.term={Uri.EscapeDataString(term)}&filter.locationId={locationId}&filter.limit={limit}&filter.start={start + 1}"
        );
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");

        var results = new List<ProductSearchResult>();
        foreach (var item in data.EnumerateArray())
        {
            var firstItem = item.GetProperty("items")[0];
            var priceElement = firstItem.GetProperty("price");
            var hasPromo =
                priceElement.TryGetProperty("promo", out var promoPrice)
                && promoPrice.ValueKind != JsonValueKind.Null;
            var regular = priceElement.GetProperty("regular").GetDecimal();
            var price = hasPromo ? promoPrice.GetDecimal() : regular;

            var promoEndDate = "";
            if (hasPromo && priceElement.TryGetProperty("expirationDate", out var expDate))
            {
                promoEndDate = expDate.TryGetProperty("value", out var val) ? val.GetString() : "";
            }

            var imageUrl = "";
            if (item.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
            {
                var frontImage = images
                    .EnumerateArray()
                    .FirstOrDefault(i =>
                        i.TryGetProperty("perspective", out var p) && p.GetString() == "front"
                    );
                if (frontImage.ValueKind != JsonValueKind.Undefined)
                {
                    imageUrl =
                        frontImage
                            .GetProperty("sizes")
                            .EnumerateArray()
                            .FirstOrDefault(s =>
                                s.TryGetProperty("size", out var sz) && sz.GetString() == "medium"
                            )
                            .GetProperty("url")
                            .GetString() ?? "";
                }
                if (string.IsNullOrEmpty(imageUrl))
                {
                    imageUrl = images[0]
                        .GetProperty("sizes")
                        .EnumerateArray()
                        .FirstOrDefault()
                        .GetProperty("url")
                        .GetString();
                }
            }

            results.Add(
                new ProductSearchResult
                {
                    ProductId = item.GetProperty("productId").GetString(),
                    Upc = item.GetProperty("upc").GetString(),
                    Description = item.GetProperty("description").GetString(),
                    Brand = item.TryGetProperty("brand", out var brand)
                        ? brand.GetString()
                        : "Unknown",
                    Size = firstItem.TryGetProperty("size", out var size)
                        ? size.GetString()
                        : "N/A",
                    Price = price,
                    RegularPrice = hasPromo ? regular : null,
                    HasPromo = hasPromo,
                    PromoEndDate = promoEndDate,
                    ImageUrl = imageUrl,
                }
            );
        }

        return results;
    }

    public async Task<string> ExchangeCodeForToken(string code, string redirectUri)
    {
        var clientId = configuration["Kroger:ClientId"];
        var clientSecret = configuration["Kroger:ClientSecret"];
        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}")
        );

        HttpRequestMessage request = new(HttpMethod.Post, "connect/oauth2/token");
        request.Headers.Authorization = new("Basic", credentials);
        request.Content = new FormUrlEncodedContent(
            [
                new("grant_type", "authorization_code"),
                new("code", code),
                new("redirect_uri", redirectUri),
            ]
        );

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString();
    }

    public async Task AddToCart(string userToken, List<CartItem> items)
    {
        HttpRequestMessage request = new(HttpMethod.Put, "cart/add");
        request.Headers.Authorization = new("Bearer", userToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(
                new { items = items.Select(i => new { upc = i.Upc, quantity = i.Quantity }) }
            ),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}