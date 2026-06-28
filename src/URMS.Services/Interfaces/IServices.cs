using URMS.Models.DTOs;

namespace URMS.Services.Interfaces;

// ─── Auth Service ─────────────────────────────────────────────────────────────

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>> RegisterFirstAccountAsync(RegisterRequest request);
    Task<ApiResponse> CreateTeacherAccountAsync(CreateTeacherRequest request, int createdByUserId);
    Task<int> GetRegisteredAccountsCountAsync();
}

// ─── Examiner Service ─────────────────────────────────────────────────────────

public interface IExaminerService
{
    // Dashboard
    Task<ApiResponse<ExaminerDashboardDto>> GetDashboardAsync(int examinerId);

    // Batch management
    Task<ApiResponse<List<BatchDto>>> GetAllBatchesAsync();
    Task<ApiResponse<BatchDto>> GetBatchAsync(int batchId);
    Task<ApiResponse<BatchDto>> CreateBatchAsync(CreateBatchRequest request);

    // Subject configuration
    Task<ApiResponse<List<SubjectConfigDto>>> GetSubjectsForSemesterAsync(int batchSemesterId);
    Task<ApiResponse<SubjectConfigDto>> AddSubjectAsync(CreateSubjectConfigRequest request);
    Task<ApiResponse<SubjectConfigDto>> UpdateSubjectAsync(int configId, UpdateSubjectConfigRequest request);
    Task<ApiResponse> DeleteSubjectAsync(int configId);

    // Teacher management
    Task<ApiResponse<List<TeacherDto>>> GetAllTeachersAsync();

    // Result generation
    Task<ApiResponse<ResultSummaryDto>> GenerateResultAsync(int batchSemesterId);
    Task<ApiResponse<ResultSummaryDto>> GetResultSummaryAsync(int batchSemesterId);
    Task<byte[]> GenerateClassResultPdfAsync(int batchSemesterId);
    Task<byte[]> GenerateStudentResultCardPdfAsync(int studentId, int batchSemesterId);

    // Notifications
    Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(int userId);
    Task<ApiResponse> MarkNotificationReadAsync(int notifId);
}

// ─── GPA Calculation Service ──────────────────────────────────────────────────

public interface IGpaCalculationService
{
    decimal GradeToPoints(string grade);
    decimal CalculateSGPA(List<(decimal gradePoints, int creditHours)> subjectResults);
    decimal CalculateCGPA(List<decimal> semesterSGPAs);
}

// ─── Notification Service ─────────────────────────────────────────────────────

public interface INotificationService
{
    Task SendAsync(int userId, string title, string message, Models.Entities.NotificationType type);
    Task SendToTeacherOnSubjectAssigned(int teacherId, string subjectName, string batchSemester);
    Task SendToExaminerOnAllApproved(int examinerId, string batchSemester);
}
