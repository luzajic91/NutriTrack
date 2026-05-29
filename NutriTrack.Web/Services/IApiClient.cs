namespace NutriTrack.Web.Services;

/// <summary>
/// A thin HTTP facade that attaches the current access token and converts
/// failures into <see cref="ApiException"/>, so feature services contain no
/// auth or error-handling boilerplate.
/// </summary>
public interface IApiClient
{
    Task<T> GetAsync<T>(string uri, CancellationToken ct = default);
    Task<T> PostAsync<T>(string uri, object body, CancellationToken ct = default);
    Task PutAsync(string uri, object body, CancellationToken ct = default);
    Task DeleteAsync(string uri, CancellationToken ct = default);
}
