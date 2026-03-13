using CartBuddy.ViewModels;
using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public partial class LogDetailPopup : AppPopup
{
    public LogDetailPopup(LogsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
