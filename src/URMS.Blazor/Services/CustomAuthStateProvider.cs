using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Blazored.LocalStorage;

namespace URMS.Blazor.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly AuthStateService _authStateService;

    public CustomAuthStateProvider(ILocalStorageService localStorage, AuthStateService authStateService)
    {
        _localStorage = localStorage;
        _authStateService = authStateService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("token");
        var identity = new ClaimsIdentity();

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var claims = ParseJwt(token);
                identity = new ClaimsIdentity(claims, "jwt");
            }
            catch
            {
                // Token invalid, clear it
                await _localStorage.RemoveItemAsync("token");
                await _localStorage.RemoveItemAsync("user");
            }
        }

        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    private List<Claim> ParseJwt(string token)
    {
        var claims = new List<Claim>();
        try
        {
            var payload = token.Split('.')[1];
            var jsonBytes = Convert.FromBase64String(payload + new string('=', (4 - payload.Length % 4) % 4));
            var keyValuePairs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    var claimValue = kvp.Value switch
                    {
                        System.Text.Json.JsonElement jElement => jElement.GetString(),
                        _ => kvp.Value?.ToString()
                    };

                    if (!string.IsNullOrEmpty(claimValue))
                    {
                        claims.Add(new Claim(kvp.Key, claimValue));
                    }
                }
            }
        }
        catch
        {
            // Silently fail if JWT parsing fails
        }

        return claims;
    }
}