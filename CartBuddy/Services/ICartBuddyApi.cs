using CartBuddy.Shared.Models;
using Refit;

namespace CartBuddy.Services;

public interface ICartBuddyApi
{
    [Get("/api/location/{zipCode}")]
    Task<LocationResponse> SearchLocations(string zipCode);

    [Get("/api/search")]
    Task<ProductSearchResponse> SearchProducts(string locationId, string term, int start = 0, int limit = 4);

    [Post("/api/cleanup")]
    Task<CleanupResponse> CleanupList([Body] CleanupRequest request);

    [Post("/api/checkout")]
    Task<CheckoutResponse> Checkout(CheckoutRequest request);
}
