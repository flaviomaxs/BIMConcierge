using BIMConcierge.Infrastructure.Auth;
using Newtonsoft.Json;
using System.Text;

namespace BIMConcierge.Infrastructure.Api;

public interface IBimApiClient
{
    Task<TResponse?> GetAsync<TResponse>(string endpoint);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body);
    Task DeleteAsync(string endpoint);
}

public static class ApiSettings
{
    // Override via environment variable BIMCONCIERGE_API_URL for on-premise deployments
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("BIMCONCIERGE_API_URL")
        ?? "https://api.bimconcierge.io/v1/";
}

public class BimApiClient : IBimApiClient
{
    private readonly HttpClient  _http;
    private readonly ITokenStore _tokens;

    public BimApiClient(HttpClient http, ITokenStore tokens)
    {
        _http   = http;
        _tokens = tokens;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint)
    {
        AttachAuthHeader();
        var response = await _http.GetAsync(endpoint);
        return await Deserialize<TResponse>(response);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        AttachAuthHeader();
        var content  = Serialize(body);
        var response = await _http.PostAsync(endpoint, content);
        return await Deserialize<TResponse>(response);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        AttachAuthHeader();
        var content  = Serialize(body);
        var response = await _http.PutAsync(endpoint, content);
        return await Deserialize<TResponse>(response);
    }

    public async Task DeleteAsync(string endpoint)
    {
        AttachAuthHeader();
        await _http.DeleteAsync(endpoint);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void AttachAuthHeader()
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrEmpty(_tokens.AccessToken))
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_tokens.AccessToken}");
    }

    private static StringContent Serialize<T>(T obj) =>
        new(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");

    private static async Task<T?> Deserialize<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(json);
    }
}
