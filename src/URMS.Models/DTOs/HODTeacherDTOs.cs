namespace URMS.Models.DTOs;

// ── HOD Dashboard ─────────────────────────────────────────────────────────────

public class HODDashboardDto
{
    public int TotalBatches { get; set; }
    public int PendingReview { get; set; }
    public int ApprovedToday { get; set; }
    public int RejectedTotal { get; set; }
    public int UnreadNotifications { get; set; }
    public List<BatchDto> Batches { get; set; } = new();
}

// ── Gradesheet Review ─────────────────────────────────────────────────────────

public class GradesheetReviewDto
{
    public int GradesheetId { get; set; }
    public int SubjectConfigId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? HodRemarks { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public List<StudentMarkReviewDto> StudentMarks { get; set; } = new();
}

public class StudentMarkReviewDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNo { get; set; } = string.Empty;
    public decimal MarksObtained { get; set; }
    public string Grade { get; set; } = string.Empty;
    public decimal GradePoints { get; set; }
}

// ── Teacher Dashboard ─────────────────────────────────────────────────────────

public class TeacherDashboardDto
{
    public string TeacherName { get; set; } = string.Empty;
    public int TotalAssigned { get; set; }
    public int Uploaded { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Pending { get; set; }
    public int UnreadNotifications { get; set; }
    public List<AssignedSubjectDto> AssignedSubjects { get; set; } = new();
}

public class AssignedSubjectDto
{
    public int SubjectConfigId { get; set; }
    public int? GradesheetId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public int SemesterNo { get; set; }
    public string GradesheetStatus { get; set; } = "Not Uploaded";
    public string? HodRemarks { get; set; }
    public DateTime? UploadedAt { get; set; }
}

// ── Analytics DTOs ────────────────────────────────────────────────────────────






public class StudentDto
{
    public int    StudentId       { get; set; }
    public string RegistrationNo  { get; set; } = "";
    public string FullName        { get; set; } = "";
    public string FatherName      { get; set; } = "";
    public string BatchName       { get; set; } = "";
    public int    CurrentSemester { get; set; }
    public bool   IsActive        { get; set; }
}

public class CreateStudentRequest
{
    public string RegistrationNo { get; set; } = "";
    public string FullName       { get; set; } = "";
    public string FatherName     { get; set; } = "";
    public int    BatchId        { get; set; }
}

public class ImportStudentsRequest
{
    public int BatchId { get; set; }
    public List<CreateStudentRequest> Students { get; set; } = new();
}
