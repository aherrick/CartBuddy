using Azure;
using Azure.AI.OpenAI;
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
builder.Services.AddSingleton<ApiLogger>();
builder.Services.AddSingleton<KrogerService>();

var config = builder.Configuration;
builder.Services.AddSingleton(_ =>
    new AzureOpenAIClient(
        new Uri(config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured")),
        new AzureKeyCredential(config["AzureOpenAI:Key"] ?? throw new InvalidOperationException("AzureOpenAI:Key not configured"))
    ).GetChatClient(config["AzureOpenAI:DeploymentName"] ?? "gpt-4o")
);
builder.Services.AddSingleton<AiCleanupService>();

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
