using System.Net.Http.Json;
using Blazored.LocalStorage;
using URMS.Models.DTOs;
namespace URMS.Blazor.Services;
public class StudentApiService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    public StudentApiService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }
    private async Task EnsureAuthorizationAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("token");
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
    public async Task<List<StudentDto>> GetByBatchAsync(int batchId)
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<StudentDto>>>($"api/students/batch/{batchId}");
        return r?.Data ?? new();
    }
    public async Task<(bool success, string message)> AddStudentAsync(CreateStudentRequest request)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.PostAsJsonAsync("api/students", request);
        var r = await response.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }
    public async Task<(bool success, string message)> DeactivateStudentAsync(int studentId)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.DeleteAsync($"api/students/{studentId}");
        var r = await response.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }
    public async Task<(bool success, string message)> ImportStudentsAsync(ImportStudentsRequest request)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.PostAsJsonAsync("api/students/import", request);
        var r = await response.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }
}