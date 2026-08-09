using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NutriTrack.Api;

namespace NutriTrack.Tests;

/// <summary>
/// Covers the configuration AddCore refuses to start without. These guards exist because the
/// failures they replace are all quiet: a signing key short enough to be brute-forced, or one
/// missing entirely, otherwise surfaces as broken logins rather than as a misconfiguration.
/// </summary>
public class StartupConfigurationTests
{
    private const string ValidSecret = "a-signing-key-that-is-long-enough-for-hmac-sha256";

    private static IConfiguration Config(
        string? jwtSecret = ValidSecret, string? clientBaseUrl = "http://localhost:5107") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=localhost;Database=NutriTrack;Trusted_Connection=True",
                ["Jwt:Secret"] = jwtSecret,
                ["Jwt:Issuer"] = "NutriTrack",
                ["Jwt:Audience"] = "NutriTrack",
                ["App:ClientBaseUrl"] = clientBaseUrl
            })
            .Build();

    private static Action AddCore(IConfiguration configuration) =>
        () => new ServiceCollection().AddCore(configuration);

    [Fact]
    public void FullyConfigured_DoesNotThrow()
    {
        AddCore(Config()).Should().NotThrow();
    }

    [Fact]
    public void MissingJwtSecret_Throws()
    {
        AddCore(Config(jwtSecret: null)).Should()
            .Throw<InvalidOperationException>().WithMessage("*Jwt:Secret is not configured*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankJwtSecret_Throws(string blank)
    {
        // The original guard was `configuration["Jwt:Secret"] ?? throw`, which only catches a
        // missing key. A blank entry reads back as an empty string, so it passed the check and
        // the application went on to sign tokens with a zero-length key.
        AddCore(Config(jwtSecret: blank)).Should()
            .Throw<InvalidOperationException>().WithMessage("*Jwt:Secret is not configured*");
    }

    [Fact]
    public void JwtSecretShorterThan256Bits_Throws()
    {
        AddCore(Config(jwtSecret: new string('k', 31))).Should()
            .Throw<InvalidOperationException>().WithMessage("*at least 32 bytes*");
    }

    [Fact]
    public void JwtSecretOfExactly256Bits_IsAccepted()
    {
        AddCore(Config(jwtSecret: new string('k', 32))).Should().NotThrow();
    }

    [Fact]
    public void MissingClientBaseUrl_Throws()
    {
        AddCore(Config(clientBaseUrl: null)).Should()
            .Throw<InvalidOperationException>().WithMessage("*App:ClientBaseUrl is not configured*");
    }
}
