using CartBuddy.Shared.Models;
using CartBuddy.ViewModels;

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

    private void OnEntryTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        if (e.DataItem is ApiLogEntry entry)
        {
            _viewModel.SelectEntryCommand.Execute(entry);
        }
    }
}
