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
    private View _currentToast;
    private CancellationTokenSource _dismissTokenSource;

    public async Task Show(string message, NotificationPopupType type = NotificationPopupType.Info)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Shell.Current?.CurrentPage as ContentPage;
            if (page?.Content is not Layout layout)
            {
                return;
            }

            if (_currentToast is not null)
            {
                layout.Children.Remove(_currentToast);
                _currentToast = null;
            }

            _dismissTokenSource?.Cancel();
            _dismissTokenSource = new CancellationTokenSource();

            var toast = CreateToast(message, type);
            _currentToast = toast;
            toast.Opacity = 0;
            toast.TranslationY = -10;
            toast.ZIndex = 1000;

            layout.Children.Add(toast);

            await Task.WhenAll(
                toast.FadeToAsync(1, 160, Easing.CubicOut),
                toast.TranslateToAsync(0, 0, 160, Easing.CubicOut)
            );

            try
            {
                await Task.Delay(2200, _dismissTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await Task.WhenAll(
                toast.FadeToAsync(0, 140, Easing.CubicIn),
                toast.TranslateToAsync(0, -10, 140, Easing.CubicIn)
            );

            if (_currentToast is not null)
            {
                layout.Children.Remove(_currentToast);
                _currentToast = null;
            }
        });
    }

    private static Border CreateToast(string message, NotificationPopupType type)
    {
        var (icon, color, bgColor) = type switch
        {
            NotificationPopupType.Success => (
                "\uF00C",
                Color.FromArgb("#2e7d32"),
                Color.FromArgb("#e8f5e9")
            ),
            NotificationPopupType.Error => (
                "\uF071",
                Color.FromArgb("#c62828"),
                Color.FromArgb("#ffebee")
            ),
            _ => ("\uF05A", Color.FromArgb("#1565c0"), Color.FromArgb("#e3f2fd")),
        };

        var border = new Border
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(16, 12, 16, 0),
            Padding = new Thickness(16, 10),
            BackgroundColor = bgColor,
            Stroke = color,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            InputTransparent = true,
            Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                },
                ColumnSpacing = 10,
            },
        };

        var grid = (Grid)border.Content;
        var iconLabel = new Label
        {
            Text = icon,
            FontFamily = "FaSolid",
            FontSize = 14,
            TextColor = color,
            VerticalOptions = LayoutOptions.Center,
        };
        var messageLabel = new Label
        {
            Text = message,
            TextColor = Color.FromArgb("#333333"),
            FontSize = 14,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.Center,
        };
        Grid.SetColumn(messageLabel, 1);
        grid.Children.Add(iconLabel);
        grid.Children.Add(messageLabel);

        return border;
    }
}
