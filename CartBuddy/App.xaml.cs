namespace CartBuddy;

public partial class App : Application
{
    public App()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
            "Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXxfdnRdQmBYVEFzWkZWYEo="
        );

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState) => new(new AppShell());
}