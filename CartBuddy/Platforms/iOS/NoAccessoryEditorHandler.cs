using CoreGraphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace CartBuddy.Platforms.iOS;

public class NoAccessoryEditorHandler : EditorHandler
{
    protected override void ConnectHandler(MauiTextView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.InputAccessoryView = new UIView(CGRect.Empty);
    }
}
