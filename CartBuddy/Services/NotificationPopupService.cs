using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace CartBuddy.Services;

public enum NotificationPopupType
{
    Info,
    Success,
    Error,
}

public interface INotificationPopupService
{
    Task Show(string message, NotificationPopupType type = NotificationPopupType.Info);
}

public class NotificationPopupService : INotificationPopupService
{
    public async Task Show(string message, NotificationPopupType type = NotificationPopupType.Info)
    {
        var (bgColor, textColor) = type switch
        {
            NotificationPopupType.Success => (Color.FromArgb("#2e7d32"), Colors.White),
            NotificationPopupType.Error => (Color.FromArgb("#c62828"), Colors.White),
            _ => (Color.FromArgb("#1565c0"), Colors.White),
        };

        var options = new SnackbarOptions
        {
            BackgroundColor = bgColor,
            TextColor = textColor,
            CornerRadius = new CornerRadius(8),
            Font = Microsoft.Maui.Font.SystemFontOfSize(14),
        };

        var page = Shell.Current?.CurrentPage as ContentPage;

        // Prefer a named top anchor; fall back to the first child of the root layout,
        // which is always the topmost visual element regardless of page type.
        IView anchor = page?.FindByName<IView>("TopBar");
        if (anchor is null && page?.Content is Layout root && root.Children.Count > 0)
        {
            anchor = root.Children[0];
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Snackbar.Make(message, duration: TimeSpan.FromSeconds(2.5), visualOptions: options, anchor: anchor)
            .Show(cts.Token);
    }
}
