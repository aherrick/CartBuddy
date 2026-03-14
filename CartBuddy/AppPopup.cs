using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public class AppPopup : Popup
{
    private const string PopupSurfaceName = "PopupSurface";
    private const double WindowsPopupMaxWidth = 1120;
    private const double IosPopupWidth = 380;
    private const double PopupHorizontalMargin = 32;

    public AppPopup()
    {
        Opened += (_, _) => ApplyPlatformLayout();
    }

    private void ApplyPlatformLayout()
    {
        if (this.FindByName<View>(PopupSurfaceName) is not { } surface)
        {
            return;
        }

        HorizontalOptions = LayoutOptions.Fill;

        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            var popupWindow = Window ?? Application.Current?.Windows.FirstOrDefault();
            var surfaceWidth = WindowsPopupMaxWidth;
            if (popupWindow is { Width: > 0 })
            {
                surfaceWidth = Math.Min(WindowsPopupMaxWidth, Math.Max(0, popupWindow.Width - PopupHorizontalMargin));
            }

            surface.HorizontalOptions = LayoutOptions.Center;
            surface.WidthRequest = surfaceWidth;
            surface.ClearValue(VisualElement.MaximumWidthRequestProperty);
            return;
        }

        if (DeviceInfo.Platform == DevicePlatform.iOS)
        {
            surface.HorizontalOptions = LayoutOptions.Center;
            surface.ClearValue(VisualElement.MaximumWidthRequestProperty);

            var popupWindow = Window ?? Application.Current?.Windows.FirstOrDefault();
            if (popupWindow?.Width > 0)
            {
                surface.WidthRequest = Math.Min(
                    IosPopupWidth,
                    Math.Max(0, popupWindow.Width - PopupHorizontalMargin)
                );
            }
            else
            {
                surface.WidthRequest = IosPopupWidth;
            }

            return;
        }

        surface.HorizontalOptions = LayoutOptions.Fill;
        surface.ClearValue(VisualElement.WidthRequestProperty);
        surface.ClearValue(VisualElement.MaximumWidthRequestProperty);
    }
}
