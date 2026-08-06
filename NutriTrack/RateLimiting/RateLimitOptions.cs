namespace NutriTrack.Api.RateLimiting;

/// <summary>
/// Rate limiting settings, bound from the "RateLimiting" configuration section.
/// The defaults here are the shipping values; appsettings only needs to carry overrides.
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Turns every policy off when false. Development sets this so local work and the
    /// Scalar UI are never throttled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Endpoints that send an outbound email per request.</summary>
    public RateLimitWindow Mail { get; set; } = new() { PermitLimit = 3, WindowMinutes = 15 };

    /// <summary>Password submission. Higher than <see cref="Mail"/> to tolerate mistyping.</summary>
    public RateLimitWindow Credentials { get; set; } = new() { PermitLimit = 10, WindowMinutes = 15 };

    /// <summary>
    /// Token exchange. Deliberately generous: every active client hits refresh-token when
    /// its access token expires, and a shared NAT egress multiplies that across users.
    /// </summary>
    public RateLimitWindow Tokens { get; set; } = new() { PermitLimit = 60, WindowMinutes = 15 };
}

/// <summary>One sliding window: how many requests, over how long.</summary>
public class RateLimitWindow
{
    public int PermitLimit { get; set; }
    public int WindowMinutes { get; set; }

    /// <summary>
    /// Segments the window is divided into. More segments means the limit slides more
    /// smoothly at the cost of memory per partition.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 5;

    /// <summary>
    /// How long one segment lasts — the soonest expiring capacity can free up, which is what
    /// a throttled caller is told to wait via Retry-After.
    /// </summary>
    public TimeSpan SegmentDuration() =>
        TimeSpan.FromMinutes((double)WindowMinutes / Math.Max(SegmentsPerWindow, 1));
}
