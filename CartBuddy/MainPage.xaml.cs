using CartBuddy.Messages;
using CartBuddy.Models;
using CartBuddy.ViewModels;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Specialized;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.DataSource.Extensions;

namespace CartBuddy;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IAsyncRelayCommand<ProductMatch> _addToCartAndCollapseCommand;
    private CartPopup _cartPopup;

    public MainPage(MainViewModel viewModel, IMessenger messenger)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Keep search groups collapsed natively to prevent an open-to-close layout jolt
        // upon initial population of the shopping list.
        SearchListView.DataSource.AutoExpandGroups = false;

        // Group the flat AllProducts list by query term.
        SearchListView.DataSource.GroupDescriptors.Add(new GroupDescriptor { PropertyName = "Query" });

        _viewModel.SearchGroups.CollectionChanged += OnSearchGroupsCollectionChanged;
        _addToCartAndCollapseCommand = new AsyncRelayCommand<ProductMatch>(AddToCartAndCollapse);

        messenger.Register<ScrollToProductMessage>(this, static (recipient, message) =>
        {
            var page = (MainPage)recipient;
            page.SearchListView.ScrollTo(message.Item, ScrollToPosition.End, true);
        });
        messenger.Register<CloseCartRequestedMessage>(this, static (recipient, message) =>
        {
            var page = (MainPage)recipient;
            if (page._cartPopup is not null)
            {
                _ = page._cartPopup.CloseAsync();
            }
        });
    }

    public IAsyncRelayCommand<SearchGroup> ViewMoreCommand => _viewModel.ViewMoreCommand;

    public IAsyncRelayCommand<ProductMatch> AddToCartCommand => _addToCartAndCollapseCommand;

    private async Task AddToCartAndCollapse(ProductMatch match)
    {
        if (match is null)
        {
            return;
        }

        await _viewModel.AddToCartCommand.ExecuteAsync(match);

        var group = SearchListView.DataSource?.Groups
            ?.OfType<GroupResult>()
            .FirstOrDefault(g => string.Equals(g.Key as string, match.Query, StringComparison.OrdinalIgnoreCase));

        if (group is not null)
        {
            SearchListView.CollapseAll();
            _viewModel.AllGroupsExpanded = false;
        }
    }

    private void OnSearchGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        SearchListView.DataSource?.Refresh();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadSettings();

        if (!_viewModel.HasStore)
        {
            Dispatcher.DispatchAsync(() => _viewModel.GoToStorePickerCommand.ExecuteAsync(null));
        }
    }

    private void OnToggleExpandAll(object sender, EventArgs e)
    {
        if (_viewModel.AllGroupsExpanded)
        {
            SearchListView.CollapseAll();
            _viewModel.AllGroupsExpanded = false;
        }
        else
        {
            SearchListView.ExpandAll();
            _viewModel.AllGroupsExpanded = true;
        }
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        var title = _viewModel.HasStore ? _viewModel.StoreDisplay : "Cart Buddy";
        var selectedAction = await DisplayActionSheetAsync(
            title,
            "Cancel",
            null,
            "Select Store", _viewModel.ThemeActionText, _viewModel.AiActionText, "View Logs"
        );
        switch (selectedAction)
        {
            case "Select Store":
                if (_viewModel.HasStore)
                {
                    var confirmed = await DisplayAlertAsync(
                        "Change Store",
                        "Switching stores will clear your current search and cart.",
                        "Continue",
                        "Cancel"
                    );
                    if (!confirmed)
                    {
                        break;
                    }
                }
                await _viewModel.GoToStorePickerCommand.ExecuteAsync(null);
                break;

            case "Use Light Mode":
            case "Use Dark Mode":
                _viewModel.ToggleThemeCommand.Execute(null);
                break;

            case "Enable AI Cleanup":
            case "Disable AI Cleanup":
                _viewModel.ToggleAiCleanupCommand.Execute(null);
                break;

            case "View Logs":
                await Shell.Current.GoToAsync(nameof(LogsPage));
                break;
        }
    }

    private async void OnCartClicked(object sender, EventArgs e)
    {
        _cartPopup = new CartPopup(_viewModel);
        await this.ShowPopupAsync(_cartPopup, null);
        _cartPopup = null;
    }

    private async void OnClearSearchClicked(object sender, EventArgs e)
    {
        var shouldClear = await DisplayAlertAsync(
            "Clear list?",
            "This will remove your current shopping list and search results.",
            "Clear",
            "Cancel"
        );
        if (!shouldClear)
        {
            return;
        }

        _viewModel.ClearSearchCommand.Execute(null);
    }
}
