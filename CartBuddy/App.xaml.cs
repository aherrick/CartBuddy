namespace CartBuddy;

public partial class App : Application
{
    public App()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
            "Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXxfcXRcQ2VcVkRxWEpWYEs="
        );

        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState) => new(new AppShell());
}