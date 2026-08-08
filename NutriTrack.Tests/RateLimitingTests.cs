using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using NutriTrack.Api.Controllers;
using NutriTrack.Api.RateLimiting;

namespace NutriTrack.Tests;

/// <summary>
/// Covers the two parts of rate limiting that can silently stop protecting anything: the
/// partition key (wrong key means one shared bucket, or none at all) and the per-action
/// opt-in (a missing attribute means no limit). The window arithmetic itself is the
/// framework's and is not re-tested here.
/// </summary>
public class RateLimitPartitionKeyTests
{
    private static HttpContext ContextWithIp(string? ip)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = ip is null ? null : IPAddress.Parse(ip);
        return context;
    }

    [Fact]
    public void KeyIsTheClientIp()
    {
        RateLimitPolicies.GetClientPartitionKey(ContextWithIp("203.0.113.7"))
            .Should().Be("203.0.113.7");
    }

    [Fact]
    public void DifferentClientsGetDifferentKeys()
    {
        var first = RateLimitPolicies.GetClientPartitionKey(ContextWithIp("203.0.113.7"));
        var second = RateLimitPolicies.GetClientPartitionKey(ContextWithIp("203.0.113.8"));

        // If these ever collapse to one key, one caller exhausts everyone's budget.
        first.Should().NotBe(second);
    }

    [Fact]
    public void SameClientGetsAStableKey()
    {
        RateLimitPolicies.GetClientPartitionKey(ContextWithIp("203.0.113.7"))
            .Should().Be(RateLimitPolicies.GetClientPartitionKey(ContextWithIp("203.0.113.7")));
    }

    [Fact]
    public void IpV6IsKeyedToo()
    {
        RateLimitPolicies.GetClientPartitionKey(ContextWithIp("2001:db8::1"))
            .Should().Be("2001:db8::1");
    }

    [Fact]
    public void UnknownClientFallsBackToAThrottledBucket()
    {
        var key = RateLimitPolicies.GetClientPartitionKey(ContextWithIp(null));

        // Must be a real key, not null or empty: an unresolvable caller is throttled
        // alongside other unresolvable callers rather than waved through unlimited.
        key.Should().Be(RateLimitPolicies.UnknownClientKey);
        key.Should().NotBeNullOrWhiteSpace();
    }
}

public class AuthControllerRateLimitCoverageTests
{
    private static IEnumerable<MethodInfo> ActionsOf<T>() =>
        typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);

    public static TheoryData<string> AnonymousAuthActions()
    {
        var data = new TheoryData<string>();
        foreach (var action in ActionsOf<AuthController>()
            .Where(m => m.GetCustomAttribute<AuthorizeAttribute>() is null))
        {
            data.Add(action.Name);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AnonymousAuthActions))]
    public void EveryAnonymousActionIsRateLimited(string actionName)
    {
        var action = ActionsOf<AuthController>().Single(m => m.Name == actionName);

        // This is the guard that matters: it fails when a new anonymous auth endpoint is
        // added without an attribute, which is how this protection quietly erodes.
        action.GetCustomAttribute<EnableRateLimitingAttribute>()
            .Should().NotBeNull(
                "anonymous auth endpoint {0} must declare a rate limit policy", actionName);
    }

    [Theory]
    [MemberData(nameof(AnonymousAuthActions))]
    public void EveryPolicyNameIsOneThatIsRegistered(string actionName)
    {
        var known = new[]
        {
            RateLimitPolicies.Mail, RateLimitPolicies.Credentials, RateLimitPolicies.Tokens
        };

        var policy = ActionsOf<AuthController>().Single(m => m.Name == actionName)
            .GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName;

        // An unregistered name throws at request time rather than at startup.
        known.Should().Contain(policy);
    }

    [Fact]
    public void TheSensitiveActionsUseTheExpectedPolicies()
    {
        PolicyFor(nameof(AuthController.Register)).Should().Be(RateLimitPolicies.Mail);
        PolicyFor(nameof(AuthController.ResendConfirmation)).Should().Be(RateLimitPolicies.Mail);
        PolicyFor(nameof(AuthController.Login)).Should().Be(RateLimitPolicies.Credentials);
        PolicyFor(nameof(AuthController.ConfirmEmail)).Should().Be(RateLimitPolicies.Tokens);
        PolicyFor(nameof(AuthController.RefreshToken)).Should().Be(RateLimitPolicies.Tokens);
    }

    [Fact]
    public void AuthenticatedRevokeIsNotLimited()
    {
        var revoke = ActionsOf<AuthController>().Single(m => m.Name == nameof(AuthController.RevokeToken));

        // Requires a valid JWT and has no abuse value; left unmetered deliberately.
        revoke.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        revoke.GetCustomAttribute<EnableRateLimitingAttribute>().Should().BeNull();
    }

    private static string? PolicyFor(string actionName) =>
        ActionsOf<AuthController>().Single(m => m.Name == actionName)
            .GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName;
}

public class RateLimitOptionsTests
{
    [Fact]
    public void DefaultsAreOrderedByWhatARequestCosts()
    {
        var options = new RateLimitOptions();

        options.Enabled.Should().BeTrue();

        // Mail sends an email per request, so it must stay the tightest of the three;
        // tokens must stay the loosest or normal clients refreshing a token get throttled.
        options.Mail.PermitLimit.Should().BeLessThan(options.Credentials.PermitLimit);
        options.Credentials.PermitLimit.Should().BeLessThan(options.Tokens.PermitLimit);
    }

    [Fact]
    public void EveryWindowIsUsable()
    {
        var options = new RateLimitOptions();

        foreach (var window in new[] { options.Mail, options.Credentials, options.Tokens })
        {
            window.PermitLimit.Should().BePositive();
            window.WindowMinutes.Should().BePositive();
            window.SegmentsPerWindow.Should().BePositive();
        }
    }

    [Fact]
    public void SegmentDurationDividesTheWindow()
    {
        new RateLimitWindow { WindowMinutes = 15, SegmentsPerWindow = 5 }
            .SegmentDuration().Should().Be(TimeSpan.FromMinutes(3));
    }

    [Fact]
    public void SegmentDurationSurvivesAMisconfiguredSegmentCount()
    {
        // Guards the Retry-After path against a divide-by-zero from bad configuration.
        new RateLimitWindow { WindowMinutes = 15, SegmentsPerWindow = 0 }
            .SegmentDuration().Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void SegmentDurationIsNeverLongerThanTheWindow()
    {
        var options = new RateLimitOptions();

        foreach (var window in new[] { options.Mail, options.Credentials, options.Tokens })
        {
            window.SegmentDuration().Should()
                .BePositive().And
                .BeLessThanOrEqualTo(TimeSpan.FromMinutes(window.WindowMinutes));
        }
    }
}
