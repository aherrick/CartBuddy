using CartBuddy.Shared.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CartBuddy.Server;

public class SessionService(IMemoryCache cache)
{
    public string SavePendingCheckout(CheckoutRequest checkoutRequest)
    {
        var state = Guid.NewGuid().ToString();
        cache.Set(state, checkoutRequest, TimeSpan.FromMinutes(30));
        return state;
    }

    public CheckoutRequest GetPendingCheckout(string state)
    {
        return cache.TryGetValue(state, out CheckoutRequest checkoutRequest)
            ? checkoutRequest
            : null;
    }

    public void ClearPendingCheckout(string state)
    {
        cache.Remove(state);
    }
}