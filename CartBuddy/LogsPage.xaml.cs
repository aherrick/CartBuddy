using CartBuddy.Shared.Models;
using CartBuddy.ViewModels;
using CommunityToolkit.Maui.Extensions;

namespace CartBuddy;

public partial class LogsPage : ContentPage
{
    private readonly LogsViewModel _viewModel;

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

    private async void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        if (e.DataItem is not ApiLogEntry entry)
        {
            return;
        }

        _viewModel.SelectedEntry = entry;
        await this.ShowPopupAsync(new LogDetailPopup(_viewModel));
    }
}
