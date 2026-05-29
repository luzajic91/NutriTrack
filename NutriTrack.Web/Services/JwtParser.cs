using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace NutriTrack.Web.Services;

/// <summary>
/// Decodes JSON Web Tokens on the client. Centralizes the base64url payload
/// decoding shared by token-expiry checks and claim extraction.
/// </summary>
public static class JwtParser
{
    /// <summary>
    /// True if the token is missing/malformed, has no expiry, or expires within
    /// <paramref name="leeway"/> of now.
    /// </summary>
    public static bool IsExpired(string token, TimeSpan leeway)
    {
        var json = DecodePayloadJson(token);
        if (json is null)
            return true;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("exp", out var exp))
                return true;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());
            return expiresAt < DateTimeOffset.UtcNow.Add(leeway);
        }
        catch
        {
            return true;
        }
    }

    /// <summary>Extracts claims from the token; returns an empty set if it cannot be parsed.</summary>
    public static IEnumerable<Claim> ParseClaims(string token)
    {
        var claims = new List<Claim>();

        var json = DecodePayloadJson(token);
        if (json is null)
            return claims;

        try
        {
            var pairs = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (pairs is null)
                return claims;

            if (pairs.TryGetValue("sub", out var sub))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.ToString()!));

            if (pairs.TryGetValue(ClaimTypes.Role, out var roles) && roles is not null)
            {
                var rolesString = roles.ToString()!;
                if (rolesString.Trim().StartsWith('['))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(rolesString);
                    foreach (var role in parsedRoles ?? [])
                        claims.Add(new Claim(ClaimTypes.Role, role));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, rolesString));
                }
            }

            foreach (var kvp in pairs)
            {
                if (kvp.Key is "sub" || kvp.Key == ClaimTypes.Role)
                    continue;
                claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
            }
        }
        catch
        {
            // Return whatever was parsed before the failure.
        }

        return claims;
    }

    private static string? DecodePayloadJson(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            payload = payload.Replace('-', '+').Replace('_', '/');

            return Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        }
        catch
        {
            return null;
        }
    }
}
