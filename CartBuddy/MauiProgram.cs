using CartBuddy.Services;
using CartBuddy.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plugin.Maui.BottomSheet.Hosting;
using Refit;

namespace CartBuddy;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

#if DEBUG
        builder.Configuration.AddUserSecrets<App>();
#endif

        // Always allow CI / shell / host-level overrides
        builder.Configuration.AddEnvironmentVariables();

        builder
            .UseMauiApp<App>()
            .UseBottomSheet()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fa-solid-900.ttf", "FaSolid");
                fonts.AddFont("fa-regular-400.ttf", "FaRegular");
                fonts.AddFont("fa-light-300.ttf", "FaLight");
            });

        // Services
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<ICartBuddyApi>(_ =>
            RestService.For<ICartBuddyApi>(
                new HttpClient { BaseAddress = new Uri(Constants.CartBuddyServerBaseUrl) }
            )
        );
        builder.Services.AddSingleton<PreferencesService>();
        builder.Services.AddSingleton<AiCleanupService>();

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<StorePickerViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<StorePickerPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}