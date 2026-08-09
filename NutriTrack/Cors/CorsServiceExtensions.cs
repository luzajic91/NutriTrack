namespace NutriTrack.Api.Cors;

/// <summary>
/// Registers the default CORS policy. The API answers exactly one origin — the Blazor client —
/// rather than any origin: bearer tokens mean a permissive policy is not classic CSRF, but it
/// would still let any page on the internet probe the API and use a stolen token freely.
/// </summary>
public static class CorsServiceExtensions
{
    public static IServiceCollection AddNutriTrackCors(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Same setting that builds confirmation links, so one value answers "where does the
        // client live". Guarded here rather than relying on AddCore, which runs afterwards.
        var clientBaseUrl = (configuration["App:ClientBaseUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(clientBaseUrl))
            throw new InvalidOperationException(
                "App:ClientBaseUrl is not configured; the CORS policy is built from it.");

        // WithOrigins matches scheme, host and port exactly, so switching launch profile means
        // updating this setting — the same trade-off confirmation links already make.
        // AllowCredentials is what lets the browser send the refresh token cookie to the API.
        // It is only legal alongside a named origin — the framework rejects it combined with
        // AllowAnyOrigin, which is one more reason the policy above is pinned.
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(clientBaseUrl)
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader()));

        return services;
    }
}
