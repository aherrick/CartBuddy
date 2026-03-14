using CartBuddy.Services;
using CartBuddy.ViewModels;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Refit;
using Syncfusion.Maui.Core.Hosting;

namespace CartBuddy;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit(options =>
            {
                options.SetShouldEnableSnackbarOnWindows(true);
                options.SetPopupOptionsDefaults(new DefaultPopupOptionsSettings
                {
                    PageOverlayColor = Colors.Transparent,
                    Shape = null,
                    Shadow = null,
                });
            })
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("fa-solid-900.ttf", "FaSolid");
            });

        // Services
        builder
            .Services.AddRefitClient<ICartBuddyApi>()
            .ConfigureHttpClient(httpClient =>
            {
                httpClient.BaseAddress = new Uri(Constants.CartBuddyServerBaseUrl);
            });
        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<StorePickerViewModel>();
        builder.Services.AddTransient<LogsViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<StorePickerPage>();
        builder.Services.AddTransient<LogsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
