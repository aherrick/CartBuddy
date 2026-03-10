using CartBuddy.Shared.Models;

namespace CartBuddy.Services;

public class MockCartBuddyApi : ICartBuddyApi
{
    public Task<LocationResponse> SearchLocations(string zipCode)
    {
        return Task.FromResult(new LocationResponse { Locations = [] });
    }

    public Task<ProductSearchResponse> SearchProducts(string locationId, string term, int start = 0, int limit = 4)
    {
        return Task.FromResult(new ProductSearchResponse { Results = [], Total = 0 });
    }

    public Task<CheckoutResponse> Checkout(CheckoutRequest request)
    {
        return Task.FromResult(new CheckoutResponse { AuthUrl = string.Empty });
    }
}
