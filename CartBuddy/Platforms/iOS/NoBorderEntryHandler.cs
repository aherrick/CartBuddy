using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace CartBuddy.Platforms.iOS;

public class NoBorderEntryHandler : EntryHandler
{
    protected override void ConnectHandler(MauiTextField platformView)
    {
        base.ConnectHandler(platformView);
        platformView.BorderStyle = UITextBorderStyle.None;
        platformView.InputAccessoryView = null;
    }
}
