using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using System.Text.Json;

namespace URMS.Blazor.Services;

/// <summary>
/// Parses JWT token from localStorage and extracts claims.
/// Used by AuthStateService to restore login state on page refresh.
/// </summary>
public static class JwtParser
{
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims  = new List<Claim>();
        var payload = jwt.Split('.')[1];

        // Pad base64
        var rem = payload.Length % 4;
        if (rem != 0) payload += new string('=', 4 - rem);
        payload = payload.Replace('-', '+').Replace('_', '/');

        byte[] jsonBytes;
        try { jsonBytes = Convert.FromBase64String(payload); }
        catch { return claims; }

        var kv = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
        if (kv is null) return claims;

        foreach (var pair in kv)
        {
            var key = pair.Key;
            var val = pair.Value.GetString() ?? "";

            if (key.EndsWith("/role") || key == "role")
                claims.Add(new Claim(ClaimTypes.Role, val));
            else if (key.EndsWith("/nameidentifier") || key == "sub" || key == "nameid")
                claims.Add(new Claim(ClaimTypes.NameIdentifier, val));
            else if (key.EndsWith("/name") || key == "name" || key == "unique_name")
                claims.Add(new Claim(ClaimTypes.Name, val));
            else if (key == "email")
                claims.Add(new Claim(ClaimTypes.Email, val));
            else
                claims.Add(new Claim(key, val));
        }

        return claims;
    }
}

/// <summary>
/// Route guard - call GuardAsync("Examiner") in OnInitializedAsync
/// to redirect unauthenticated or wrong-role users.
/// </summary>
public class AuthGuard : ComponentBase
{
    [Inject] protected AuthStateService   Auth { get; set; } = null!;
    [Inject] protected NavigationManager Nav  { get; set; } = null!;

    protected async Task GuardAsync(string requiredRole)
    {
        await Auth.InitializeAsync();

        if (!Auth.IsAuthenticated)
        {
            Nav.NavigateTo("/login");
            return;
        }

        if (!string.Equals(Auth.CurrentUser!.Role, requiredRole, StringComparison.OrdinalIgnoreCase))
        {
            Nav.NavigateTo(Auth.CurrentUser.Role switch
            {
                "Examiner" => "/examiner",
                "HOD"      => "/hod",
                "Teacher"  => "/teacher",
                _          => "/login"
            });
        }
    }
}
