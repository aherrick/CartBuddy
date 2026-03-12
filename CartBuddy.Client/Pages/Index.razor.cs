using Blazored.LocalStorage;
using Blazored.Toast.Services;
using CartBuddy.Client.Services;
using CartBuddy.Shared.Models;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CartBuddy.Client.Pages;

public partial class Index
{
    private const int SearchPageSize = 10;

    [Inject]
    private IJSRuntime JS { get; set; }

    [Inject]
    private ApiService Api { get; set; }

    [Inject]
    private NavigationManager Nav { get; set; }

    [Inject]
    private ILocalStorageService LocalStorage { get; set; }

    [Inject]
    private CartStorageService CartStorage { get; set; }

    [Inject]
    private DraftStorageService DraftStorage { get; set; }

    [Inject]
    private IToastService Toast { get; set; }

    [Inject]
    private SweetAlertService Swal { get; set; }

    private string zipCode = "";
    private string itemsText = "";
    private string locationId = "";
    private List<LocationInfo> locations = [];
    private readonly List<TermSearchResult> searchResults = [];
    private readonly List<CartItem> cart = [];
    private LocationInfo selectedStore = null;
    private bool showLocationSearch = true;
    private bool allCollapsed = true;
    private bool isLoadingLocations;
    private bool isSearching;
    private bool isCheckingOut;

    protected override async Task OnInitializedAsync()
    {
        zipCode = await DraftStorage.GetZipCodeAsync();
        itemsText = await DraftStorage.GetItemsTextAsync();

        var stored = await LocalStorage.GetItemAsync<LocationInfo>("selectedStore");
        if (stored != null)
        {
            selectedStore = stored;
            locationId = stored.LocationId;
            showLocationSearch = false;
            Toast.ShowInfo("Store loaded from storage");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadCartAsync();

            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            if (uri.Query.Contains("success=true"))
            {
                await ClearCart();

                var result = await Swal.FireAsync(
                    new SweetAlertOptions
                    {
                        Title = "Cart created!",
                        Icon = SweetAlertIcon.Success,
                        Text = "Your items were added to your Kroger cart.",
                        ShowCancelButton = true,
                        ConfirmButtonText = "🛒 View on Kroger.com",
                        CancelButtonText = "Continue shopping",
                        ConfirmButtonColor = "#0033A0",
                        CancelButtonColor = "#6c757d",
                    }
                );

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

            StateHasChanged();
        }
    }

    private int TotalQuantity => cart.Sum(c => c.Quantity);
    private decimal TotalPrice => cart.Sum(c => c.Price * c.Quantity);

    private bool IsTermFulfilled(TermSearchResult termResult)
    {
        var productUpcs = termResult.Results.Select(p => p.Upc).ToList();
        return cart.Any(c => productUpcs.Contains(c.Upc));
    }

    private async Task SetLocation()
    {
        if (isLoadingLocations)
        {
            return;
        }

        isLoadingLocations = true;

        try
        {
            var response = await Api.GetAsync<LocationResponse>($"/api/location/{zipCode}");
            if (response != null)
            {
                locations = response.Locations;
                Toast.ShowSuccess($"Found {locations.Count} stores");
            }
            else
            {
                Toast.ShowError("Error getting locations");
            }
        }
        finally
        {
            isLoadingLocations = false;
        }
    }

    private async Task SelectLocation(string locId)
    {
        if (!string.IsNullOrEmpty(locationId) && locationId != locId && cart.Count > 0)
        {
            var result = await Swal.FireAsync(
                new SweetAlertOptions
                {
                    Title = "Change stores?",
                    Text = "Changing stores will clear your current cart and search results.",
                    Icon = SweetAlertIcon.Warning,
                    ShowCancelButton = true,
                    ConfirmButtonText = "Change store",
                    CancelButtonText = "Keep current store",
                    ConfirmButtonColor = "#0033A0",
                    CancelButtonColor = "#6c757d",
                }
            );

            if (!result.IsConfirmed)
            {
                return;
            }

            await ClearCart(confirm: false, showToast: false);
            searchResults.Clear();
            Toast.ShowInfo("Cart cleared for the new store");
        }

        locationId = locId;
        selectedStore = locations.FirstOrDefault(l => l.LocationId == locId);
        if (selectedStore != null)
        {
            await LocalStorage.SetItemAsync("selectedStore", selectedStore);
            showLocationSearch = false;
        }
        Toast.ShowSuccess("Store selected");
    }

    private void ChangeStore()
    {
        showLocationSearch = true;
        locations.Clear();
        zipCode = "";
    }

    private async Task SearchItems()
    {
        if (isSearching)
        {
            return;
        }

        searchResults.Clear();

        var terms = itemsText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(term => term.Trim())
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .ToList();

        if (terms.Count == 0)
        {
            Toast.ShowInfo("Enter at least one item");
            return;
        }

        isSearching = true;

        try
        {
            var isFirst = true;
            foreach (var term in terms)
            {
                var response = await Api.GetAsync<ProductSearchResponse>(
                    $"/api/search?locationId={locationId}&term={Uri.EscapeDataString(term)}&start=0&limit={SearchPageSize}"
                );

                searchResults.Add(
                    new TermSearchResult
                    {
                        Term = term,
                        Results = response?.Results ?? [],
                        TotalAvailable = response?.Total ?? 0,
                        NextStart = SearchPageSize,
                        IsCollapsed = !isFirst,
                    }
                );

                isFirst = false;
            }
        }
        finally
        {
            isSearching = false;
        }

        var matchedTerms = searchResults.Count(result => result.Results.Count > 0);
        var missingTerms = searchResults.Count - matchedTerms;

        if (matchedTerms > 0)
        {
            Toast.ShowSuccess($"Found matches for {matchedTerms} items");
        }

        if (missingTerms > 0)
        {
            Toast.ShowWarning($"No matches for {missingTerms} items");
        }
    }

    private async Task LoadMore(TermSearchResult termResult)
    {
        var response = await Api.GetAsync<ProductSearchResponse>(
            $"/api/search?locationId={locationId}&term={Uri.EscapeDataString(termResult.Term)}&start={termResult.NextStart}&limit={SearchPageSize}"
        );

        if (response?.Results != null && response.Results.Count != 0)
        {
            termResult.Results.AddRange(response.Results);
        }

        termResult.NextStart += SearchPageSize;

        if (termResult.NextStart >= termResult.TotalAvailable)
        {
            Toast.ShowInfo("All items loaded");
        }
    }

    private void ToggleAllCollapse()
    {
        allCollapsed = !allCollapsed;
        foreach (var result in searchResults)
        {
            result.IsCollapsed = allCollapsed;
        }
    }

    private void ToggleCollapse(TermSearchResult termResult)
    {
        termResult.IsCollapsed = !termResult.IsCollapsed;
    }

    private async Task AddToCart(ProductSearchResult product, TermSearchResult termResult)
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
        termResult.IsCollapsed = true;

        await SaveCartAsync();
    }

