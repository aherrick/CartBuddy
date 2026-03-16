using CartBuddy.Data.Models;
using CartBuddy.Data.Services;
using CartBuddy.Shared.Models;

namespace CartBuddy.Server;

public class KrogerService(KrogerClient krogerClient, ApiLogger apiLogger)
{
    public async Task<List<LocationInfo>> GetLocationsByZip(string zipCode)
    {
        var transactionId = Guid.NewGuid();
        apiLogger.Log(nameof(GetLocationsByZip), ApiLogDirection.Request, new { zipCode }, transactionId);
        var locations = await krogerClient.SearchLocations(zipCode, 5);
        var result = locations.Select(MapLocationInfo).ToList();
        apiLogger.Log(nameof(GetLocationsByZip), ApiLogDirection.Response, result, transactionId);
        return result;
    }

    public async Task<ProductSearchResponse> SearchProducts(
        string locationId,
        string term,
        int start = 0,
        int limit = 10
    )
    {
        var transactionId = Guid.NewGuid();
        var page = await krogerClient.SearchProducts(term, locationId, start, limit);
        apiLogger.Log(nameof(SearchProducts), ApiLogDirection.KrogerRequest, page.RawRequest, transactionId);
        apiLogger.Log(nameof(SearchProducts), ApiLogDirection.KrogerResponse, page.RawResponse, transactionId);

        var response = new ProductSearchResponse
        {
            Results = [.. page.Results.Select(MapProductSearchResult)],
            Total = page.TotalCount,
        };
        apiLogger.Log(nameof(SearchProducts), ApiLogDirection.Response, response, transactionId);
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
        var transactionId = Guid.NewGuid();
        apiLogger.Log(nameof(CreateCart), ApiLogDirection.Request, new { itemCount = items.Count, items }, transactionId);
        await krogerClient.AddToCart(
            userToken,
            items.Select(item => new KrogerCartItem { Upc = item.Upc, Quantity = item.Quantity })
        );
        apiLogger.Log(nameof(CreateCart), ApiLogDirection.Response, new { result = "success" }, transactionId);
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
            SoldByWeight = product.SoldByWeight,
        };
    }
}
