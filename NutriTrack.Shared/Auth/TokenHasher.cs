namespace NutriTrack.Shared.Auth;

/// <summary>
/// Hashes bearer tokens for storage, so that reading the database yields nothing usable.
/// Refresh and confirmation tokens are 32–64 bytes of CSPRNG output, so a plain SHA-256 is
/// enough: there is no low-entropy secret to brute-force, and no reason to pay a password
/// hash's cost on every token refresh. Hex output is always 64 characters, which fits the
/// existing column width.
/// </summary>
public static class TokenHasher
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
