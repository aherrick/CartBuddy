using CartBuddy.Converters;
using CartBuddy.Models;
using CartBuddy.ViewModels;
using CommunityToolkit.Maui.Extensions;
using System.Collections.Specialized;
using Syncfusion.Maui.DataSource;

namespace CartBuddy;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private CartPopup _cartPopup;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Wire converter so it can look up SearchGroup metadata by query key
        if (Resources["GroupInfoConverter"] is GroupInfoConverter conv)
        {
            conv.ViewModel = viewModel;
        }

        // Keep search groups collapsed natively to prevent an open-to-close layout jolt
        // upon initial population of the shopping list
        SearchListView.DataSource.AutoExpandGroups = false;

        // Group the flat AllProducts list by Query term
        SearchListView.DataSource.GroupDescriptors.Add(new Syncfusion.Maui.DataSource.GroupDescriptor
        {
            PropertyName = "Query"
        });

        _viewModel.SearchGroups.CollectionChanged += OnSearchGroupsCollectionChanged;
        _viewModel.ScrollToItem = item => SearchListView.ScrollTo(item, Microsoft.Maui.Controls.ScrollToPosition.End, true);
        _viewModel.CloseCartRequested = () => _ = _cartPopup?.CloseAsync();
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

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        RawItemsEditor.Unfocus();
        await _viewModel.SearchCommand.ExecuteAsync(null);

        if (_viewModel.HasResults)
        {
            // Syncfusion automatically renders new groups collapsed (via AutoExpandGroups = false)
            // We just ensure the expand/collapse toggle button state resets properly.
            _viewModel.AllGroupsExpanded = false;
        }
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
