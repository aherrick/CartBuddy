using CartBuddy.Converters;
using CartBuddy.Models;
using CartBuddy.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using Syncfusion.Maui.DataSource;
using Syncfusion.Maui.DataSource.Extensions;

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

        // Keep search groups collapsed natively to prevent an open-to-close layout jolt
        // upon initial population of the shopping list
        SearchListView.DataSource.AutoExpandGroups = false;

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

        // Since AutoExpandGroups is false, calling Refresh() will collapse everything.
        // We capture the currently expanded groups first so we can restore them.
        var expandedKeys = SearchListView.DataSource.Groups
            .OfType<GroupResult>()
            .Where(g => g.IsExpand)
            .Select(g => g.Key)
            .ToList();

        // Refresh applies new data (like View More items)
        SearchListView.DataSource.Refresh();

        // Restore the expanded state of any groups that were already open
        foreach (var group in SearchListView.DataSource.Groups.OfType<GroupResult>())
        {
            if (expandedKeys.Contains(group.Key))
            {
                SearchListView.ExpandGroup(group);
            }
        }

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
        List<string> actions = [_viewModel.StoreActionText, _viewModel.ThemeActionText, _viewModel.AiActionText, "View Logs"];
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

            case "View Logs":
                await Shell.Current.GoToAsync(nameof(LogsPage));
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
