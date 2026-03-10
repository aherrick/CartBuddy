using CartBuddy.Data.Models;
using CartBuddy.Data.Services;
using CartBuddy.Models;

namespace CartBuddy.Services;

public class KrogerApiService(KrogerClient krogerClient)
{
    public async Task<ProductSearchPage> SearchProducts(
        string term,
        string locationId,
        int start = 0,
        int limit = 4
    )
    {
        var page = await krogerClient.SearchProducts(term, locationId, start, limit);
        return new ProductSearchPage([.. page.Results.Select(MapProductMatch)], page.TotalCount);
    }

    public Task<List<KrogerLocation>> SearchLocations(string zipCode) =>
        krogerClient.SearchLocations(zipCode);

    // Opens Kroger's login page in the platform browser via WebAuthenticator.
    // MAUI intercepts the cartbuddy:// callback and returns the auth code.
    // We exchange it through the shared data client for a user token with cart.basic:write scope.
    public async Task<string> AuthenticateUser()
    {
        var authUrl = new Uri(
            krogerClient.CreateAuthorizationUrl(Constants.KrogerRedirectUri, "cart.basic:write")
        );

        var result = await WebAuthenticator.Default.AuthenticateAsync(
            authUrl,
            new Uri(Constants.KrogerRedirectUri)
        );

        var code = result.Properties["code"];
        return await krogerClient.ExchangeCodeForToken(code, Constants.KrogerRedirectUri);
    }

    public async Task AddToCart(string userToken, List<CartLine> items)
    {
        await krogerClient.AddToCart(
            userToken,
            items
                .Where(item => !string.IsNullOrWhiteSpace(item.Upc))
                .Select(item => new KrogerCartItem { Upc = item.Upc, Quantity = item.Quantity })
        );
    }

    private static ProductMatch MapProductMatch(KrogerProduct product)
    {
        return new ProductMatch
        {
            Query = product.Query,
            ProductId = product.ProductId,
            Upc = product.Upc,
            Description = product.Description,
            Brand = string.IsNullOrWhiteSpace(product.Brand) ? "Unknown" : product.Brand,
            Size = string.IsNullOrWhiteSpace(product.Size) ? "N/A" : product.Size,
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            RegularPrice = product.RegularPrice,
            HasPromo = product.HasPromo,
            PromoEndDate = product.PromoEndDate,
        };
    }
}