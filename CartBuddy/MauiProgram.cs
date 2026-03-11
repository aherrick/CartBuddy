using CartBuddy.Services;
using CartBuddy.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plugin.Maui.BottomSheet.Hosting;
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
            .ConfigureSyncfusionCore()
            .UseBottomSheet()
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