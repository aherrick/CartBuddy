using System.Collections.Generic;
using System.Text.Json;
using CartBuddy.Models;

namespace CartBuddy.Services;

public static class PreferencesService
{
    public static string ZipCode
    {
        get => Preferences.Default.Get(nameof(ZipCode), "");
        set => Preferences.Default.Set(nameof(ZipCode), value);
    }

    public static string StoreId
    {
        get => Preferences.Default.Get(nameof(StoreId), "");
        set => Preferences.Default.Set(nameof(StoreId), value);
    }

    public static string StoreName
    {
        get => Preferences.Default.Get(nameof(StoreName), "");
        set => Preferences.Default.Set(nameof(StoreName), value);
    }

    public static AppTheme Theme
    {
        get => (AppTheme)Preferences.Default.Get(nameof(Theme), (int)AppTheme.Dark);
        set
        {
            Preferences.Default.Set(nameof(Theme), (int)value);
            MainThread.BeginInvokeOnMainThread(() => Application.Current?.UserAppTheme = value);
        }
    }

    public static bool HasStore => !string.IsNullOrEmpty(StoreId);

    public static bool UseAiCleanup
    {
        get => Preferences.Default.Get(nameof(UseAiCleanup), false);
        set => Preferences.Default.Set(nameof(UseAiCleanup), value);
    }

    public static List<CartLine> Cart
    {
        get
        {
            var json = Preferences.Default.Get(nameof(Cart), "");
            if (string.IsNullOrEmpty(json))
            {
                return [];
            }
            return JsonSerializer.Deserialize<List<CartLine>>(json);
        }
        set => Preferences.Default.Set(nameof(Cart), JsonSerializer.Serialize(value));
    }

    public static void ApplyTheme() =>
        MainThread.BeginInvokeOnMainThread(() => Application.Current?.UserAppTheme = Theme);
}
