using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace NutriTrack.Api.RateLimiting;

/// <summary>
/// Registers the per-IP sliding-window limiters guarding the anonymous auth endpoints.
/// Counters are held in memory, which is correct for a single instance; scaling out would
/// multiply every limit by the instance count and needs a distributed store instead.
/// </summary>
public static class RateLimiterServiceExtensions
{
    private const string LoggerCategory = "NutriTrack.Api.RateLimiting";

    public static IServiceCollection AddNutriTrackRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitOptions.SectionName)
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        // Retry-After hint per policy, populated as policies are added. See ResolveRetryAfter.
        var retryAfterHints = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);

        services.AddRateLimiter(limiter =>
        {
            // The framework default is 503; a throttled caller should be told 429.
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPerClientPolicy(
                RateLimitPolicies.Mail, options.Mail, options.Enabled, retryAfterHints);
            limiter.AddPerClientPolicy(
                RateLimitPolicies.Credentials, options.Credentials, options.Enabled, retryAfterHints);
            limiter.AddPerClientPolicy(
                RateLimitPolicies.Tokens, options.Tokens, options.Enabled, retryAfterHints);

            limiter.OnRejected = (context, ct) => OnRejectedAsync(context, retryAfterHints);
        });

        return services;
    }

    /// <summary>
    /// Adds a sliding-window policy partitioned by client IP. A fixed window would permit a
    /// double burst either side of the boundary, which is exactly the traffic shape these
    /// limits exist to stop.
    /// </summary>
    private static void AddPerClientPolicy(
        this RateLimiterOptions limiter,
        string policyName,
        RateLimitWindow window,
        bool enabled,
        Dictionary<string, TimeSpan> retryAfterHints)
    {
        retryAfterHints[policyName] = window.SegmentDuration();

        limiter.AddPolicy(policyName, context =>
        {
            var partitionKey = RateLimitPolicies.GetClientPartitionKey(context);

            // Policies are still registered when disabled — an endpoint referencing a policy
            // that does not exist throws at request time rather than running unlimited.
            if (!enabled)
                return RateLimitPartition.GetNoLimiter(partitionKey);

            return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = window.PermitLimit,
                    Window = TimeSpan.FromMinutes(window.WindowMinutes),
                    SegmentsPerWindow = window.SegmentsPerWindow,

                    // Never queue. Holding an auth request open just delays the 429.
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        });
    }

    /// <summary>
    /// Writes the same <c>ApiErrorResponse</c> body every other failure uses, so the Blazor
    /// client reads a throttle through its normal error path.
    /// </summary>
    private static ValueTask OnRejectedAsync(
        OnRejectedContext context, Dictionary<string, TimeSpan> retryAfterHints)
    {
        var httpContext = context.HttpContext;

        if (ResolveRetryAfter(context, retryAfterHints) is { } retryAfter)
            httpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        // Logged in full rather than through LogMasking: a masked address is useless for the
        // forensics this line exists to support.
        httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LoggerCategory)
            .LogWarning(
                "Rate limit rejected {Method} {Path} for client {ClientKey}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                RateLimitPolicies.GetClientPartitionKey(httpContext));

        return new ValueTask(httpContext.Response.WriteErrorAsync(
            StatusCodes.Status429TooManyRequests,
            RateLimitPolicies.RejectedMessage,
            RateLimitPolicies.RejectedErrorCode));
    }

    /// <summary>
    /// How long the caller should wait before retrying.
    /// </summary>
    /// <remarks>
    /// <see cref="SlidingWindowRateLimiter"/> advertises <c>RETRY_AFTER</c> in
    /// <see cref="RateLimitLease.MetadataNames"/> but never populates it on a rejected lease —
    /// unlike the fixed-window and token-bucket limiters, which do. So fall back to the
    /// segment duration: capacity in a sliding window can only free when a segment rolls off,
    /// making that the soonest a retry could succeed, which is exactly what Retry-After is
    /// defined to convey. The metadata is still preferred if a future runtime supplies it.
    /// </remarks>
    private static TimeSpan? ResolveRetryAfter(
        OnRejectedContext context, Dictionary<string, TimeSpan> retryAfterHints)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var fromLimiter))
            return fromLimiter;

        var policyName = context.HttpContext.GetEndpoint()
            ?.Metadata.GetMetadata<EnableRateLimitingAttribute>()
            ?.PolicyName;

        return policyName is not null && retryAfterHints.TryGetValue(policyName, out var hint)
            ? hint
            : null;
    }
}