    private async Task IncreaseQuantity(CartItem item)
    {
        item.Quantity++;
        await SaveCartAsync();
    }

    private async Task DecreaseQuantity(CartItem item)
    {
        item.Quantity--;
        if (item.Quantity <= 0)
        {
            cart.Remove(item);
        }

        await SaveCartAsync();
    }

    private async Task<bool> ClearCart(bool confirm = true, bool showToast = true)
    {
        if (confirm && cart.Count > 0)
        {
            var result = await Swal.FireAsync(
                new SweetAlertOptions
                {
                    Title = "Clear cart?",
                    Text = "This will remove all items from your cart.",
                    Icon = SweetAlertIcon.Warning,
                    ShowCancelButton = true,
                    ConfirmButtonText = "Clear cart",
                    CancelButtonText = "Cancel",
                    ConfirmButtonColor = "#dc3545",
                    CancelButtonColor = "#6c757d",
                }
            );

            if (!result.IsConfirmed)
            {
                return false;
            }
        }

        cart.Clear();
        await CartStorage.ClearCartAsync();
        if (showToast)
        {
            Toast.ShowInfo("Cart cleared");
        }

        return true;
    }

    private async Task PromptClearCart()
    {
        await ClearCart();
    }

    private async Task Checkout()
    {
        if (isCheckingOut)
        {
            return;
        }

        isCheckingOut = true;

        try
        {
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
                Toast.ShowError("Error starting checkout");
            }
        }
        finally
        {
            isCheckingOut = false;
        }
    }

    private async Task LoadCartAsync()
    {
        var storedCart = await CartStorage.GetCartAsync();
        if (storedCart == null || storedCart.Count == 0)
        {
            return;
        }

        cart.Clear();
        cart.AddRange(storedCart);
        Toast.ShowInfo("Cart restored");
    }

    private async Task SaveCartAsync()
    {
        await CartStorage.SaveCartAsync(cart);
    }

    private async Task PersistItemsTextAsync()
    {
        await DraftStorage.SaveItemsTextAsync(itemsText);
    }

    private async Task PersistZipCodeAsync()
    {
        await DraftStorage.SaveZipCodeAsync(zipCode);
    }

    private class TermSearchResult
    {
        public string Term { get; set; }
        public List<ProductSearchResult> Results { get; set; } = [];
        public int TotalAvailable { get; set; }
        public int NextStart { get; set; } = SearchPageSize;
        public bool IsCollapsed { get; set; }
    }
}
