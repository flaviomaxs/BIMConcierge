using BIMConcierge.Core.Interfaces;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BIMConcierge.Infrastructure.Api;

public interface IBimApiClient
{
    Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken ct = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default);
    Task DeleteAsync(string endpoint, CancellationToken ct = default);
}

public static class ApiSettings
{
    // Override via environment variable BIMCONCIERGE_API_URL for on-premise deployments
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("BIMCONCIERGE_API_URL")
        ?? "https://bimconcierge.onrender.com/v1/";
}

public class BimApiClient : IBimApiClient
{
    private readonly HttpClient _http;
    private readonly ITokenStore _tokens;

    public BimApiClient(HttpClient http, ITokenStore tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AttachAuthHeader(request);
        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        return await Deserialize<TResponse>(response).ConfigureAwait(false);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        AttachAuthHeader(request);
        request.Content = Serialize(body);
        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        return await Deserialize<TResponse>(response).ConfigureAwait(false);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
        AttachAuthHeader(request);
        request.Content = Serialize(body);
        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        return await Deserialize<TResponse>(response).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        AttachAuthHeader(request);
        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void AttachAuthHeader(HttpRequestMessage request)
    {
        var token = _tokens.AccessToken;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static StringContent Serialize<T>(T obj) =>
        new(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");

    private static async Task<T?> Deserialize<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
