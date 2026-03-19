using Syncfusion.Maui.Core;

namespace CartBuddy.Services;

public interface IBusyOverlayService
{
    void Attach(ContentPage page);
    void Show(string message = "Loading...");
    void Hide();
}

public sealed class BusyOverlayService : IBusyOverlayService
{
    private WeakReference<ContentPage> _attachedPage;
    private Grid _overlay;
    private Label _messageLabel;

    public void Attach(ContentPage page)
    {
        MainThread.BeginInvokeOnMainThread(() => AttachCore(page));
    }

    public void Show(string message = "Loading...")
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_overlay is null)
            {
                return;
            }

            if (_messageLabel is not null)
            {
                _messageLabel.Text = message;
            }

            if (_overlay.IsVisible)
            {
                return;
            }

            _overlay.Opacity = 0;
            _overlay.IsVisible = true;
            await _overlay.FadeToAsync(1, 120, Easing.CubicInOut);
        });
    }

    public void Hide()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_overlay is null || !_overlay.IsVisible)
            {
                return;
            }

            await _overlay.FadeToAsync(0, 120, Easing.CubicInOut);
            _overlay.IsVisible = false;
            _overlay.Opacity = 1;
        });
    }

    private void AttachCore(ContentPage page)
    {
        if (_attachedPage?.TryGetTarget(out var existingPage) == true && ReferenceEquals(existingPage, page) && _overlay is not null)
        {
            return;
        }

        if (page.Content is not View content)
        {
            return;
        }

        var indicator = new SfBusyIndicator
        {
            IsRunning = true,
            WidthRequest = 72,
            HeightRequest = 72,
            IndicatorColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        _messageLabel = new Label
        {
            Text = "Loading...",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
        };

        var contentStack = new VerticalStackLayout
        {
            Spacing = 14,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { indicator, _messageLabel },
        };

        _overlay = new Grid
        {
            IsVisible = false,
            Opacity = 1,
            InputTransparent = false,
            Background = new SolidColorBrush(Color.FromArgb("#60000000")),
            Children = { contentStack },
        };

        var host = new Grid();
        host.Children.Add(content);
        host.Children.Add(_overlay);

        page.Content = host;
        _attachedPage = new WeakReference<ContentPage>(page);
    }
}
