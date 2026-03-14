using CartBuddy.Shared.Models;
using CartBuddy.ViewModels;
using CommunityToolkit.Maui.Extensions;

namespace CartBuddy;

public partial class LogsPage : ContentPage
{
    private readonly LogsViewModel _viewModel;
    private LogDetailPopup _logDetailPopup;

    public LogsPage(LogsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadLogsCommand.ExecuteAsync(null);
    }

    private async void OnRefreshLogs(object sender, EventArgs e)
    {
        if (sender is not RefreshView refreshView)
        {
            return;
        }

        await _viewModel.LoadLogsCommand.ExecuteAsync(null);
        refreshView.IsRefreshing = false;
    }

    private async void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        if (e.DataItem is not ApiLogEntry entry)
        {
            return;
        }

        _logDetailPopup = new LogDetailPopup(entry);
        await this.ShowPopupAsync(_logDetailPopup, null);
        _logDetailPopup = null;
    }
}
