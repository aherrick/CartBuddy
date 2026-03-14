using CartBuddy.Shared.Models;
using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public partial class LogDetailPopup : AppPopup
{
    private readonly ApiLogEntry _entry;

    public LogDetailPopup(ApiLogEntry entry)
    {
        InitializeComponent();

        _entry = entry;

        MethodNameLabel.Text = entry.MethodName;
        MetadataLabel.Text = $"{entry.DirectionLabel} | {entry.Timestamp:yyyy-MM-dd HH:mm:ss} UTC";
        DirectionLabel.Text = entry.DirectionLabel;
        DirectionBadge.BackgroundColor = GetDirectionColor(entry.Direction);
        PayloadLabel.Text = string.IsNullOrWhiteSpace(entry.Payload) ? "No payload" : entry.Payload;
    }

    private async void OnCopyPayloadClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_entry.Payload))
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(_entry.Payload);
    }

    private static Color GetDirectionColor(ApiLogDirection direction) =>
        direction switch
        {
            ApiLogDirection.Request => Color.FromArgb("#5B8DEF"),
            ApiLogDirection.Response => Color.FromArgb("#34C759"),
            ApiLogDirection.KrogerRequest => Color.FromArgb("#2F6FED"),
            ApiLogDirection.KrogerResponse => Color.FromArgb("#14B8A6"),
            _ => Color.FromArgb("#7C7C88")
        };
}
