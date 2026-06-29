using System.Net.Http.Json;
using Blazored.LocalStorage;
using URMS.Models.DTOs;


namespace URMS.Blazor.Services;

public class HODApiService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public HODApiService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task EnsureAuthorizationAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("token");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<HODDashboardDto?> GetDashboardAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<HODDashboardDto>>("api/hod/dashboard");
        return r?.Data;
    }

    public async Task<List<BatchDto>> GetBatchesAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<BatchDto>>>("api/hod/batches");
        return r?.Data ?? new();
    }
    public async Task<BatchAnalyticsDto?> GetAnalyticsAsync(int batchSemesterId)
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<BatchAnalyticsDto>>(
            $"api/analytics/semester/{batchSemesterId}");
        return r?.Data;
    }

    public async Task<List<GradesheetReviewDto>> GetGradsheetsAsync(int batchSemesterId)
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<GradesheetReviewDto>>>(
            $"api/hod/semesters/{batchSemesterId}/gradesheets");
        return r?.Data ?? new();
    }

    public async Task<(bool ok, string msg)> ApproveAsync(int gradesheetId)
    {
        await EnsureAuthorizationAsync();
        var res = await _http.PostAsync($"api/hod/gradesheets/{gradesheetId}/approve", null);
        var r = await res.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool ok, string msg)> RejectAsync(int gradesheetId, string remarks)
    {
        await EnsureAuthorizationAsync();
        var res = await _http.PostAsJsonAsync($"api/hod/gradesheets/{gradesheetId}/reject",
            new { Remarks = remarks });
        var r = await res.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool ok, string msg)> CustomiseAsync(int gradesheetId, List<MarkUpdateDto> updatedMarks)
    {
        await EnsureAuthorizationAsync();
        var res = await _http.PostAsJsonAsync($"api/hod/gradesheets/{gradesheetId}/customise",
            new { UpdatedMarks = updatedMarks });
        var r = await res.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool ok, List<List<string>> rows)> PreviewGradesheetAsync(int gradesheetId)
    {
        await EnsureAuthorizationAsync();
        try
        {
            var res = await _http.GetFromJsonAsync<PreviewResponse>(
                $"api/hod/gradesheets/{gradesheetId}/preview");
            return (true, res?.Rows ?? new());
        }
        catch { return (false, new()); }
    }

    public async Task<(bool ok, string msg)> CustomiseWithGridAsync(int gradesheetId, List<List<string>> editedRows, int regNoCol, int totalCol)
    {
        await EnsureAuthorizationAsync();
        var res = await _http.PostAsJsonAsync($"api/hod/gradesheets/{gradesheetId}/customise-grid",
            new { EditedRows = editedRows, RegNoCol = regNoCol, TotalCol = totalCol });
        var r = await res.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<List<NotificationDto>> GetNotificationsAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<NotificationDto>>>("api/hod/notifications");
        return r?.Data ?? new();
    }

    public async Task MarkReadAsync(int notifId)
    {
        await EnsureAuthorizationAsync();
        await _http.PatchAsync($"api/hod/notifications/{notifId}/read", null);
    }

    public async Task<(bool ok, string msg)> CreateTeacherAsync(CreateTeacherRequest req)
    {
        await EnsureAuthorizationAsync();
        var res = await _http.PostAsJsonAsync("api/auth/create-teacher", req);
        var r = await res.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }
}

public class MarkUpdateDto
{
    public int StudentId { get; set; }
    public decimal MarksObtained { get; set; }
}

public class PreviewResponse
{
    public bool Success { get; set; }
    public List<List<string>> Rows { get; set; } = new();
}

public class TeacherApiService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;

    public TeacherApiService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    private async Task EnsureAuthorizationAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("token");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<TeacherDashboardDto?> GetDashboardAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<TeacherDashboardDto>>("api/teacher/dashboard");
        return r?.Data;
    }

    public async Task<List<AssignedSubjectDto>> GetSubjectsAsync()
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<AssignedSubjectDto>>>("api/teacher/my-subjects");
        return r?.Data ?? new();
    }

    public async Task<(bool ok, string msg)> UploadGradesheetAsync(
        int subjectConfigId, byte[] fileBytes, string fileName)
    {
        await EnsureAuthorizationAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileBytes), "file", fileName);
        var res = await _http.PostAsync($"api/teacher/upload-gradesheet/{subjectConfigId}", content);
        var r = await res.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }

    public async Task<(bool ok, string msg)> ReuploadGradesheetAsync(
        int gradesheetId, byte[] fileBytes, string fileName)
    {
        await EnsureAuthorizationAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileBytes), "file", fileName);
        var res = await _http.PostAsync($"api/teacher/reupload-gradesheet/{gradesheetId}", content);
        var r = await res.Content.ReadFromJsonAsync<ApiResponse>();
        return (r?.Success ?? false, r?.Message ?? "Error");
    }
    public async Task<List<NotificationDto>> GetNotificationsAsync()










    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<List<NotificationDto>>>("api/teacher/notifications");
        return r?.Data ?? new();
    }

    public async Task MarkReadAsync(int notifId)
    {
        await EnsureAuthorizationAsync();
        await _http.PatchAsync($"api/teacher/notifications/{notifId}/read", null);
    }

    public async Task<BatchAnalyticsDto?> GetAnalyticsAsync(int batchSemesterId)
    {
        await EnsureAuthorizationAsync();
        var r = await _http.GetFromJsonAsync<ApiResponse<BatchAnalyticsDto>>(
            $"api/analytics/semester/{batchSemesterId}");
        return r?.Data;
    }
}

