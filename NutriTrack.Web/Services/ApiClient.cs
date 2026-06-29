using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NutriTrack.Shared.Services;

namespace NutriTrack.Web.Services;

/// <inheritdoc cref="IApiClient" />
public class ApiClient : IApiClient
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    private readonly HttpClient _http;
    private readonly IAuthService _auth;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, IAuthService auth, ILogger<ApiClient> logger)
    {
        _http = http;
        _auth = auth;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string uri, CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Get, uri, body: null, ct);
        return await ReadAsync<T>(response, ct);
    }

    public async Task<T> PostAsync<T>(string uri, object body, CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Post, uri, body, ct);
        return await ReadAsync<T>(response, ct);
    }

    public async Task PutAsync(string uri, object body, CancellationToken ct = default) =>
        await SendAsync(HttpMethod.Put, uri, body, ct);

    public async Task DeleteAsync(string uri, CancellationToken ct = default) =>
        await SendAsync(HttpMethod.Delete, uri, body: null, ct);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync()
            ?? throw new ApiException(401, "You are not signed in.");

        // Set the token per-request (reliable in Blazor WebAssembly). The request is
        // intentionally not disposed before the caller reads the response body.
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        // Never log the bearer token or request body.
        _logger.LogDebug("API request {Method} {Uri}", method, uri);

        var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
            return response;

        var correlationId = response.Headers.TryGetValues(CorrelationIdHeader, out var ids)
            ? string.Join(',', ids)
            : null;
        var error = await response.Content.ReadAsStringAsync(ct);

        _logger.LogWarning(
            "API request {Method} {Uri} failed: {StatusCode} (correlationId={CorrelationId})",
            method, uri, (int)response.StatusCode, correlationId);

        throw new ApiException(
            (int)response.StatusCode,
            string.IsNullOrWhiteSpace(error) ? response.ReasonPhrase ?? "Request failed." : error);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new ApiException((int)response.StatusCode, "The server returned an empty response.");
}
