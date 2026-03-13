using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CartBuddy.Data.Models;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;

namespace CartBuddy.Data.Services;

public class KrogerClient(HttpClient httpClient, IConfiguration configuration)
{
    private static readonly Uri BaseUri = new("https://api.kroger.com/v1/");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly ResiliencePipeline<KrogerProductSearchPage> SearchProductsRetryPipeline =
        new ResiliencePipelineBuilder<KrogerProductSearchPage>()
            .AddRetry(
                new RetryStrategyOptions<KrogerProductSearchPage>
                {
                    MaxRetryAttempts = 4,
                    Delay = TimeSpan.FromMilliseconds(250),
                    ShouldHandle = args =>
                        ValueTask.FromResult(ShouldRetryForMissingPrice(args.Outcome.Result)),
                }
            )
            .Build();

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
        int limit = 10
    ) =>
        await SearchProductsRetryPipeline.ExecuteAsync(async _ =>
        {
            var token = await GetClientToken();
            // Kroger's Product API documents "ais" as the Available In Store fulfillment filter.
            var requestUri = new Uri(
                BaseUri,
                $"products?filter.term={Uri.EscapeDataString(term)}&filter.locationId={Uri.EscapeDataString(locationId)}&filter.fulfillment=ais&filter.limit={limit}&filter.start={start + 1}"
            );

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, _);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<KrogerProductResponse>(
                payload,
                JsonOptions
            );

            var page = new KrogerProductSearchPage
            {
                RawRequest = requestUri.ToString(),
                RawResponse = payload,
                TotalCount = apiResponse?.Meta?.Pagination?.Total ?? 0,
                Results = [],
            };

            if (apiResponse?.Data is null)
            {
                return page;
            }

            foreach (var item in apiResponse.Data)
            {
                if (MapProduct(term, item) is { } product)
                {
                    page.Results.Add(product);
                }
            }

            return page;
        });

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
        var apiResponse = JsonSerializer.Deserialize<KrogerLocationResponse>(payload, JsonOptions);

        if (apiResponse?.Data is null)
        {
            return [];
        }

        return apiResponse
            .Data.Select(loc => new KrogerLocation
            {
                LocationId = loc.LocationId ?? string.Empty,
                Name = loc.Name ?? string.Empty,
                Address = loc.Address is not null
                    ? new KrogerAddress
                    {
                        AddressLine1 = loc.Address.AddressLine1 ?? string.Empty,
                        City = loc.Address.City ?? string.Empty,
                        State = loc.Address.State ?? string.Empty,
                        ZipCode = loc.Address.ZipCode ?? string.Empty,
                    }
                    : new KrogerAddress(),
            })
            .ToList();
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

            clientToken =
                document.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
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

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(BaseUri, "connect/oauth2/token")
        );
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

    private static KrogerProduct MapProduct(string query, KrogerProductData item)
    {
        if (item.Items is not { Count: > 0 })
        {
            return null;
        }

        // Pick ONE canonical variant that has a price
        var variant = item.Items.FirstOrDefault(v => v.Price is not null);
        if (variant is null)
        {
            return null;  // No priced variant = skip this product
        }

        var regularPrice = variant.Price.Regular;
        var promoPrice = variant.Price.Promo;
        var hasPromo = promoPrice > 0m;
        var displayPrice = hasPromo ? promoPrice : regularPrice;

        if (displayPrice <= 0m)
        {
            return null;
        }

        var upc = item.Upc ?? variant.Upc;
        if (string.IsNullOrWhiteSpace(upc))
        {
            return null;
        }

        return new KrogerProduct
        {
            Query = query,
            ProductId = item.ProductId ?? string.Empty,
            Upc = upc,
            Description = item.Description ?? string.Empty,
            Brand = item.Brand ?? string.Empty,
            Size = variant.Size ?? string.Empty,
            ImageUrl = GetImageUrl(item.Images),
            Price = displayPrice,
            RegularPrice = regularPrice,
            HasPromo = hasPromo,
            PromoEndDate = hasPromo ? variant.Price.ExpirationDate?.Value ?? string.Empty : string.Empty,
        };
    }

    private static bool ShouldRetryForMissingPrice(KrogerProductSearchPage page)
    {
        if (page is null)
        {
            return false;
        }

        // Retry when the API returned data but MapProduct filtered everything out (all priceless)
        return page.TotalCount > 0 && page.Results.Count == 0;
    }

    private static string GetImageUrl(List<KrogerImage> images)
    {
        if (images is not { Count: > 0 })
        {
            return string.Empty;
        }

        var frontImage = images.FirstOrDefault(img =>
            string.Equals(img.Perspective, "front", StringComparison.OrdinalIgnoreCase)
        );
        return GetPreferredSizeUrl(frontImage ?? images[0]);
    }

    private static string GetPreferredSizeUrl(KrogerImage image)
    {
        if (image?.Sizes is not { Count: > 0 })
        {
            return string.Empty;
        }

        var medium = image.Sizes.FirstOrDefault(s =>
            string.Equals(s.Size, "medium", StringComparison.OrdinalIgnoreCase)
        );
        return (medium ?? image.Sizes[0]).Url ?? string.Empty;
    }
}
