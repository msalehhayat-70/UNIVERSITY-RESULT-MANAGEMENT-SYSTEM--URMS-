using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using URMS.Models.DTOs;

namespace URMS.Blazor.Services;

public class ExaminerApiService
{
    private readonly HttpClient _http;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    public ExaminerApiService(HttpClient http, AuthenticationStateProvider authStateProvider, ILocalStorageService localStorage)
    {
        _http = http;
        _authStateProvider = authStateProvider;
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

    public async Task<ExaminerDashboardDto?> GetDashboardAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<ExaminerDashboardDto>>("api/examiner/dashboard");
        return r?.Data;
    }

    public async Task<List<BatchDto>> GetBatchesAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<BatchDto>>>("api/examiner/batches");
        return r?.Data ?? new();
    }

    public async Task<BatchDto?> CreateBatchAsync(CreateBatchRequest request)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.PostAsJsonAsync("api/examiner/batches", request);
        if (!response.IsSuccessStatusCode) return null;
        var r = await response.Content.ReadFromJsonAsync<ApiResponse<BatchDto>>();
        return r?.Data;
    }

    public async Task<List<SubjectConfigDto>> GetSubjectsAsync(int batchSemesterId)
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<SubjectConfigDto>>>(
            $"api/examiner/semesters/{batchSemesterId}/subjects");
        return r?.Data ?? new();
    }

    public async Task<(bool success, string message, SubjectConfigDto? data)> AddSubjectAsync(CreateSubjectConfigRequest request)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.PostAsJsonAsync("api/examiner/subjects", request);
        var r = await response.Content.ReadFromJsonAsync<ApiResponse<SubjectConfigDto>>();
        return (r?.Success ?? false, r?.Message ?? "Error", r?.Data);
    }

    public async Task<(bool success, string message)> UpdateSubjectAsync(int configId, UpdateSubjectConfigRequest request)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.PutAsJsonAsync($"api/examiner/subjects/{configId}", request);
        var r = await response.Content.ReadFromJsonAsync<ApiResponse<SubjectConfigDto>>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool success, string message)> DeleteSubjectAsync(int configId)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.DeleteAsync($"api/examiner/subjects/{configId}");
        var r = await response.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<List<TeacherDto>> GetTeachersAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<TeacherDto>>>("api/examiner/teachers");
        return r?.Data ?? new();
    }

    public async Task<(bool success, string message)> CreateTeacherAsync(CreateTeacherRequest request)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.PostAsJsonAsync("api/auth/create-teacher", request);
        var r = await response.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool success, string message, ResultSummaryDto? data)> GenerateResultAsync(int batchSemesterId)
    {
        await EnsureAuthorizationAsync();
        var response = await _http.PostAsync($"api/examiner/generate-result/{batchSemesterId}", null);
        var r = await response.Content.ReadFromJsonAsync<ApiResponse<ResultSummaryDto>>();
        return (r?.Success ?? false, r?.Message ?? "Error", r?.Data);
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<NotificationDto>>>("api/examiner/notifications");
        return r?.Data ?? new();
    }

    public async Task MarkReadAsync(int notifId)
    {
        await EnsureAuthorizationAsync();
        await _http.PatchAsync($"api/examiner/notifications/{notifId}/read", null);
    }

    public async Task<(bool success, string message, int added)> ImportStudentsAsync(
        Stream fileStream, int batchId, string fileName)
    {
        await EnsureAuthorizationAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        var response = await _http.PostAsync($"api/import/students?batchId={batchId}", content);
        try
        {
            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            if (response.IsSuccessStatusCode)
                return (true, result?.message ?? "Import successful", result?.added ?? 0);
            return (false, result?.message ?? "Import failed", 0);
        }
        catch
        {
            return (false, "Failed to process response", 0);
        }
    }
}

public class NotificationStateService
{
    public int UnreadCount { get; private set; }
    public List<NotificationDto> Notifications { get; private set; } = new();
    public event Action? OnChanged;

    public void Update(List<NotificationDto> notifs)
    {
        Notifications = notifs;
        UnreadCount = notifs.Count(n => !n.IsRead);
        OnChanged?.Invoke();
    }
}
