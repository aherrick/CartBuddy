using Blazored.LocalStorage;

namespace CartBuddy.Client.Services;

public class DraftStorageService(ILocalStorageService localStorage)
{
    private const string ItemsTextStorageKey = "cartBuddy.itemsText";
    private const string ZipCodeStorageKey = "cartBuddy.zipCode";

    public async Task<string> GetItemsTextAsync() =>
        await localStorage.GetItemAsync<string>(ItemsTextStorageKey) ?? "";

    public async Task SaveItemsTextAsync(string itemsText)
    {
        if (string.IsNullOrWhiteSpace(itemsText))
        {
            await ClearItemsTextAsync();
            return;
        }

        await localStorage.SetItemAsync(ItemsTextStorageKey, itemsText);
    }

    public async Task ClearItemsTextAsync() =>
        await localStorage.RemoveItemAsync(ItemsTextStorageKey);

    public async Task<string> GetZipCodeAsync() =>
        await localStorage.GetItemAsync<string>(ZipCodeStorageKey) ?? "";

    public async Task SaveZipCodeAsync(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
        {
            await ClearZipCodeAsync();
            return;
        }

        await localStorage.SetItemAsync(ZipCodeStorageKey, zipCode);
    }

    public async Task ClearZipCodeAsync() =>
        await localStorage.RemoveItemAsync(ZipCodeStorageKey);
}