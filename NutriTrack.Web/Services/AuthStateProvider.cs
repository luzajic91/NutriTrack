using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace NutriTrack.Web.Services;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public AuthStateProvider(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("accessToken");

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(_anonymous);
            }

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public void NotifyUserAuthentication(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();

        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return claims;

            var payload = parts[1];

            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            payload = payload.Replace('-', '+').Replace('_', '/');

            var jsonBytes = Convert.FromBase64String(payload);
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (keyValuePairs == null)
                return claims;

            if (keyValuePairs.TryGetValue("sub", out var sub))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.ToString()!));
            }

            keyValuePairs.TryGetValue(ClaimTypes.Role, out var roles);

            if (roles != null)
            {
                var rolesString = roles.ToString()!;

                if (rolesString.Trim().StartsWith("["))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(rolesString);
                    if (parsedRoles != null)
                    {
                        foreach (var role in parsedRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, rolesString));
                }
            }

            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Key == "sub" || kvp.Key == ClaimTypes.Role)
                    continue;

                claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
            }

            return claims;
        }
        catch
        {
            return claims;
        }
    }
}