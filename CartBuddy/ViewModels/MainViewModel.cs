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
    private readonly ICartBuddyApi _api;
    private readonly IMessenger _messenger;
    private CancellationTokenSource _searchCts = new();

    public MainViewModel(ICartBuddyApi api, IMessenger messenger)
    {
        _api = api;
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

    public ObservableCollection<SearchGroup> SearchGroups { get; } = [];

    public ObservableCollection<CartLine> CartItems { get; } = [];

    public ObservableCollection<ProductMatch> AllProducts { get; } = [];

    [ObservableProperty]
    private bool _allGroupsExpanded = true;

    [ObservableProperty]
    private int _groupStateVersion;

    private string _lastKnownStoreId;
    private bool _cartLoaded;

    public bool HasStore => PreferencesService.HasStore;

    public string StoreDisplay => HasStore ? PreferencesService.StoreName : "No store selected";

    public bool HasResults => AllProducts.Count > 0;

    public bool HasItemsText => !string.IsNullOrWhiteSpace(RawItemsText);

    public bool HasCartItems => CartItems.Count > 0;

    public bool IsCartEmpty => !HasCartItems;

    public Color CartIconColor =>
        HasCartItems
            ? (Color)Application.Current.Resources["Primary"]
            : Application.Current?.RequestedTheme == AppTheme.Dark
                ? (Color)Application.Current.Resources["Gray100"]
                : (Color)Application.Current.Resources["Gray950"];

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

    public void LoadSettings()
    {
        IsDarkMode = PreferencesService.Theme == AppTheme.Dark;

        var currentStoreId = PreferencesService.StoreId;
        if (_lastKnownStoreId is not null && _lastKnownStoreId != currentStoreId)
        {
            ClearSearch();
            CartItems.Clear();
        }
        _lastKnownStoreId = currentStoreId;

        if (!_cartLoaded)
        {
            _cartLoaded = true;
            LoadCart();
        }

        OnPropertyChanged(nameof(HasStore));
        OnPropertyChanged(nameof(StoreDisplay));
        UpdateSearchState();
        UpdateCartState();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        PreferencesService.Theme = value ? AppTheme.Dark : AppTheme.Light;
        OnPropertyChanged(nameof(ThemeActionText));
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
            await NotificationPopupService.Show("Paste a list first", NotificationPopupType.Info);
            return;
        }

        if (!HasStore)
        {
            await NotificationPopupService.Show("Select a store first", NotificationPopupType.Info);
            return;
        }

        IsBusy = true;

        await _searchCts.CancelAsync();
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;

        try
        {
            var rawTerms = ParseTerms(RawItemsText);
            var classifiedItems = await _api.CleanupList(new CleanupRequest { Items = rawTerms }, ct)
                ?? [.. rawTerms.Select(item => new CategoryItem { Item = item, Category = "other" })];

            ct.ThrowIfCancellationRequested();

            RawItemsText = string.Join(Environment.NewLine, classifiedItems.Select(item => item.Item));

            SearchGroups.Clear();
            AllProducts.Clear();
            foreach (var searchItem in classifiedItems)
            {
                var page = await SearchProducts(
                    PreferencesService.StoreId,
                    searchItem,
                    0,
                    SearchConstants.PageSize,
                    ct
                );
                var group = new SearchGroup(
                    searchItem.Item,
                    page.TotalCount,
                    SearchConstants.PageSize,
                    searchItem.Category
                );
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
                    AllProducts.Add(new ProductMatch { Query = searchItem.Item, IsNoResult = true });
                }
            }

            SyncGroupSelections();
            AllGroupsExpanded = false;

            if (SearchGroups.Count > 0)
            {
                IsItemsEditorVisible = false;
            }
        }
        catch (OperationCanceledException)
        {
            // new search was triggered — discard results silently
        }
        catch (Exception ex)
        {
            await NotificationPopupService.Show($"Search failed: {ex.Message}", NotificationPopupType.Error);
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
                PreferencesService.StoreId,
                new CategoryItem { Item = group.Query, Category = group.Category },
                group.LoadedCount,
                group.PageSize
            );
            group.AddMatches(page.Results);
            foreach (var product in page.Results)
            {
                AllProducts.Add(product);
            }
            GroupStateVersion++;
            if (page.Results.Count > 0)
            {
                _messenger.Send(new ScrollToProductMessage(page.Results[^1]));
            }
        }
        catch (Exception ex)
        {
            await NotificationPopupService.Show($"Couldn't load more: {ex.Message}", NotificationPopupType.Error);
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
            await NotificationPopupService.Show("Add items to the cart first", NotificationPopupType.Info);
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
            await NotificationPopupService.Show(
                $"Added {cartItems.Count} lines to your Kroger cart",
                NotificationPopupType.Success
            );
            await Launcher.Default.OpenAsync("https://www.kroger.com/cart");
        }
        catch (TaskCanceledException)
        {
            await NotificationPopupService.Show("Sign-in cancelled", NotificationPopupType.Info);
        }
        catch (Exception ex)
        {
            await NotificationPopupService.Show($"Checkout failed: {ex.Message}", NotificationPopupType.Error);
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
                SoldByWeight = match.SoldByWeight,
                AverageWeightPerUnit = match.AverageWeightPerUnit,
                Quantity = 1,
            };
            cartLine.SourceQueries.Add(match.Query);
            CartItems.Add(cartLine);
        }
        await NotificationPopupService.Show("Added to cart", NotificationPopupType.Success);
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
        OnPropertyChanged(nameof(IsCartEmpty));
        OnPropertyChanged(nameof(CartIconColor));
        OnPropertyChanged(nameof(CartItemCount));
        OnPropertyChanged(nameof(CartTotal));
        OnPropertyChanged(nameof(CartSummary));
        SaveCart();
    }

    private void SaveCart()
    {
        PreferencesService.Cart = [.. CartItems];
    }

    private void LoadCart()
    {
        foreach (var item in PreferencesService.Cart)
        {
            CartItems.Add(item);
        }
    }

    private async Task<ProductSearchPage> SearchProducts(
        string locationId,
        CategoryItem item,
        int start,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var page = await PollyHelper.ExecuteSearchRetry(
            () => _api.SearchProducts(new ProductSearchRequest { LocationId = locationId, Item = item, Start = start, Limit = limit }, cancellationToken),
            cancellationToken
        );
        return new ProductSearchPage(
            [.. page.Results.Select(result => MapProductMatch(result, item.Item))],
            page.Total
        );
    }

    private static ProductMatch MapProductMatch(ProductSearchResult product, string query) =>
        new()
        {
            Query = query,
            Upc = product.Upc,
            Description = product.Description,
            Brand = product.Brand,
            Size = product.Size,
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            RegularPrice = product.RegularPrice ?? product.Price,
            HasPromo = product.HasPromo,
            PromoEndDate = product.PromoEndDate,
            SoldByWeight = product.SoldByWeight,
            AverageWeightPerUnit = product.AverageWeightPerUnit,
        };

}
