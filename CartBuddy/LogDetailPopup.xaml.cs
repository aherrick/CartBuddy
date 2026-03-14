using CartBuddy.Services;
using CartBuddy.Shared.Models;
using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public partial class LogDetailPopup : AppPopup
{
    private readonly string _payload;

    public LogDetailPopup(ApiLogEntry entry)
    {
        InitializeComponent();

        _payload = string.IsNullOrWhiteSpace(entry.Payload) ? string.Empty : entry.Payload;

        MethodNameLabel.Text = entry.MethodName;
        MetadataLabel.Text = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} UTC";
        DirectionLabel.Text = entry.DirectionLabel;
        EntryTypeLabel.Text = entry.EntryTypeLabel;
        PayloadLabel.Text = string.IsNullOrWhiteSpace(_payload) ? "No payload" : _payload;
    }

    private async void OnCopyPayloadClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_payload))
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(_payload);
        await NotificationPopupService.Show("Copied payload to clipboard", NotificationPopupType.Success);
    }
}
