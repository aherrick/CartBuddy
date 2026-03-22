using System.Collections.ObjectModel;
using CartBuddy.Messages;
using CartBuddy.Models;
using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace CartBuddy.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ICartBuddyApi _api;
    private readonly IMessenger _messenger;
    private CancellationTokenSource _searchCts = new();
    private string _lastKnownStoreId;
    private bool _cartLoaded;

    public MainViewModel(ICartBuddyApi api, IMessenger messenger)
    {
        _api = api;
        _messenger = messenger;
        ConfirmedSearchTerms.CollectionChanged += (_, _) => UpdateSearchTermState();
        SearchGroups.CollectionChanged += (_, _) => UpdateSearchState();
        AllProducts.CollectionChanged += (_, _) => UpdateSearchState();
        CartItems.CollectionChanged += (_, _) => UpdateCartState();
    }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isDarkMode;

    public ObservableCollection<CategoryItem> ConfirmedSearchTerms { get; } = [];

    public ObservableCollection<SearchGroup> SearchGroups { get; } = [];

    public ObservableCollection<CartLine> CartItems { get; } = [];

    public ObservableCollection<ProductMatch> AllProducts { get; } = [];

    [ObservableProperty]
    private bool _allGroupsExpanded = true;

    [ObservableProperty]
    private int _groupStateVersion;

    public bool HasStore => PreferencesService.HasStore;

    public string StoreDisplay => HasStore ? PreferencesService.StoreName : "No store selected";

    public bool HasResults => AllProducts.Count > 0;

    public bool ShowEmptyState => !HasResults && !IsBusy;

    public bool HasPreparedSearchTerms => ConfirmedSearchTerms.Count > 0;

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

    public string SearchTermsSummary =>
        HasPreparedSearchTerms
            ? $"{ConfirmedSearchTerms.Count} confirmed terms"
            : "Build and review a list before searching";

    public string SearchTermsActionText => HasPreparedSearchTerms ? "Edit List" : "Build List";

    public string EmptyStateTitle =>
        HasPreparedSearchTerms ? "No results to show" : "Start a shopping list";

    public string EmptyStateDescription =>
        HasPreparedSearchTerms
            ? "Edit your confirmed list to run a fresh search on the main page."
            : "Build and review a cleaned shopping list in the popup, then search Kroger from here.";

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
        UpdateSearchTermState();
        UpdateSearchState();
        UpdateCartState();
    }

    public List<CategoryItem> GetConfirmedSearchTerms() =>
        [.. ConfirmedSearchTerms.Select(item => item.Clone())];

    public async Task ApplyConfirmedSearchTermsAsync(IReadOnlyList<CategoryItem> terms)
    {
        ResetSearchCancellationToken();
        SetConfirmedSearchTerms(terms);
        ClearSearchResults();
        AllGroupsExpanded = true;

        if (!HasPreparedSearchTerms)
        {
            IsBusy = false;
            return;
        }

        if (!HasStore)
        {
            await NotificationPopupService.Show("Select a store first", NotificationPopupType.Info);
            return;
        }

        try
        {
            await ExecuteBusyAsync(async () =>
            {
                var ct = _searchCts.Token;
                var nextGroups = new List<SearchGroup>(ConfirmedSearchTerms.Count);
                var nextProducts = new List<ProductMatch>();

                foreach (var confirmedItem in ConfirmedSearchTerms)
                {
                    var page = await SearchProducts(
                        PreferencesService.StoreId,
                        confirmedItem,
                        0,
                        SearchConstants.PageSize,
                        ct
                    );

                    ct.ThrowIfCancellationRequested();

                    var group = new SearchGroup(
                        confirmedItem.Item,
                        page.TotalCount,
                        SearchConstants.PageSize,
                        confirmedItem.Category
                    );
                    group.AddMatches(page.Results);
                    nextGroups.Add(group);

                    if (group.Count > 0)
                    {
                        nextProducts.AddRange(group);
                    }
                    else
                    {
                        nextProducts.Add(new ProductMatch { Query = group.Query, IsNoResult = true });
                    }
                }

                ApplySearchResults(nextGroups, nextProducts);
                AllGroupsExpanded = false;
            });
        }
        catch (OperationCanceledException)
        {
            // new search was triggered — discard results silently
        }
        catch (Exception ex)
        {
            await NotificationPopupService.Show($"Search failed: {ex.Message}", NotificationPopupType.Error);
        }
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        PreferencesService.Theme = value ? AppTheme.Dark : AppTheme.Light;
        OnPropertyChanged(nameof(ThemeActionText));
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

        try
        {
            await ExecuteBusyAsync(async () =>
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
            });
        }
        catch (Exception ex)
        {
            await NotificationPopupService.Show($"Couldn't load more: {ex.Message}", NotificationPopupType.Error);
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

        try
        {
            await ExecuteBusyAsync(async () =>
            {
                var useBrowserCheckout = DeviceInfo.Platform == DevicePlatform.WinUI;
                var returnUri = useBrowserCheckout
                    ? Constants.KrogerBrowserReturnUri
                    : Constants.KrogerRedirectUri;

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
                        ReturnUri = returnUri,
                    }
                );

                if (useBrowserCheckout)
                {
                    _messenger.Send(new CloseCartRequestedMessage());
                    await Launcher.Default.OpenAsync(new Uri(checkout.AuthUrl));
                    await NotificationPopupService.Show(
                        "Checkout opened in your browser. Finish sign-in there to add items to Kroger.",
                        NotificationPopupType.Info
                    );
                    return;
                }

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
            });
        }
        catch (TaskCanceledException)
        {
            await NotificationPopupService.Show("Sign-in cancelled", NotificationPopupType.Info);
        }
        catch (Exception ex)
        {
            await NotificationPopupService.Show($"Checkout failed: {ex.Message}", NotificationPopupType.Error);
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
        ResetSearchCancellationToken();
        ConfirmedSearchTerms.Clear();
        ClearSearchResults();
        AllGroupsExpanded = true;
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

    private void ResetSearchCancellationToken()
    {
        var previous = _searchCts;
        _searchCts = new CancellationTokenSource();

        previous.Cancel();
        previous.Dispose();
    }

    private void ClearSearchResults()
    {
        SearchGroups.Clear();
        AllProducts.Clear();
    }

    private async Task ExecuteBusyAsync(Func<Task> action)
    {
        IsBusy = true;

        try
        {
            await action();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private void UpdateSearchState()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    private void UpdateSearchTermState()
    {
        OnPropertyChanged(nameof(HasPreparedSearchTerms));
        OnPropertyChanged(nameof(SearchTermsSummary));
        OnPropertyChanged(nameof(SearchTermsActionText));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
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

    private void SetConfirmedSearchTerms(IEnumerable<CategoryItem> terms)
    {
        ConfirmedSearchTerms.Clear();

        foreach (var term in terms)
        {
            if (string.IsNullOrWhiteSpace(term?.Item))
            {
                continue;
            }

            ConfirmedSearchTerms.Add(term.Clone());
        }
    }

    private void ApplySearchResults(IReadOnlyList<SearchGroup> groups, IReadOnlyList<ProductMatch> products)
    {
        ClearSearchResults();

        foreach (var group in groups)
        {
            SearchGroups.Add(group);
        }

        foreach (var product in products)
        {
            AllProducts.Add(product);
        }

        SyncGroupSelections();
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
