using CommunityToolkit.Maui.Views;
using System.ComponentModel;

namespace CartBuddy;

public class AppPopup : Popup
{
    private const string PopupSurfaceName = "PopupSurface";
    private const string PopupChromeGridId = "PopupChromeGrid";
    private const double WindowsPopupMaxWidth = 1120;
    private const double IosPopupWidth = 360;
    private const double PopupHorizontalMargin = 32;

    public AppPopup()
    {
        PropertyChanged += OnPopupPropertyChanged;
        Opened += (_, _) => ApplyPlatformLayout();
    }

    private void OnPopupPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Content) or nameof(Window))
        {
            // Apply an early size hint before open to reduce first-frame width jumps on iOS.
            ApplyPlatformLayout();
        }
    }

    private void ApplyPlatformLayout()
    {
        if (this.FindByName<View>(PopupSurfaceName) is not { } surface)
        {
            return;
        }

        if (surface is Border border)
        {
            EnsureCloseButton(border);
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

    private void EnsureCloseButton(Border surface)
    {
        if (surface.Content is not { } existingContent)
        {
            return;
        }

        if (existingContent is Grid existingGrid && existingGrid.AutomationId == PopupChromeGridId)
        {
            return;
        }

        var closeButton = new Button
        {
            Text = "\uF00D",
            FontFamily = "FaSolid",
            FontSize = 16,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            CornerRadius = 20,
            Padding = new Thickness(10, 8),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            MinimumWidthRequest = 40,
            MinimumHeightRequest = 40,
            Margin = new Thickness(0, 6, 6, 0),
        };
        closeButton.SetDynamicResource(Button.TextColorProperty, "Gray400");
        SemanticProperties.SetDescription(closeButton, "Close");
        SemanticProperties.SetHint(closeButton, "Closes this popup");
        closeButton.Clicked += async (_, _) => await CloseAsync();

        var chromeGrid = new Grid
        {
            AutomationId = PopupChromeGridId,
        };

        surface.Content = null;
        chromeGrid.Children.Add(existingContent);
        chromeGrid.Children.Add(closeButton);
        surface.Content = chromeGrid;
    }
}
