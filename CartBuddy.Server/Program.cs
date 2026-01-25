using CartBuddy.Server;
using CartBuddy.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Services.AddMemoryCache();
builder.Services.AddScoped<SessionService>();
builder.Services.AddSingleton<KrogerTokenCache>();
builder.Services.AddHttpClient<KrogerService>(client =>
{
    client.BaseAddress = new Uri("https://api.kroger.com/v1/");
});

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

app.MapGet(
    "/api/search",
    async (
        string locationId,
        string term,
        KrogerService kroger,
        int start = 0,
        int limit = 5
    ) =>
    {
        var results = await kroger.SearchProducts(locationId, term, start, limit);
        return Results.Ok(results);
    }
);

app.MapPost(
    "/api/checkout",
    async (HttpContext context, CheckoutRequest req, SessionService session) =>
    {
        var state = session.SavePendingCheckout(req);

        var clientId = context.RequestServices.GetRequiredService<IConfiguration>()[
            "Kroger:ClientId"
        ];
        var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/api/oauth/callback";
        var authUrl =
            $"https://api.kroger.com/v1/connect/oauth2/authorize?scope=cart.basic:write&response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}";

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
        SessionService session
    ) =>
    {
        var pending = session.GetPendingCheckout(state);
        if (pending == null)
            return Results.BadRequest("Invalid state");

        session.ClearPendingCheckout(state);

        var redirectUri = $"{context.Request.Scheme}://{context.Request.Host}/api/oauth/callback";
        var userToken = await kroger.ExchangeCodeForToken(code, redirectUri);
        await kroger.AddToCart(userToken, pending.Items);

        return Results.Redirect("https://www.kroger.com/cart");
    }
);

app.MapFallbackToFile("index.html");

app.Run();