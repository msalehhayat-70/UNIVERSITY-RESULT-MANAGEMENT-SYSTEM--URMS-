namespace URMS.Models.DTOs;

// ─── Auth DTOs ────────────────────────────────────────────────────────────────

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int UserId { get; set; }
}

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CreateTeacherRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// ─── Batch DTOs ───────────────────────────────────────────────────────────────

public class BatchDto
{
    public int BatchId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public string ProgramCode { get; set; } = string.Empty;
    public string SemesterType { get; set; } = string.Empty;
    public int Year { get; set; }
    public int BatchNumber { get; set; }
    public List<BatchSemesterDto> Semesters { get; set; } = new();
}

public class BatchSemesterDto
{
    public int BatchSemesterId { get; set; }
    public int SemesterNo { get; set; }
    public bool IsConfigured { get; set; }
    public bool IsResultGenerated { get; set; }
    public int TotalSubjects { get; set; }
    public int ApprovedSubjects { get; set; }
    public int UploadedSubjects { get; set; }
    public bool CanGenerateResult { get; set; }
}

public class CreateBatchRequest
{
    public int ProgramId { get; set; }
    public string SemesterType { get; set; } = string.Empty;
    public int Year { get; set; }
    public int BatchNumber { get; set; }
}

// ─── Subject Configuration DTOs ───────────────────────────────────────────────

public class SubjectConfigDto
{
    public int ConfigId { get; set; }
    public int BatchSemesterId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public int MaxMarks { get; set; }
    public bool IsLocked { get; set; }
    public string GradesheetStatus { get; set; } = "Not Uploaded";
    public DateTime CreatedAt { get; set; }
}

public class CreateSubjectConfigRequest
{
    public int BatchSemesterId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string SubjectType { get; set; } = "Theory";   // Theory | Laboratory
    public int TeacherId { get; set; }
    public int MaxMarks { get; set; } = 100;
}

public class UpdateSubjectConfigRequest
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public int MaxMarks { get; set; }
}

// ─── Result Generation DTOs ───────────────────────────────────────────────────

public class GenerateResultRequest
{
    public int BatchSemesterId { get; set; }
}

public class ResultSummaryDto
{
    public int BatchSemesterId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;    // ← ADDED
    public string Section { get; set; } = string.Empty;    // ← ADDED
    public int SemesterNo { get; set; }
    public int TotalStudents { get; set; }
    public int PassedStudents { get; set; }
    public int FailedStudents { get; set; }
    public decimal ClassAverageSGPA { get; set; }
    public string AISummary { get; set; } = string.Empty;
    public List<StudentResultDto> StudentResults { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class StudentResultDto
{
    public int StudentId { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<SubjectMarkDto> SubjectMarks { get; set; } = new();
    public decimal SGPA { get; set; }
    public decimal CGPA { get; set; }
    public bool IsPass { get; set; }
    public int TotalCreditHours { get; set; }
    public string FatherName { get; set; } = string.Empty; // ← FIXED nullable warning
}

public class SubjectMarkDto
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public decimal MarksObtained { get; set; }
    public int MaxMarks { get; set; }
    public string Grade { get; set; } = string.Empty;
    public decimal GradePoints { get; set; }
    public decimal WeightedPoints { get; set; }   // GradePoints × CreditHours
}

// ─── Teacher DTOs ─────────────────────────────────────────────────────────────

public class TeacherDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AssignedSubjects { get; set; }
}

// ─── Notification DTOs ────────────────────────────────────────────────────────

public class NotificationDto
{
    public int NotifId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string NotifType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ─── Dashboard DTOs ───────────────────────────────────────────────────────────

public class ExaminerDashboardDto
{
    public int TotalBatches { get; set; }
    public int TotalTeachers { get; set; }
    public int PendingApprovals { get; set; }
    public int SemestersReadyForResult { get; set; }
    public int UnreadNotifications { get; set; }
    public List<BatchDto> Batches { get; set; } = new();
}

public class BatchAnalyticsDto
{
    public string BatchName { get; set; } = "";
    public int SemesterNo { get; set; }
    public int TotalStudents { get; set; }
    public int PassedStudents { get; set; }
    public int FailedStudents { get; set; }
    public double ClassAvgSGPA { get; set; }
    public int AtRiskCount { get; set; }
    public string AISummary { get; set; } = "";
    public List<SubjectStatDto> SubjectStats { get; set; } = new();
    public List<TeacherStatDto> TeacherStats { get; set; } = new();
    public List<AtRiskStudentDto> AtRiskStudents { get; set; } = new();
    public List<string> Anomalies { get; set; } = new();
    public List<TopStudentDto> TopStudents { get; set; } = new();
}

public class SubjectStatDto
{
    public string SubjectName { get; set; } = "";
    public string SubjectType { get; set; } = "";
    public int CreditHours { get; set; }
    public decimal AverageMarks { get; set; }
    public int FailCount { get; set; }
    public double PassRate { get; set; }
}

public class TeacherStatDto
{
    public string TeacherName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public int CreditHours { get; set; }
    public double PassRate { get; set; }
}

public class AtRiskStudentDto
{
    public string RegNo { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal SGPA { get; set; }
    public decimal CGPA { get; set; }
    public string RiskReason { get; set; } = "";
}

public class TopStudentDto
{
    public string RegNo { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal SGPA { get; set; }
    public decimal CGPA { get; set; }
    public int Rank { get; set; }
}

// ─── Generic Response ─────────────────────────────────────────────────────────

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResponse Ok(string message = "Success") =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(string message) =>
        new() { Success = false, Message = message };
}