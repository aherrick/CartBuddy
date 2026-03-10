using AspNetCoreRateLimit;
using CartBuddy.Data.Services;
using CartBuddy.Server;
using CartBuddy.Shared.Models;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Services.AddMemoryCache();

builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.GeneralRules =
    [
        new()
        {
            Endpoint = "*:/api/*",
            Period = "1m",
            Limit = 100,
        },
        new()
        {
            Endpoint = "POST:/api/checkout",
            Period = "5m",
            Limit = 10,
        },
    ];
});
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<KrogerClient>();
builder.Services.AddSingleton<KrogerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseIpRateLimiting();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet(
    "/api/location/{zipCode}",
    async (string zipCode, KrogerService kroger) =>
    {
        var locations = await kroger.GetLocationsByZip(zipCode);
        return Results.Ok(new LocationResponse { Locations = locations });
    }
);

app.MapGet("/api/health", () => Results.Ok());

app.MapGet(
    "/api/search",
    async (string locationId, string term, KrogerService kroger, int start = 0, int limit = 5) =>
    {
        var response = await kroger.SearchProducts(locationId, term, start, limit);
        return Results.Ok(response);
    }
);

app.MapPost(
    "/api/checkout",
    async (HttpContext context, CheckoutRequest req, IMemoryCache cache, KrogerService kroger) =>
    {
        var state = Guid.NewGuid().ToString();
        cache.Set(state, req, TimeSpan.FromMinutes(30));
        var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/api/oauth/callback";
        var authUrl = kroger.CreateAuthorizationUrl(redirectUri, "cart.basic:write", state);

        return Results.Ok(new CheckoutResponse { AuthUrl = authUrl });
    }
);

app.MapGet(
    "/api/oauth/callback",
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
            var redirectUri =
                $"{context.Request.Scheme}://{context.Request.Host}/api/oauth/callback";
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

app.MapFallbackToFile("index.html");

app.Run();

static string BuildCheckoutRedirect(string returnUri, bool isSuccess)
{
    if (string.IsNullOrWhiteSpace(returnUri))
    {
        return isSuccess ? "/?success=true" : "/?error=cart";
    }

    var separator = returnUri.Contains('?') ? '&' : '?';
    var outcome = isSuccess ? "success=true" : "error=cart";
    return $"{returnUri}{separator}{outcome}";
}
