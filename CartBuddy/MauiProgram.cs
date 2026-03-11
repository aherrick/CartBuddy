using CartBuddy.Services;
using CartBuddy.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plugin.Maui.BottomSheet.Hosting;
using Refit;
#if IOS || MACCATALYST
using Microsoft.Maui.Controls.Handlers.Items2;
using UIKit;
#endif

namespace CartBuddy;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

#if IOS || MACCATALYST
        CollectionViewHandler2.Mapper.AppendToMapping(
            "StickyGroupHeaders",
            (handler, view) =>
            {
                if (!view.IsGrouped)
                {
                    return;
                }

                if (handler.PlatformView is UICollectionView collectionView &&
                    collectionView.CollectionViewLayout is UICollectionViewFlowLayout layout)
                {
                    layout.SectionHeadersPinToVisibleBounds = true;
                    layout.InvalidateLayout();
                }
            }
        );
#endif

        builder
            .UseMauiApp<App>()
            .UseBottomSheet()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fa-solid-900.ttf", "FaSolid");
            });

        // Services
        builder.Services.AddRefitClient<ICartBuddyApi>()
            .ConfigureHttpClient(httpClient =>
            {
                httpClient.BaseAddress = new Uri(Constants.CartBuddyServerBaseUrl);
            });

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
