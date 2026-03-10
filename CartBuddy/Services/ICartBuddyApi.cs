using CartBuddy.Shared.Models;

namespace CartBuddy.Services;

public interface ICartBuddyApi
{
    Task<LocationResponse> SearchLocations(string zipCode);

    Task<ProductSearchResponse> SearchProducts(string locationId, string term, int start = 0, int limit = 4);

    Task<CheckoutResponse> Checkout(CheckoutRequest request);
}
