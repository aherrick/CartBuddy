namespace CartBuddy;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(StorePickerPage), typeof(StorePickerPage));
        Routing.RegisterRoute(nameof(LogsPage), typeof(LogsPage));
    }
}