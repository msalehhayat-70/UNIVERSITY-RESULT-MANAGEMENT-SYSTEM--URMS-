using Blazored.LocalStorage;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using URMS.Models.DTOs;

namespace URMS.Blazor.Services;

public class AuthStateService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _storage;
    private bool _initialized;

    public LoginResponse? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;
    public string Greeting => DateTime.Now.Hour switch
    {
        >= 5 and < 12 => "Good Morning",
        >= 12 and < 17 => "Good Afternoon",
        >= 17 and < 21 => "Good Evening",
        _ => "Good Night"
    };
    public event Action? OnAuthChanged;

    public AuthStateService(HttpClient http, ILocalStorageService storage)
    { _http = http; _storage = storage; }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            var token = await _storage.GetItemAsStringAsync("token");
            var userJson = await _storage.GetItemAsync<LoginResponse>("user");
            if (!string.IsNullOrEmpty(token) && userJson is not null)
            {
                CurrentUser = userJson;
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch { }
    }

    public async Task<(bool ok, string message)> LoginAsync(LoginRequest request)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/auth/login", request);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
            if (result?.Data is null || !result.Success) return (false, result?.Message ?? "Login failed.");
            CurrentUser = result.Data;
            await _storage.SetItemAsStringAsync("token", result.Data.Token);
            await _storage.SetItemAsync("user", result.Data);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Data.Token);
            OnAuthChanged?.Invoke();
            return (true, "Login successful.");
        }
        catch (Exception ex) { return (false, $"Cannot connect to server: {ex.Message}"); }
    }

    public async Task<(bool ok, string message)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/auth/register", request);
            var result = await resp.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
            if (result?.Data is null || !result.Success) return (false, result?.Message ?? "Registration failed.");
            CurrentUser = result.Data;
            await _storage.SetItemAsStringAsync("token", result.Data.Token);
            await _storage.SetItemAsync("user", result.Data);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Data.Token);
            OnAuthChanged?.Invoke();
            return (true, result.Message);
        }
        catch (Exception ex) { return (false, $"Cannot connect: {ex.Message}"); }
    }

    public async Task<(int count, bool canRegister)> GetRegistrationStatusAsync()
    {
        try
        {
            var r = await _http.GetFromJsonAsync<RegistrationStatusResponse>("api/auth/registration-status");
            return (r?.RegisteredAccounts ?? 0, r?.CanRegister ?? true);
        }
        catch { return (0, true); }
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        _http.DefaultRequestHeaders.Authorization = null;
        _initialized = false;
        try { await _storage.RemoveItemAsync("token"); await _storage.RemoveItemAsync("user"); } catch { }
        OnAuthChanged?.Invoke();
    }

    public async Task UpdateNameAsync(string newName)
    {
        if (CurrentUser is null) return;
        CurrentUser.FullName = newName;
        // persist to local storage so refresh keeps the new name
        await _storage.SetItemAsync("user", CurrentUser);
        OnAuthChanged?.Invoke();
    }
}

public class RegistrationStatusResponse
{
    public int  RegisteredAccounts { get; set; }
    public bool CanRegister        { get; set; }
}
