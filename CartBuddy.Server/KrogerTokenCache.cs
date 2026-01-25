using System.Text;
using System.Text.Json;

namespace CartBuddy.Server;

public class KrogerTokenCache(IConfiguration configuration, IHttpClientFactory httpClientFactory)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string _clientToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public async Task<string> GetClientTokenAsync()
    {
        if (!string.IsNullOrEmpty(_clientToken) && DateTime.UtcNow < _tokenExpiry)
            return _clientToken;

        await _lock.WaitAsync();
        try
        {
            if (!string.IsNullOrEmpty(_clientToken) && DateTime.UtcNow < _tokenExpiry)
                return _clientToken;

            var clientId = configuration["Kroger:ClientId"];
            var clientSecret = configuration["Kroger:ClientSecret"];
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}")
            );

            using var httpClient = httpClientFactory.CreateClient();
            HttpRequestMessage request = new(
                HttpMethod.Post,
                "https://api.kroger.com/v1/connect/oauth2/token"
            );
            request.Headers.Authorization = new("Basic", credentials);
            request.Content = new FormUrlEncodedContent(
                [new("grant_type", "client_credentials"), new("scope", "product.compact")]
            );

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            _clientToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

            return _clientToken;
        }
        finally
        {
            _lock.Release();
        }
    }
}