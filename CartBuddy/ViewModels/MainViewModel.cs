using System.Collections.ObjectModel;
using CartBuddy.Models;
using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CartBuddy.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace CartBuddy.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int PageSize = 10;

    private readonly ICartBuddyApi _api;
    private readonly IMessenger _messenger;
    private readonly INotificationPopupService _notifications;

    public MainViewModel(
        ICartBuddyApi api,
        INotificationPopupService notifications,
        IMessenger messenger
    )
    {
        _api = api;
        _notifications = notifications;
        _messenger = messenger;
        SearchGroups.CollectionChanged += (_, _) => UpdateSearchState();
        AllProducts.CollectionChanged += (_, _) => UpdateSearchState();
        CartItems.CollectionChanged += (_, _) => UpdateCartState();
    }

    [ObservableProperty]
    private string _rawItemsText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private bool _isItemsEditorVisible = true;

    [ObservableProperty]
    private bool _isAiCleanupEnabled;

    public ObservableCollection<SearchGroup> SearchGroups { get; } = [];

    public ObservableCollection<CartLine> CartItems { get; } = [];

    public ObservableCollection<ProductMatch> AllProducts { get; } = [];

    [ObservableProperty]
    private bool _allGroupsExpanded = true;

    [ObservableProperty]
    private int _groupStateVersion;

    private string _lastKnownStoreId;

    public bool HasStore => PreferencesService.HasStore;

    public string StoreDisplay => HasStore ? PreferencesService.StoreName : "No store selected";

    public bool HasResults => AllProducts.Count > 0;

    public bool HasItemsText => !string.IsNullOrWhiteSpace(RawItemsText);

    public bool HasCartItems => CartItems.Count > 0;

    public int CartItemCount => CartItems.Sum(item => item.Quantity);

    public decimal CartTotal => CartItems.Sum(item => item.LineTotal);

    public string CartSummary =>
        HasCartItems ? $"{CartItemCount} items | ${CartTotal:F2}" : "Cart is empty";

    public bool CanToggleItemsEditor => SearchGroups.Count > 0;

    public string ItemsEditorToggleText => IsItemsEditorVisible ? "Hide List" : "Edit List";

    public string ItemsSummary
    {
        get
        {
            var itemCount = ParseTerms(RawItemsText).Count;
            return itemCount == 0 ? "Enter one item per line" : $"{itemCount} items in list";
        }
    }

    public string ThemeActionText => IsDarkMode ? "Use Light Mode" : "Use Dark Mode";

    public string AiActionText => IsAiCleanupEnabled ? "Disable AI Cleanup" : "Enable AI Cleanup";

    public void LoadSettings()
    {
        IsDarkMode = PreferencesService.Theme == AppTheme.Dark;
        IsAiCleanupEnabled = PreferencesService.UseAiCleanup;

        var currentStoreId = PreferencesService.StoreId;
        if (_lastKnownStoreId is not null && _lastKnownStoreId != currentStoreId)
        {
            ClearSearch();
            CartItems.Clear();
        }
        _lastKnownStoreId = currentStoreId;

        OnPropertyChanged(nameof(HasStore));
        OnPropertyChanged(nameof(StoreDisplay));
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
            await ShowSnackbar("Paste a list first", NotificationPopupType.Info);
            return;
        }

        if (!HasStore)
        {
            await ShowSnackbar("Select a store first", NotificationPopupType.Info);
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
                var cleanupResponse = await _api.CleanupList(
                    new CleanupRequest { Items = rawTerms }
                );
                if (cleanupResponse.CleanedItems is { Count: > 0 })
                {
                    searchTerms = cleanupResponse.CleanedItems;
                    RawItemsText = string.Join(Environment.NewLine, cleanupResponse.CleanedItems);
                }
            }

            SearchGroups.Clear();
            AllProducts.Clear();
            foreach (var term in searchTerms)
            {
                var page = await SearchProducts(term, PreferencesService.StoreId, 0, PageSize);
                var group = new SearchGroup(term, page.TotalCount, PageSize);
                group.AddMatches(page.Results);
                SearchGroups.Add(group);
                if (page.Results.Count > 0)
                {
                    foreach (var product in page.Results)
                    {
                        AllProducts.Add(product);
                    }
                }
                else
                {
                    AllProducts.Add(new ProductMatch { Query = term, IsNoResult = true });
                }
            }

            SyncGroupSelections();
            AllGroupsExpanded = false;

            if (SearchGroups.Count > 0)
            {
                IsItemsEditorVisible = false;
            }
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Search failed: {ex.Message}", NotificationPopupType.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ViewMore(SearchGroup group)
    {
        if (
            group is null
            || !group.HasMore
            || string.IsNullOrWhiteSpace(PreferencesService.StoreId)
        )
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
            ProductMatch lastAdded = null;
            foreach (var product in page.Results)
            {
                AllProducts.Add(product);
                lastAdded = product;
            }
            GroupStateVersion++;
            if (lastAdded is not null)
            {
                _messenger.Send(new ScrollToProductMessage(lastAdded));
            }
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Couldn't load more: {ex.Message}", NotificationPopupType.Error);
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
            await ShowSnackbar("Add items to the cart first", NotificationPopupType.Info);
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
                        }),
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
            _messenger.Send(new CloseCartRequestedMessage());
            await ShowSnackbar(
                $"Added {cartItems.Count} lines to your Kroger cart",
                NotificationPopupType.Success
            );
            await Launcher.Default.OpenAsync("https://www.kroger.com/cart");
        }
        catch (TaskCanceledException)
        {
            await ShowSnackbar("Sign-in cancelled", NotificationPopupType.Info);
        }
        catch (Exception ex)
        {
            await ShowSnackbar($"Checkout failed: {ex.Message}", NotificationPopupType.Error);
        }
        finally
        {
            IsBusy = false;
        }
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
            existingLine.SourceQueries.Add(match.Query);
            UpdateCartState();
        }
        else
        {
            var cartLine = new CartLine
            {
                Upc = match.Upc,
                Description = match.Description,
                Brand = match.Brand,
                Size = match.Size,
                ImageUrl = match.ImageUrl,
                Price = match.Price,
                Quantity = 1,
            };
            cartLine.SourceQueries.Add(match.Query);
            CartItems.Add(cartLine);
        }
        await ShowSnackbar("Added to cart", NotificationPopupType.Success);
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
            return;
        }

        UpdateCartState();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchGroups.Clear();
        AllProducts.Clear();
        RawItemsText = string.Empty;
        IsItemsEditorVisible = true;
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
        OnPropertyChanged(nameof(CanToggleItemsEditor));
    }

    private void SyncGroupSelections()
    {
        for (var i = 0; i < SearchGroups.Count; i++)
        {
            var group = SearchGroups[i];
            group.IsCompleted = CartItems.Any(item =>
                item.Quantity > 0 && item.SourceQueries.Contains(group.Query)
            );
        }
        GroupStateVersion++;
    }

    private void UpdateCartState()
    {
        SyncGroupSelections();
        OnPropertyChanged(nameof(HasCartItems));
        OnPropertyChanged(nameof(CartItemCount));
        OnPropertyChanged(nameof(CartTotal));
        OnPropertyChanged(nameof(CartSummary));
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

    private async Task ShowSnackbar(
        string message,
        NotificationPopupType type = NotificationPopupType.Info
    )
    {
        await _notifications.Show(message, type);
    }
}
