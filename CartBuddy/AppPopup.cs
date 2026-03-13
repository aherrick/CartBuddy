using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public class AppPopup : Popup
{
    private const string PopupSurfaceName = "PopupSurface";
    private const double WindowsPopupMaxWidth = 1120;

    public AppPopup()
    {
        Opened += (_, _) => ApplyWindowsLayout();
    }

    private void ApplyWindowsLayout()
    {
        if (this.FindByName<View>(PopupSurfaceName) is not { } surface)
        {
            return;
        }

        HorizontalOptions = LayoutOptions.Fill;
        surface.HorizontalOptions = LayoutOptions.Fill;

        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            surface.MaximumWidthRequest = WindowsPopupMaxWidth;
            return;
        }

        surface.ClearValue(VisualElement.MaximumWidthRequestProperty);
    }
}
