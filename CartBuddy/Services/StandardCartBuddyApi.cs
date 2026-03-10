using System.Net.Http.Json;
using CartBuddy.Shared.Models;

namespace CartBuddy.Services;

public class StandardCartBuddyApi(HttpClient httpClient) : ICartBuddyApi
{
    public async Task<LocationResponse> SearchLocations(string zipCode)
    {
        var result = await httpClient.GetFromJsonAsync<LocationResponse>($"/api/location/{zipCode}");
        return result ?? new LocationResponse { Locations = [] };
    }

    public async Task<ProductSearchResponse> SearchProducts(string locationId, string term, int start = 0, int limit = 4)
    {
        var result = await httpClient.GetFromJsonAsync<ProductSearchResponse>($"/api/search?locationId={locationId}&term={term}&start={start}&limit={limit}");
        return result ?? new ProductSearchResponse { Results = [], Total = 0 };
    }

    public async Task<CheckoutResponse> Checkout(CheckoutRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("/api/checkout", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        return result ?? new CheckoutResponse { AuthUrl = string.Empty };
    }
}

