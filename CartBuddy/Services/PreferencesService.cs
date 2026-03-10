using Microsoft.Extensions.Configuration;

namespace CartBuddy.Services;

public class PreferencesService(IConfiguration config)
{
    private const string ZipCodeKey = "zip_code";
    private const string StoreIdKey = "store_id";
    private const string StoreNameKey = "store_name";
    private const string ThemeKey = "theme";

    public string ZipCode
    {
        get => Preferences.Default.Get(ZipCodeKey, "");
        set => Preferences.Default.Set(ZipCodeKey, value);
    }

    public string StoreId
    {
        get => Preferences.Default.Get(StoreIdKey, "");
        set => Preferences.Default.Set(StoreIdKey, value);
    }

    public string StoreName
    {
        get => Preferences.Default.Get(StoreNameKey, "");
        set => Preferences.Default.Set(StoreNameKey, value);
    }

    public AppTheme Theme
    {
        get => (AppTheme)Preferences.Default.Get(ThemeKey, (int)AppTheme.Dark);
        set
        {
            Preferences.Default.Set(ThemeKey, (int)value);
            if (Application.Current is not null)
            {
                Application.Current.UserAppTheme = value;
            }
        }
    }

    public bool HasStore => !string.IsNullOrEmpty(StoreId);

    public bool IsAiConfigured =>
        !string.IsNullOrEmpty(config["AzureOpenAI:Endpoint"])
        && !string.IsNullOrEmpty(config["AzureOpenAI:Key"]);

    public void ApplyTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = Theme;
        }
    }
}