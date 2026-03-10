using CartBuddy.Services;

namespace CartBuddy;

public partial class App : Application
{
    public App(PreferencesService preferences)
    {
        InitializeComponent();
        preferences.ApplyTheme();
    }

    protected override Window CreateWindow(IActivationState activationState) =>
        new(new AppShell());
}