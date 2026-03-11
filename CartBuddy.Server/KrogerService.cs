using CartBuddy.Data.Models;
using CartBuddy.Data.Services;
using CartBuddy.Shared.Models;

namespace CartBuddy.Server;

public class KrogerService(KrogerClient krogerClient, ApiLogger apiLogger)
{
    public async Task<List<LocationInfo>> GetLocationsByZip(string zipCode)
    {
        apiLogger.Log(nameof(GetLocationsByZip), "Request", new { zipCode });
        var locations = await krogerClient.SearchLocations(zipCode, 5);
        var result = locations.Select(MapLocationInfo).ToList();
        apiLogger.Log(nameof(GetLocationsByZip), "Response", result);
        return result;
    }

    public async Task<ProductSearchResponse> SearchProducts(
        string locationId,
        string term,
        int start = 0,
        int limit = 5
    )
    {
        var page = await krogerClient.SearchProducts(term, locationId, start, limit);
        apiLogger.Log(nameof(SearchProducts), "Kroger Request", page.RawRequest);
        apiLogger.Log(nameof(SearchProducts), "Kroger Response", page.RawResponse);

        var response = new ProductSearchResponse
        {
            Results = [.. page.Results.Select(MapProductSearchResult)],
            Total = page.TotalCount,
        };
        apiLogger.Log(nameof(SearchProducts), "Response", response);
        return response;
    }

    /// <summary>
    /// Exchanges an OAuth authorization code for a user access token.
    /// This token represents the authenticated user and has the scopes they approved.
    /// </summary>
    public async Task<string> ExchangeCodeForToken(string code, string redirectUri)
        => await krogerClient.ExchangeCodeForToken(code, redirectUri);

    public string CreateAuthorizationUrl(string redirectUri, string scopes, string state)
        => krogerClient.CreateAuthorizationUrl(redirectUri, scopes, state);

    /// <summary>
    /// Adds items to the authenticated user's Kroger cart using the Cart API.
    /// Requires a user access token with the cart.basic:write scope.
    /// Uses PUT /v1/cart/add (the public Cart API endpoint).
    /// </summary>
    public async Task<string> CreateCart(string userToken, List<CartItem> items)
    {
        apiLogger.Log(nameof(CreateCart), "Request", new { itemCount = items.Count, items });
        await krogerClient.AddToCart(
            userToken,
            items.Select(item => new KrogerCartItem { Upc = item.Upc, Quantity = item.Quantity })
        );
        apiLogger.Log(nameof(CreateCart), "Response", new { result = "success" });
        return "success";
    }

    private static LocationInfo MapLocationInfo(KrogerLocation location)
    {
        var address = location.Address;
        var addressText = address is null
            ? string.Empty
            : $"{address.AddressLine1}, {address.City}, {address.State}";

        return new LocationInfo
        {
            LocationId = location.LocationId,
            Name = location.Name,
            Address = addressText,
        };
    }

    private static ProductSearchResult MapProductSearchResult(KrogerProduct product)
    {
        return new ProductSearchResult
        {
            ProductId = product.ProductId,
            Upc = product.Upc,
            Description = product.Description,
            Brand = string.IsNullOrWhiteSpace(product.Brand) ? string.Empty : product.Brand,
            Size = string.IsNullOrWhiteSpace(product.Size) ? string.Empty : product.Size,
            Price = product.Price,
            RegularPrice = product.HasPromo ? product.RegularPrice : null,
            HasPromo = product.HasPromo,
            PromoEndDate = product.PromoEndDate,
            ImageUrl = product.ImageUrl,
        };
    }
}