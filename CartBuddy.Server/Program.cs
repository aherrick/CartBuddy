using CartBuddy.Data.Services;
using CartBuddy.Server;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Services.AddMemoryCache();
builder.Services.AddCartBuddyRateLimiter();

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
app.UseRateLimiter();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapApiEndpoints();

app.MapFallbackToFile("index.html");

app.Run();
