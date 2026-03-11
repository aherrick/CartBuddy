using CartBuddy.Converters;
using CartBuddy.Models;
using CartBuddy.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using Syncfusion.Maui.DataSource;

namespace CartBuddy;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private static readonly HashSet<string> GroupRefreshProperties =
    [
        nameof(SearchGroup.HasMore),
        nameof(SearchGroup.IsCompleted),
        nameof(SearchGroup.PageSummary),
    ];

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Wire the converter so it can look up SearchGroup metadata by query key
        if (Resources["GroupInfoConverter"] is GroupInfoConverter conv)
        {
            conv.ViewModel = viewModel;
        }

        // Group the flat AllProducts list by Query term
        SearchListView.DataSource.GroupDescriptors.Add(new Syncfusion.Maui.DataSource.GroupDescriptor
        {
            PropertyName = "Query"
        });

        // Refresh group headers whenever SearchGroup metadata changes (HasMore, IsCompleted, PageSummary)
        _viewModel.SearchGroups.CollectionChanged += OnSearchGroupsCollectionChanged;
    }

    private void OnSearchGroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (SearchGroup group in e.OldItems)
            {
                ((INotifyPropertyChanged)group).PropertyChanged -= OnSearchGroupPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (SearchGroup group in e.NewItems)
            {
                ((INotifyPropertyChanged)group).PropertyChanged += OnSearchGroupPropertyChanged;
            }
        }

        SearchListView.DataSource?.Refresh();
    }

    private void OnSearchGroupPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (SearchListView.DataSource is null || !GroupRefreshProperties.Contains(e.PropertyName ?? string.Empty))
        {
            return;
        }

        SearchListView.CanMaintainScrollPosition = true;
        SearchListView.DataSource.Refresh();
        SearchListView.CanMaintainScrollPosition = false;
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
        List<string> actions = [_viewModel.StoreActionText, _viewModel.ThemeActionText, _viewModel.AiActionText];
        var title = _viewModel.HasStore ? _viewModel.StoreDisplay : "Cart Buddy";

        if (_viewModel.HasStore)
        {
            actions.Add("Clear Store");
        }

        var selectedAction = await DisplayActionSheetAsync(
            title,
            "Cancel",
            null,
            [.. actions]
        );
        switch (selectedAction)
        {
            case "Select Store":
            case "Change Store":
                await _viewModel.GoToStorePickerCommand.ExecuteAsync(null);
                break;

            case "Clear Store":
                _viewModel.ClearStoreCommand.Execute(null);
                break;

            case "Use Light Mode":
            case "Use Dark Mode":
                _viewModel.ToggleThemeCommand.Execute(null);
                break;

            case "Enable AI Cleanup":
            case "Disable AI Cleanup":
                _viewModel.ToggleAiCleanupCommand.Execute(null);
                break;
        }
    }

    private void OnCartClicked(object sender, EventArgs e)
    {
        _viewModel.ToggleCartCommand.Execute(null);
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        RawItemsEditor.Unfocus();
        await _viewModel.SearchCommand.ExecuteAsync(null);
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

    private async void OnClearCartClicked(object sender, EventArgs e)
    {
        var shouldClear = await DisplayAlertAsync(
            "Clear cart?",
            "This will remove all cart items and reset your selected matches.",
            "Clear",
            "Cancel"
        );
        if (!shouldClear)
        {
            return;
        }

        _viewModel.ClearCartCommand.Execute(null);
    }
}
