using URMS.Models.DTOs;

namespace URMS.Services.Interfaces;

public interface IHODService
{
    Task<ApiResponse<HODDashboardDto>>             GetDashboardAsync(int hodId);
    Task<ApiResponse<List<BatchDto>>>              GetAllBatchesAsync();
    Task<ApiResponse<List<GradesheetReviewDto>>>   GetGradsheetsForSemesterAsync(int batchSemesterId);
    Task<ApiResponse>                              ApproveGradesheetAsync(int gradesheetId, int hodId);
    Task<ApiResponse>                              RejectGradesheetAsync(int gradesheetId, int hodId, string remarks);
    Task<ApiResponse>                              CustomiseGradesheetAsync(int gradesheetId, int hodId, object request);
    Task<ApiResponse>                              CustomiseWithGridAsync(int gradesheetId, int hodId, List<List<string>> editedRows, int regNoCol, int totalCol);
    Task<ApiResponse<List<NotificationDto>>>       GetNotificationsAsync(int userId);
    Task<ApiResponse> MarkNotificationReadAsync(int notifId);
    Task<URMS.Models.Entities.Gradesheet?> GetGradesheetFileAsync(int gradesheetId);
    Task<ApiResponse<GradesheetReviewDto>> GetGradesheetDetailAsync(int gradesheetId);
    // In IHODService.cs — add these two method signatures:
    Task<int> GetBatchSemesterIdForGradesheetAsync(int gradesheetId);
    Task<bool> AreAllGradsheetsApprovedAsync(int batchSemesterId);
}

public interface ITeacherService
{
    Task<ApiResponse<TeacherDashboardDto>>         GetDashboardAsync(int teacherId);
    Task<ApiResponse<List<AssignedSubjectDto>>>    GetAssignedSubjectsAsync(int teacherId);
    Task<ApiResponse>                              UploadGradesheetAsync(int subjectConfigId, int teacherId, byte[] fileBytes, string fileName);
    Task<ApiResponse>                              ReuploadGradesheetAsync(int gradesheetId, int teacherId, byte[] fileBytes, string fileName);
    Task<ApiResponse<List<NotificationDto>>>       GetNotificationsAsync(int userId);
    Task<ApiResponse>                              MarkNotificationReadAsync(int notifId);
}

// In HODService.cs — add these two implementations:
