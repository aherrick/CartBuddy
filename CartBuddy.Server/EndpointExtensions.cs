using CartBuddy.Shared.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CartBuddy.Server;

public static class EndpointExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api");

        apiGroup.MapGet(
            "/location/{zipCode}",
            async (string zipCode, KrogerService kroger) =>
            {
                var locations = await kroger.GetLocationsByZip(zipCode);
                return Results.Ok(new LocationResponse { Locations = locations });
            }
        );

        apiGroup.MapGet("/health", () => Results.Ok());

        apiGroup.MapGet("/logs", (ApiLogger logger) => Results.Ok(logger.GetAll()));

        apiGroup.MapGet(
            "/search",
            async (string locationId, string term, KrogerService kroger, int start = 0, int limit = 10) =>
            {
                var response = await kroger.SearchProducts(locationId, term, start, limit);
                return Results.Ok(response);
            }
        );

        apiGroup.MapPost(
            "/cleanup",
            async (CleanupRequest req, AiCleanupService aiCleanup) =>
            {
                var cleanedItems = await aiCleanup.CleanupList(req.Items ?? []);
                return Results.Ok(new CleanupResponse { CleanedItems = cleanedItems });
            }
        );

        apiGroup.MapPost(
            "/checkout",
            async (HttpContext context, CheckoutRequest req, IMemoryCache cache, KrogerService kroger) =>
            {
                var state = Guid.NewGuid().ToString();
                cache.Set(state, req, TimeSpan.FromMinutes(30));
                var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/api/oauth/callback";
                var authUrl = kroger.CreateAuthorizationUrl(redirectUri, "cart.basic:write", state);
                return Results.Ok(new CheckoutResponse { AuthUrl = authUrl });
            }
        ).RequireRateLimiting("checkout");

        apiGroup.MapGet(
            "/oauth/callback",
            async (
                HttpContext context,
                string code,
                string state,
                KrogerService kroger,
                IMemoryCache cache,
                ILogger<Program> logger
            ) =>
            {
                if (!cache.TryGetValue(state, out CheckoutRequest pending))
                {
                    return Results.BadRequest("Invalid state");
                }

                cache.Remove(state);

                try
                {
                    var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/api/oauth/callback";
                    var userToken = await kroger.ExchangeCodeForToken(code, redirectUri);
                    await kroger.CreateCart(userToken, pending.Items);

                    return Results.Redirect(BuildCheckoutRedirect(pending.ReturnUri, true));
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex, "Kroger cart creation failed");
                    return Results.Redirect(BuildCheckoutRedirect(pending.ReturnUri, false));
                }
            }
        );

        return app;
    }

    private static string BuildCheckoutRedirect(string returnUri, bool isSuccess)
    {
        if (string.IsNullOrWhiteSpace(returnUri))
        {
            return isSuccess ? "/?success=true" : "/?error=cart";
        }

        var separator = returnUri.Contains('?') ? '&' : '?';
        var outcome = isSuccess ? "success=true" : "error=cart";
        return $"{returnUri}{separator}{outcome}";
    }
}
