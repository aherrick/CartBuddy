using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace CartBuddy.Services;

public enum NotificationPopupType
{
    Info,
    Success,
    Error,
}

public static class NotificationPopupService
{
    public static async Task Show(string message, NotificationPopupType type = NotificationPopupType.Info)
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

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Snackbar.Make(message, duration: TimeSpan.FromSeconds(2.5), visualOptions: options)
            .Show(cts.Token);
    }
}
