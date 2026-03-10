using Microsoft.Extensions.Configuration;

namespace CartBuddy.Services;

public class PreferencesService(IConfiguration config)
{
    public string ZipCode
    {
        get => Preferences.Default.Get(nameof(ZipCode), "");
        set => Preferences.Default.Set(nameof(ZipCode), value);
    }

    public string StoreId
    {
        get => Preferences.Default.Get(nameof(StoreId), "");
        set => Preferences.Default.Set(nameof(StoreId), value);
    }

    public string StoreName
    {
        get => Preferences.Default.Get(nameof(StoreName), "");
        set => Preferences.Default.Set(nameof(StoreName), value);
    }

    public AppTheme Theme
    {
        get => (AppTheme)Preferences.Default.Get(nameof(Theme), (int)AppTheme.Dark);
        set
        {
            Preferences.Default.Set(nameof(Theme), (int)value);
            MainThread.BeginInvokeOnMainThread(() => Application.Current?.UserAppTheme = value);
        }
    }

    public bool HasStore => !string.IsNullOrEmpty(StoreId);

    public bool IsAiConfigured =>
        !string.IsNullOrEmpty(config["AzureOpenAI:Endpoint"])
        && !string.IsNullOrEmpty(config["AzureOpenAI:Key"]);

    public void ApplyTheme() => MainThread.BeginInvokeOnMainThread(() => Application.Current?.UserAppTheme = Theme);
}