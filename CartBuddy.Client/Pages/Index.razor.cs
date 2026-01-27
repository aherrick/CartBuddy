using Blazored.LocalStorage;
using CartBuddy.Client.Services;
using CartBuddy.Shared.Models;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CartBuddy.Client.Pages;

public partial class Index
{
    [Inject]
    private IJSRuntime JS { get; set; }

    [Inject]
    private ApiService Api { get; set; }

    [Inject]
    private NavigationManager Nav { get; set; }

    [Inject]
    private ILocalStorageService LocalStorage { get; set; }

    [Inject]
    private SweetAlertService Swal { get; set; }

    private string zipCode = "";
    private string itemsText = "";
    private string locationId = "";
    private string status = "";
    private List<LocationInfo> locations = [];
    private readonly List<TermSearchResult> searchResults = [];
    private readonly List<CartItem> cart = [];
    private LocationInfo selectedStore = null;
    private bool showLocationSearch = true;
    private readonly Dictionary<string, int> termOffsets = [];
    private readonly Dictionary<string, bool> termCollapsed = [];
    private bool allCollapsed = true;

    protected override async Task OnInitializedAsync()
    {
        var stored = await LocalStorage.GetItemAsync<LocationInfo>("selectedStore");
        if (stored != null)
        {
            selectedStore = stored;
            locationId = stored.LocationId;
            showLocationSearch = false;
            status = "Store loaded from storage";
        }

        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        if (uri.Query.Contains("success=true"))
        {
            var result = await Swal.FireAsync(new SweetAlertOptions
            {
                Title = "Cart created!",
                Icon = SweetAlertIcon.Success,
                Text = "Your items were added to your Kroger cart.",
                ShowCancelButton = true,
                ConfirmButtonText = "🛒 View on Kroger.com",
                CancelButtonText = "Continue shopping",
                ConfirmButtonColor = "#0033A0",
                CancelButtonColor = "#6c757d"
            });

            if (result.IsConfirmed)
            {
                await JS.InvokeVoidAsync("open", "https://www.kroger.com/cart", "_blank");
                Nav.NavigateTo("/", replace: true);
            }
            else
            {
                Nav.NavigateTo("/", replace: true);
            }
        }
    }

    private int TotalQuantity => cart.Sum(c => c.Quantity);
    private decimal TotalPrice => cart.Sum(c => c.Price * c.Quantity);

    private bool IsTermFulfilled(string term)
    {
        var termResult = searchResults.FirstOrDefault(t => t.Term == term);
        if (termResult == null)
            return false;

        var productUpcs = termResult.Results.Select(p => p.Upc).ToList();
        return cart.Any(c => productUpcs.Contains(c.Upc));
    }

    private async Task SetLocation()
    {
        status = "Getting locations...";
        var response = await Api.GetAsync<LocationResponse>($"/api/location/{zipCode}");
        if (response != null)
        {
            locations = response.Locations;
            status = $"Found {locations.Count} stores";
        }
        else
        {
            status = "Error getting locations";
        }
    }

    private async Task SelectLocation(string locId)
    {
        locationId = locId;
        selectedStore = locations.FirstOrDefault(l => l.LocationId == locId);
        if (selectedStore != null)
        {
            await LocalStorage.SetItemAsync("selectedStore", selectedStore);
            showLocationSearch = false;
        }
        status = $"Store selected";
    }

    private void ChangeStore()
    {
        showLocationSearch = true;
        locations.Clear();
        zipCode = "";
    }

    private async Task SearchItems()
    {
        status = "Searching...";
        searchResults.Clear();
        termOffsets.Clear();
        termCollapsed.Clear();

        var terms = itemsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var isFirst = true;
        foreach (var term in terms)
        {
            var trimmedTerm = term.Trim();
            var results = await Api.GetAsync<List<ProductSearchResult>>(
                $"/api/search?locationId={locationId}&term={Uri.EscapeDataString(trimmedTerm)}&start=0&limit=5"
            );

            searchResults.Add(new TermSearchResult { Term = trimmedTerm, Results = results ?? [] });
            termOffsets[trimmedTerm] = 5;
            termCollapsed[trimmedTerm] = !isFirst;
            isFirst = false;
        }
        status = $"Found results for {searchResults.Count} terms";
    }

    private async Task LoadMore(string term)
    {
        var termResult = searchResults.FirstOrDefault(t => t.Term == term);
        if (termResult != null)
        {
            var currentOffset = termOffsets.GetValueOrDefault(term, 5);
            var nextItems = await Api.GetAsync<List<ProductSearchResult>>(
                $"/api/search?locationId={locationId}&term={Uri.EscapeDataString(term)}&start={currentOffset}&limit=5"
            );

            if (nextItems != null && nextItems.Count != 0)
            {
                termResult.Results.AddRange(nextItems);
                termOffsets[term] = currentOffset + 5;
            }
        }
    }

    private void ToggleAllCollapse()
    {
        allCollapsed = !allCollapsed;
        foreach (var term in termCollapsed.Keys.ToList())
        {
            termCollapsed[term] = allCollapsed;
        }
    }

    private void ToggleCollapse(string term)
    {
        if (termCollapsed.TryGetValue(term, out var isCollapsed))
        {
            termCollapsed[term] = !isCollapsed;
        }
        else
        {
            termCollapsed[term] = true;
        }
    }

    private void AddToCart(ProductSearchResult product, string term)
    {
        var existing = cart.FirstOrDefault(c => c.Upc == product.Upc);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            cart.Add(
                new CartItem
                {
                    Upc = product.Upc,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl,
                }
            );
        }

        // Auto-collapse the term section
        termCollapsed[term] = true;
    }

    private static void IncreaseQuantity(CartItem item) => item.Quantity++;

    private void DecreaseQuantity(CartItem item)
    {
        item.Quantity--;
        if (item.Quantity <= 0)
            cart.Remove(item);
    }

    private async Task Checkout()
    {
        status = "Starting checkout...";
        var result = await Api.PostAsync<CheckoutRequest, CheckoutResponse>(
            "/api/checkout",
            new CheckoutRequest { LocationId = locationId, Items = cart }
        );
        if (result != null)
        {
            Nav.NavigateTo(result.AuthUrl, true);
        }
        else
        {
            status = "Error starting checkout";
        }
    }

    private class TermSearchResult
    {
        public string Term { get; set; }
        public List<ProductSearchResult> Results { get; set; }
    }
}