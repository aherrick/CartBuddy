using Blazored.LocalStorage;
using CartBuddy.Shared.Models;

namespace CartBuddy.Client.Services;

public class CartStorageService(ILocalStorageService localStorage)
{
    private const string CartStorageKey = "cartBuddy.cart";

    public async Task<List<CartItem>> GetCartAsync() =>
        await localStorage.GetItemAsync<List<CartItem>>(CartStorageKey) ?? [];

    public async Task SaveCartAsync(List<CartItem> cart)
    {
        if (cart.Count == 0)
        {
            await ClearCartAsync();
            return;
        }

        await localStorage.SetItemAsync(CartStorageKey, cart);
    }

    public async Task ClearCartAsync() =>
        await localStorage.RemoveItemAsync(CartStorageKey);
}