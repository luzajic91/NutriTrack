using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NutriTrack.Api.Cors;

namespace NutriTrack.Tests;

/// <summary>
/// The policy used to be AllowAnyOrigin, which let any page on the internet call the API and
/// use a stolen bearer token without restriction. These pin it to the client origin.
/// </summary>
public class CorsConfigurationTests
{
    private const string ClientUrl = "http://localhost:5107";

    private static CorsPolicy BuildDefaultPolicy(string? clientBaseUrl = ClientUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:ClientBaseUrl"] = clientBaseUrl
            })
            .Build();

        var provider = new ServiceCollection()
            .AddNutriTrackCors(configuration)
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        return options.GetPolicy(options.DefaultPolicyName)!;
    }

    [Fact]
    public void DefaultPolicy_AllowsOnlyTheClientOrigin()
    {
        var policy = BuildDefaultPolicy();

        policy.AllowAnyOrigin.Should().BeFalse();
        policy.Origins.Should().BeEquivalentTo([ClientUrl]);
    }

    [Fact]
    public void DefaultPolicy_DoesNotAllowAForeignOrigin()
    {
        BuildDefaultPolicy().Origins.Should().NotContain("https://evil.example");
    }

    [Fact]
    public void DefaultPolicy_SupportsCredentials()
    {
        // Without this the browser never attaches the refresh cookie, and every refresh fails.
        // It is legal only alongside a named origin — the framework rejects it with
        // AllowAnyOrigin, which is a second reason the origin above stays pinned.
        BuildDefaultPolicy().SupportsCredentials.Should().BeTrue();
    }

    [Fact]
    public void DefaultPolicy_StillAllowsAnyMethodAndHeader()
    {
        var policy = BuildDefaultPolicy();

        // Restricting the origin should not have restricted the verbs or the Authorization
        // header the client actually needs.
        policy.AllowAnyMethod.Should().BeTrue();
        policy.AllowAnyHeader.Should().BeTrue();
    }

    [Fact]
    public void ATrailingSlashOnTheConfiguredUrl_IsTrimmed()
    {
        // WithOrigins matches the header verbatim, and browsers never send a trailing slash,
        // so leaving one in configuration would silently refuse every request.
        BuildDefaultPolicy($"{ClientUrl}/").Origins.Should().BeEquivalentTo([ClientUrl]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MissingClientBaseUrl_Throws(string? missing)
    {
        var act = () => BuildDefaultPolicy(missing);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*App:ClientBaseUrl is not configured*");
    }
}
