using Blazored.LocalStorage;
using Blazored.Toast;
using CartBuddy.Client;
using CartBuddy.Client.Services;
using CurrieTechnologies.Razor.SweetAlert2;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();
builder.Services.AddSweetAlert2(options =>
{
    options.Theme = SweetAlertTheme.Dark;
});
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromMinutes(5),
});
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<CartStorageService>();

await builder.Build().RunAsync();