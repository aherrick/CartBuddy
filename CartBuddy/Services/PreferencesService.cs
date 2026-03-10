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

    public static void ApplyTheme() => MainThread.BeginInvokeOnMainThread(() => Application.Current?.UserAppTheme = Theme);
}