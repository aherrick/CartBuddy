using CartBuddy.Shared.Models;
using Refit;

namespace CartBuddy.Services;

public interface ICartBuddyApi
{
    [Get("/api/location/{zipCode}")]
    Task<LocationResponse> SearchLocations(string zipCode);

    [Post("/api/search")]
    Task<ProductSearchResponse> SearchProducts([Body] ProductSearchRequest request);

    [Post("/api/cleanup")]
    Task<List<CategoryItem>> CleanupList([Body] CleanupRequest request);

    [Post("/api/checkout")]
    Task<CheckoutResponse> Checkout(CheckoutRequest request);

    [Get("/api/logs")]
    Task<List<ApiLogEntry>> GetLogs();
}
