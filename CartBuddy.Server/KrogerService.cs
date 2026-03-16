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
        List<LocationInfo> result = [.. locations.Select(MapLocationInfo)];
        apiLogger.Log(nameof(GetLocationsByZip), ApiLogDirection.Response, result, transactionId);
        return result;
    }

    public async Task<ProductSearchResponse> SearchProducts(ProductSearchRequest req)
    {
        var transactionId = Guid.NewGuid();
        var primaryPage = await krogerClient.SearchProducts(req.Item.Item, req.LocationId, req.Start, req.Limit);

        List<KrogerProduct> results;
        int total;

        if (string.Equals(req.Item.Category, "produce", StringComparison.OrdinalIgnoreCase))
        {
            var produceTerm = $"produce {req.Item.Item}";
            var producePage = await krogerClient.SearchProducts(produceTerm, req.LocationId, req.Start, req.Limit);

            apiLogger.Log(
                nameof(SearchProducts),
                ApiLogDirection.KrogerRequest,
                new { primary = primaryPage.RawRequest, produce = producePage.RawRequest },
                transactionId
            );
            apiLogger.Log(
                nameof(SearchProducts),
                ApiLogDirection.KrogerResponse,
                new { primary = primaryPage.RawResponse, produce = producePage.RawResponse },
                transactionId
            );

            results = MergeProducts(producePage.Results, primaryPage.Results);
            total = results.Count;
        }
        else
        {
            apiLogger.Log(
                nameof(SearchProducts),
                ApiLogDirection.KrogerRequest,
                primaryPage.RawRequest,
                transactionId
            );
            apiLogger.Log(
                nameof(SearchProducts),
                ApiLogDirection.KrogerResponse,
                primaryPage.RawResponse,
                transactionId
            );

            results = primaryPage.Results;
            total = primaryPage.TotalCount;
        }

        var response = new ProductSearchResponse
        {
            Results = [.. results.Select(MapProductSearchResult)],
            Total = total,
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
            AverageWeightPerUnit = product.AverageWeightPerUnit,
        };
    }

    private static List<KrogerProduct> MergeProducts(List<KrogerProduct> produce, List<KrogerProduct> primary)
        => [.. produce.Concat(primary).DistinctBy(p => p.Upc ?? p.ProductId ?? p.Description, StringComparer.OrdinalIgnoreCase)];
}
