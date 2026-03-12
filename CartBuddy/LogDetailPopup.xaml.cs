using CartBuddy.ViewModels;
using CommunityToolkit.Maui.Views;

namespace CartBuddy;

public partial class LogDetailPopup : Popup
{
    public LogDetailPopup(LogsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
