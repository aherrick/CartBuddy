using CartBuddy.ViewModels;
using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public partial class CartPopup : Popup
{
    private readonly MainViewModel _viewModel;
    private const double WindowsCartMaxWidth = 1120;
    private const double WindowsCartMaxHeight = 820;
    private const double WindowsCartChromeMargin = 48;

    public CartPopup(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;
            Opened += (_, _) => ApplyWindowsPopupSize();
        }
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

    private void ApplyWindowsPopupSize()
    {
        var popupWindow = Window ?? Application.Current?.Windows.FirstOrDefault();
        if (popupWindow is null)
        {
            return;
        }

        var width = Math.Min(WindowsCartMaxWidth, popupWindow.Width - WindowsCartChromeMargin);
        var height = Math.Min(
            WindowsCartMaxHeight,
            popupWindow.Height - WindowsCartChromeMargin
        );

        if (width > 0 && height > 0)
        {
            CartContainer.HorizontalOptions = LayoutOptions.Center;
            CartContainer.VerticalOptions = LayoutOptions.Center;
            CartContainer.WidthRequest = width;
            CartContainer.HeightRequest = height;
        }
    }
}
