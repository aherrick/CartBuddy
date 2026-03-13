using CartBuddy.ViewModels;
using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public partial class CartPopup : Popup
{
    private readonly MainViewModel _viewModel;

    public CartPopup(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private async void OnClearCartClicked(object sender, EventArgs e)
    {
        var shouldClear = await Shell.Current.CurrentPage.DisplayAlertAsync(
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
