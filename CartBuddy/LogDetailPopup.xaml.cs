using CartBuddy.Shared.Models;
using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public partial class LogDetailPopup : AppPopup
{
    public LogDetailPopup(ApiLogEntry entry)
    {
        InitializeComponent();

        MethodNameLabel.Text = entry.MethodName;
        MetadataLabel.Text = $"{entry.DirectionLabel} | {entry.Timestamp:yyyy-MM-dd HH:mm:ss} UTC";
        PayloadLabel.Text = string.IsNullOrWhiteSpace(entry.Payload) ? "No payload" : entry.Payload;
    }
}
