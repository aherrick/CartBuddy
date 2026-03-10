using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CartBuddy.Models;
using Microsoft.Extensions.Configuration;

namespace CartBuddy.Services;

public class KrogerApiService(HttpClient http, IConfiguration config)
{
    private string _clientToken;
    private DateTime _clientTokenExpiry;

    private async Task EnsureClientToken()
    {
        if (!string.IsNullOrEmpty(_clientToken) && DateTime.UtcNow < _clientTokenExpiry)
        {
            return;
        }

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{config["Kroger:ClientId"]}:{config["Kroger:ClientSecret"]}")
        );

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{Constants.KrogerBaseUrl}/connect/oauth2/token"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = "product.compact",
            }
        );

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<KrogerTokenResponse>(json);
        _clientToken = token.AccessToken;
        _clientTokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 60);
    }

    public async Task<ProductSearchPage> SearchProducts(
        string term,
        string locationId,
        int start = 0,
        int limit = 4
    )
    {
        await EnsureClientToken();

        var url =
            $"{Constants.KrogerBaseUrl}/products?filter.term={Uri.EscapeDataString(term)}"
            + $"&filter.locationId={Uri.EscapeDataString(locationId)}"
            + $"&filter.limit={limit}&filter.start={start + 1}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _clientToken);

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        var totalCount = 0;
        if (
            document.RootElement.TryGetProperty("meta", out var meta)
            && meta.TryGetProperty("pagination", out var pagination)
            && pagination.TryGetProperty("total", out var totalElement)
        )
        {
            totalCount = totalElement.GetInt32();
        }

        List<ProductMatch> results = [];
        if (document.RootElement.TryGetProperty("data", out var data))
        {
            foreach (var item in data.EnumerateArray())
            {
                if (TryCreateProductMatch(term, item, out var match))
                {
                    results.Add(match);
                }
            }
        }

        return new ProductSearchPage(results, totalCount);
    }

    public async Task<List<KrogerLocation>> SearchLocations(string zipCode)
    {
        await EnsureClientToken();

        var url =
            $"{Constants.KrogerBaseUrl}/locations?filter.zipCode.near={Uri.EscapeDataString(zipCode)}&filter.limit=10";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _clientToken);

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<KrogerLocationSearchResponse>(json);
        return result?.Data ?? [];
    }

    // Opens Kroger's login page in the platform browser via WebAuthenticator.
    // MAUI intercepts the cartbuddy:// callback and returns the auth code.
    // We exchange it here for a user token with cart.basic:write scope — no server needed.
    public async Task<string> AuthenticateUser()
    {
        var authUrl = new Uri(
            $"{Constants.KrogerBaseUrl}/connect/oauth2/authorize"
                + $"?client_id={Uri.EscapeDataString(config["Kroger:ClientId"])}"
                + $"&redirect_uri={Uri.EscapeDataString(Constants.KrogerRedirectUri)}"
                + $"&response_type=code"
                + $"&scope=cart.basic%3Awrite"
        );

        var result = await WebAuthenticator.Default.AuthenticateAsync(
            authUrl,
            new Uri(Constants.KrogerRedirectUri)
        );

        var code = result.Properties["code"];

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{config["Kroger:ClientId"]}:{config["Kroger:ClientSecret"]}")
        );

        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{Constants.KrogerBaseUrl}/connect/oauth2/token"
        );
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        tokenRequest.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = Constants.KrogerRedirectUri,
            }
        );

        var tokenResponse = await http.SendAsync(tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var json = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<KrogerTokenResponse>(json);
        return token.AccessToken;
    }

    public async Task AddToCart(string userToken, List<CartLine> items)
    {
        var cartItems = items
            .Where(i => !string.IsNullOrEmpty(i.Upc))
            .Select(i => new { upc = i.Upc, quantity = i.Quantity })
            .ToList();

        if (cartItems.Count == 0)
        {
            return;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{Constants.KrogerBaseUrl}/cart/add"
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { items = cartItems }),
            Encoding.UTF8,
            "application/json"
        );

        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Cart API failed ({(int)response.StatusCode}): {body}",
                null,
                response.StatusCode
            );
        }
    }

    private static bool TryCreateProductMatch(
        string query,
        JsonElement item,
        out ProductMatch match
    )
    {
        match = null;

        if (!item.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        {
            return false;
        }

        var firstItem = items[0];
        if (
            !firstItem.TryGetProperty("price", out var priceElement)
            || !priceElement.TryGetProperty("regular", out var regularPriceElement)
        )
        {
            return false;
        }

        var regularPrice = regularPriceElement.GetDecimal();
        var price = regularPrice;
        var hasPromo =
            priceElement.TryGetProperty("promo", out var promoPriceElement)
            && promoPriceElement.ValueKind != JsonValueKind.Null;

        if (hasPromo)
        {
            price = promoPriceElement.GetDecimal();
        }

        var promoEndDate = string.Empty;
        if (
            hasPromo
            && priceElement.TryGetProperty("expirationDate", out var expirationDateElement)
            && expirationDateElement.TryGetProperty("value", out var expirationValueElement)
            && expirationValueElement.ValueKind != JsonValueKind.Null
        )
        {
            promoEndDate = expirationValueElement.GetString() ?? string.Empty;
        }

        match = new ProductMatch
        {
            Query = query,
            ProductId = GetString(item, "productId"),
            Upc = GetString(item, "upc"),
            Description = GetString(item, "description"),
            Brand = GetString(item, "brand"),
            Size = GetString(firstItem, "size"),
            ImageUrl = GetImageUrl(item),
            Price = price,
            RegularPrice = regularPrice,
            HasPromo = hasPromo,
            PromoEndDate = promoEndDate,
        };

        if (string.IsNullOrWhiteSpace(match.Brand))
        {
            match.Brand = "Unknown";
        }

        if (string.IsNullOrWhiteSpace(match.Size))
        {
            match.Size = "N/A";
        }

        return !string.IsNullOrWhiteSpace(match.Upc);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (
            element.TryGetProperty(propertyName, out var value)
            && value.ValueKind != JsonValueKind.Null
        )
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string GetImageUrl(JsonElement item)
    {
        if (!item.TryGetProperty("images", out var images) || images.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        foreach (var image in images.EnumerateArray())
        {
            if (
                image.TryGetProperty("perspective", out var perspective)
                && perspective.GetString() == "front"
            )
            {
                var imageUrl = GetPreferredImageUrl(image);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    return imageUrl;
                }
            }
        }

        return GetPreferredImageUrl(images[0]);
    }

    private static string GetPreferredImageUrl(JsonElement image)
    {
        if (!image.TryGetProperty("sizes", out var sizes) || sizes.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        foreach (var size in sizes.EnumerateArray())
        {
            if (size.TryGetProperty("size", out var sizeName) && sizeName.GetString() == "medium")
            {
                return GetString(size, "url");
            }
        }

        return GetString(sizes[0], "url");
    }
}