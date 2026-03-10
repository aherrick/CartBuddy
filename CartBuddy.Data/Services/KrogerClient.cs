using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CartBuddy.Data.Models;
using Microsoft.Extensions.Configuration;

namespace CartBuddy.Data.Services;

public class KrogerClient(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly Uri BaseUri = new("https://api.kroger.com/v1/");
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    private string clientToken = string.Empty;
    private DateTime clientTokenExpiry = DateTime.MinValue;

    public string CreateAuthorizationUrl(string redirectUri, string scopes, string state = "")
    {
        List<string> query =
        [
            $"scope={Uri.EscapeDataString(scopes)}",
            "response_type=code",
            $"client_id={Uri.EscapeDataString(GetRequiredSetting("Kroger:ClientId"))}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
        ];

        if (!string.IsNullOrWhiteSpace(state))
        {
            query.Add($"state={Uri.EscapeDataString(state)}");
        }

        return new Uri(BaseUri, $"connect/oauth2/authorize?{string.Join("&", query)}").ToString();
    }

    public async Task<string> ExchangeCodeForToken(string code, string redirectUri)
    {
        using var request = CreateAuthorizedTokenRequest(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
            }
        );

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
    }

    public async Task<KrogerProductSearchPage> SearchProducts(
        string term,
        string locationId,
        int start = 0,
        int limit = 4
    )
    {
        var token = await GetClientToken();
        var requestUri = new Uri(
            BaseUri,
            $"products?filter.term={Uri.EscapeDataString(term)}&filter.locationId={Uri.EscapeDataString(locationId)}&filter.limit={limit}&filter.start={start + 1}"
        );

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        var page = new KrogerProductSearchPage
        {
            Results = []
        };
        if (
            document.RootElement.TryGetProperty("meta", out var meta)
            && meta.TryGetProperty("pagination", out var pagination)
            && pagination.TryGetProperty("total", out var totalElement)
        )
        {
            page.TotalCount = totalElement.GetInt32();
        }

        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            return page;
        }

        foreach (var item in data.EnumerateArray())
        {
            if (TryCreateProduct(term, item, out var product))
            {
                page.Results.Add(product);
            }
        }

        return page;
    }

    public async Task<List<KrogerLocation>> SearchLocations(string zipCode, int limit = 10)
    {
        var token = await GetClientToken();
        var requestUri = new Uri(
            BaseUri,
            $"locations?filter.zipCode.near={Uri.EscapeDataString(zipCode)}&filter.limit={limit}"
        );

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);

        List<KrogerLocation> locations = [];
        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            return locations;
        }

        foreach (var location in data.EnumerateArray())
        {
            locations.Add(
                new KrogerLocation
                {
                    LocationId = GetString(location, "locationId"),
                    Name = GetString(location, "name"),
                    Address = location.TryGetProperty("address", out var address)
                        ? new KrogerAddress
                        {
                            AddressLine1 = GetString(address, "addressLine1"),
                            City = GetString(address, "city"),
                            State = GetString(address, "state"),
                            ZipCode = GetString(address, "zipCode"),
                        }
                        : new KrogerAddress(),
                }
            );
        }

        return locations;
    }

    public async Task AddToCart(string userToken, IEnumerable<KrogerCartItem> items)
    {
        var cartItems = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Upc) && item.Quantity > 0)
            .Select(item => new { upc = item.Upc, quantity = item.Quantity })
            .ToList();

        if (cartItems.Count == 0)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(BaseUri, "cart/add"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { items = cartItems }),
            Encoding.UTF8,
            "application/json"
        );

        using var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Kroger cart/add failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}",
                null,
                response.StatusCode
            );
        }
    }

    private async Task<string> GetClientToken()
    {
        if (!string.IsNullOrWhiteSpace(clientToken) && DateTime.UtcNow < clientTokenExpiry)
        {
            return clientToken;
        }

        await tokenLock.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(clientToken) && DateTime.UtcNow < clientTokenExpiry)
            {
                return clientToken;
            }

            using var request = CreateAuthorizedTokenRequest(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["scope"] = "product.compact",
                }
            );

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(payload);

            clientToken = document.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
            var expiresIn = document.RootElement.GetProperty("expires_in").GetInt32();
            clientTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);
            return clientToken;
        }
        finally
        {
            tokenLock.Release();
        }
    }

    private HttpRequestMessage CreateAuthorizedTokenRequest(
        IReadOnlyDictionary<string, string> formValues
    )
    {
        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes(
                $"{GetRequiredSetting("Kroger:ClientId")}:{GetRequiredSetting("Kroger:ClientSecret")}"
            )
        );

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(BaseUri, "connect/oauth2/token"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(formValues);
        return request;
    }

    private string GetRequiredSetting(string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing configuration value '{key}'.");
        }

        return value;
    }

    private static bool TryCreateProduct(string query, JsonElement item, out KrogerProduct product)
    {
        product = null;

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
        var hasPromo =
            priceElement.TryGetProperty("promo", out var promoPriceElement)
            && promoPriceElement.ValueKind != JsonValueKind.Null;

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

        product = new KrogerProduct
        {
            Query = query,
            ProductId = GetString(item, "productId"),
            Upc = GetString(item, "upc"),
            Description = GetString(item, "description"),
            Brand = GetString(item, "brand"),
            Size = GetString(firstItem, "size"),
            ImageUrl = GetImageUrl(item),
            Price = hasPromo ? promoPriceElement.GetDecimal() : regularPrice,
            RegularPrice = regularPrice,
            HasPromo = hasPromo,
            PromoEndDate = promoEndDate,
        };

        return !string.IsNullOrWhiteSpace(product.Upc);
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