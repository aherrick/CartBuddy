using CartBuddy.ViewModels;

namespace CartBuddy;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = InitializePage();
    }

    private async Task InitializePage()
    {
        try
        {
            _viewModel.LoadSettings();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Startup error", ex.Message, "OK");
        }
    }
}