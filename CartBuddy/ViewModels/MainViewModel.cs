using System.Collections.ObjectModel;
using CartBuddy.Models;
using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CartBuddy.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int PageSize = 4;

    private readonly ICartBuddyApi _api;

    public MainViewModel(ICartBuddyApi api)
    {
        _api = api;
        SearchGroups.CollectionChanged += (_, _) => UpdateSearchState();
        CartItems.CollectionChanged += (_, _) => UpdateCartState();
    }

    [ObservableProperty]
    private string _rawItemsText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _snackbarMessage;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isCartOpen;

    [ObservableProperty]
    private bool _isItemsEditorVisible = true;

    [ObservableProperty]
    private bool _isSnackbarVisible;

    private int _snackbarVersion;


    [ObservableProperty]
    private bool _isAiCleanupEnabled;

    public ObservableCollection<SearchGroup> SearchGroups { get; } = [];

    public ObservableCollection<CartLine> CartItems { get; } = [];

    public bool HasStore => PreferencesService.HasStore;

    public string StoreDisplay => HasStore ? $"🏪 {PreferencesService.StoreName}" : "No store selected";

    public string StoreActionText => HasStore ? "Change Store" : "Select Store";

    public bool HasResults => SearchGroups.Count > 0;

    public bool HasItemsText => !string.IsNullOrWhiteSpace(RawItemsText);

    public bool HasCartItems => CartItems.Count > 0;

    public int CartItemCount => CartItems.Sum(item => item.Quantity);

    public decimal CartTotal => CartItems.Sum(item => item.LineTotal);

    public string CartSummary =>
        HasCartItems ? $"{CartItemCount} items • ${CartTotal:F2}" : "Cart is empty";

    public string CartButtonText => HasCartItems ? $"Cart ({CartItemCount})" : "Cart";

    public string ItemsEditorToggleText => IsItemsEditorVisible ? "Hide Items" : "Show Items";

    public string ItemsSummary
    {
        get
        {
            var itemCount = ParseTerms(RawItemsText).Count;
            return itemCount == 0 ? "Enter a list of items return delimited, then search" : $"{itemCount} items ready";
        }
    }

    public string ToggleAllText =>
        SearchGroups.Any(group => !group.IsExpanded) ? "Expand All" : "Collapse All";

    public string ThemeActionText => IsDarkMode ? "Use Light Mode" : "Use Dark Mode";

    public string AiActionText => IsAiCleanupEnabled ? "Disable AI Cleanup" : "Enable AI Cleanup";

    public void LoadSettings()
    {
        IsDarkMode = PreferencesService.Theme == AppTheme.Dark;
        IsAiCleanupEnabled = PreferencesService.UseAiCleanup;
        OnPropertyChanged(nameof(HasStore));
        OnPropertyChanged(nameof(StoreDisplay));
        OnPropertyChanged(nameof(StoreActionText));
        OnPropertyChanged(nameof(ThemeActionText));
        OnPropertyChanged(nameof(AiActionText));
        UpdateSearchState();
        UpdateCartState();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        PreferencesService.Theme = value ? AppTheme.Dark : AppTheme.Light;
        OnPropertyChanged(nameof(ThemeActionText));
    }

    partial void OnIsAiCleanupEnabledChanged(bool value)
    {
        PreferencesService.UseAiCleanup = value;
        OnPropertyChanged(nameof(AiActionText));
    }

    partial void OnIsItemsEditorVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ItemsEditorToggleText));
        OnPropertyChanged(nameof(ItemsSummary));
    }

    partial void OnRawItemsTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasItemsText));
        OnPropertyChanged(nameof(ItemsSummary));
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(RawItemsText))
        {
            await ShowSnackbar("Paste a list first");
            return;
        }

        if (!HasStore)
        {
            await ShowSnackbar("Select a store first");
            return;
        }

        IsBusy = true;

        try
        {
            var rawTerms = ParseTerms(RawItemsText);
            var searchTerms = rawTerms;

            if (IsAiCleanupEnabled)
            {
                // Clean up list with AI so you don't have to worry about strange pasted text
                var cleanupResponse = await _api.CleanupList(new CleanupRequest { Items = rawTerms });
                if (cleanupResponse.CleanedItems is { Count: > 0 })
                {
                    searchTerms = cleanupResponse.CleanedItems;
                    RawItemsText = string.Join(Environment.NewLine, cleanupResponse.CleanedItems);
                }
            }

            SearchGroups.Clear();
            var index = 0;
            foreach (var term in searchTerms)
            {
                var page = await SearchProducts(term, PreferencesService.StoreId, 0, PageSize);
                var group = new SearchGroup(term, page.TotalCount, PageSize)
                {
                    IsExpanded = index == 0,
                };
                group.AddMatches(page.Results);
                SearchGroups.Add(group);
                index++;
            }

            if (SearchGroups.Count > 0)
            {
                IsItemsEditorVisible = false;
            }

            _ = ShowSnackbar($"Loaded {SearchGroups.Count} groups");
        }
        catch (Exception ex)
        {
            _ = ShowSnackbar($"Search failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ViewMore(SearchGroup group)
    {
        if (group is null || !group.HasMore || string.IsNullOrWhiteSpace(PreferencesService.StoreId))
        {
            return;
        }

        IsBusy = true;

        try
        {
            var page = await SearchProducts(
                group.Query,
                PreferencesService.StoreId,
                group.LoadedCount,
                group.PageSize
            );
            group.AddMatches(page.Results);
            group.IsExpanded = true;
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Couldn't load more: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Checkout()
    {
        var cartItems = CartItems.Where(item => item.Quantity > 0).ToList();
        if (cartItems.Count == 0)
        {
            await ShowSnackbar("Add items to the cart first");
            return;
        }

        IsBusy = true;

        try
        {
            var checkout = await _api.Checkout(
                new CheckoutRequest
                {
                    LocationId = PreferencesService.StoreId,
                    Items =
                    [
                        .. cartItems.Select(item => new CartItem
                        {
                            Upc = item.Upc,
                            Description = item.Description,
                            Price = item.Price,
                            Quantity = item.Quantity,
                            ImageUrl = item.ImageUrl,
                        })
                    ],
                    ReturnUri = Constants.KrogerRedirectUri,
                }
            );

            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(checkout.AuthUrl),
                new Uri(Constants.KrogerRedirectUri)
            );

            if (result.Properties.TryGetValue("error", out var error) && error == "cart")
            {
                throw new InvalidOperationException("Server checkout failed.");
            }

            ClearCart();
            IsCartOpen = false;
            await ShowSnackbar($"Added {cartItems.Count} lines to your Kroger cart");
        }
        catch (TaskCanceledException)
        {
            await ShowSnackbar("Sign-in cancelled");
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Checkout failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleGroup(SearchGroup group)
    {
        if (group is null)
        {
            return;
        }

        group.IsExpanded = !group.IsExpanded;
        OnPropertyChanged(nameof(ToggleAllText));
    }

    [RelayCommand]
    private void ToggleAllGroups()
    {
        var expandAll = SearchGroups.Any(group => !group.IsExpanded);
        foreach (var group in SearchGroups)
        {
            group.IsExpanded = expandAll;
        }

        OnPropertyChanged(nameof(ToggleAllText));
    }

    [RelayCommand]
    private async Task AddToCart(ProductMatch match)
    {
        if (match is null || string.IsNullOrWhiteSpace(match.Upc))
        {
            return;
        }

        var existingLine = CartItems.FirstOrDefault(item => item.Upc == match.Upc);
        if (existingLine is not null)
        {
            existingLine.Quantity++;
        }
        else
        {
            CartItems.Add(
                new CartLine
                {
                    ProductId = match.ProductId,
                    Upc = match.Upc,
                    Description = match.Description,
                    ImageUrl = match.ImageUrl,
                    Price = match.Price,
                    Quantity = 1,
                }
            );
        }

        CompleteGroupAndAdvance(match);
        UpdateCartState();
        await ShowSnackbar("Added to cart");
    }

    [RelayCommand]
    private void IncreaseQuantity(CartLine item)
    {
        if (item is null)
        {
            return;
        }

        item.Quantity++;
        UpdateCartState();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartLine item)
    {
        if (item is null)
        {
            return;
        }

        item.Quantity--;
        if (item.Quantity <= 0)
        {
            CartItems.Remove(item);
        }

        UpdateCartState();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        ResetGroupSelections();
        UpdateCartState();
    }

    [RelayCommand]
    private void ToggleCart()
    {
        IsCartOpen = !IsCartOpen;
    }

    [RelayCommand]
    private void OpenCart()
    {
        IsCartOpen = true;
    }

    [RelayCommand]
    private void CloseCart()
    {
        IsCartOpen = false;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchGroups.Clear();
        RawItemsText = string.Empty;
        IsItemsEditorVisible = true;
        SnackbarMessage = string.Empty;
        IsSnackbarVisible = false;
        UpdateSearchState();
    }

    [RelayCommand]
    private void ToggleItemsEditor()
    {
        IsItemsEditorVisible = !IsItemsEditorVisible;
    }

    [RelayCommand]
    private async Task GoToStorePicker()
    {
        await Shell.Current.GoToAsync("StorePickerPage");
    }

    [RelayCommand]
    private void ClearStore()
    {
        PreferencesService.StoreId = string.Empty;
        PreferencesService.StoreName = string.Empty;
        OnPropertyChanged(nameof(HasStore));
        OnPropertyChanged(nameof(StoreDisplay));
        OnPropertyChanged(nameof(StoreActionText));
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
    }

    [RelayCommand]
    private void ToggleAiCleanup()
    {
        IsAiCleanupEnabled = !IsAiCleanupEnabled;
    }

    private static List<string> ParseTerms(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        return
        [
            .. input
                .Split(
                    ['\r', '\n'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];
    }

    private void UpdateSearchState()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(ToggleAllText));
    }

    private void CompleteGroupAndAdvance(ProductMatch match)
    {
        var currentGroup = SearchGroups.FirstOrDefault(group => group.Query == match.Query);
        if (currentGroup is null)
        {
            return;
        }

        currentGroup.IsCompleted = true;
        currentGroup.IsExpanded = false;

        var currentIndex = SearchGroups.IndexOf(currentGroup);
        for (var i = currentIndex + 1; i < SearchGroups.Count; i++)
        {
            if (!SearchGroups[i].IsCompleted)
            {
                SearchGroups[i].IsExpanded = true;
                return;
            }
        }
    }

    private void ResetGroupSelections()
    {
        for (var i = 0; i < SearchGroups.Count; i++)
        {
            SearchGroups[i].IsCompleted = false;
            SearchGroups[i].IsExpanded = i == 0;
        }
    }

    private void UpdateCartState()
    {
        OnPropertyChanged(nameof(HasCartItems));
        OnPropertyChanged(nameof(CartItemCount));
        OnPropertyChanged(nameof(CartTotal));
        OnPropertyChanged(nameof(CartSummary));
        OnPropertyChanged(nameof(CartButtonText));
    }

    private async Task<ProductSearchPage> SearchProducts(
        string term,
        string locationId,
        int start,
        int limit
    )
    {
        var page = await _api.SearchProducts(locationId, term, start, limit);
        return new ProductSearchPage(
            [.. page.Results.Select(result => MapProductMatch(result, term))],
            page.Total
        );
    }

    private static ProductMatch MapProductMatch(ProductSearchResult product, string query)
    {
        return new ProductMatch
        {
            Query = query,
            ProductId = product.ProductId,
            Upc = product.Upc,
            Description = product.Description,
            Brand = string.IsNullOrWhiteSpace(product.Brand) ? string.Empty : product.Brand,
            Size = string.IsNullOrWhiteSpace(product.Size) ? string.Empty : product.Size,
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            RegularPrice = product.RegularPrice ?? product.Price,
            HasPromo = product.HasPromo,
            PromoEndDate = product.PromoEndDate,
        };
    }

    private async Task ShowSnackbar(string message)
    {
        var version = Interlocked.Increment(ref _snackbarVersion);
        SnackbarMessage = message;
        IsSnackbarVisible = true;

        await Task.Delay(2200);
        if (version == _snackbarVersion)
        {
            IsSnackbarVisible = false;
        }
    }
}
