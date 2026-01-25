using System.Net.Http.Json;
using CurrieTechnologies.Razor.SweetAlert2;

namespace CartBuddy.Client.Services;

public class ApiService(HttpClient http, SweetAlertService swal)
{
    public async Task<T> GetAsync<T>(string url) =>
        await SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, url));

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data) =>
        await SendAsync<TResponse>(
            new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(data) }
        );

    private async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        try
        {
            var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await ShowError($"Request failed: {error}");
                return default;
            }
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            await ShowError($"Error: {ex.Message}");
            return default;
        }
    }

    private async Task ShowError(string message)
    {
        await swal.FireAsync(
            new SweetAlertOptions
            {
                Icon = SweetAlertIcon.Error,
                Title = "Oops...",
                Text = message,
                ConfirmButtonColor = "#198754",
            }
        );
    }
}