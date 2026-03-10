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
        _viewModel.LoadSettings();
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        List<string> actions = [_viewModel.StoreActionText, _viewModel.ThemeActionText];

        if (_viewModel.HasStore)
        {
            actions.Add("Clear Store");
        }

        var selectedAction = await DisplayActionSheetAsync(
            "Cart Buddy",
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
        }
    }

    private void OnCartClicked(object sender, EventArgs e)
    {
        _viewModel.ToggleCartCommand.Execute(null);
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