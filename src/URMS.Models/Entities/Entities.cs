namespace URMS.Models.Entities;

// ─── Enums ────────────────────────────────────────────────────────────────────

public enum UserRole
{
    Examiner,
    HOD,
    Teacher
}

public enum SubjectType
{
    Theory,
    Laboratory
}

public enum GradesheetStatus
{
    Pending,
    Approved,
    Rejected,
    Customised
}

public enum NotificationType
{
    GradesheetApproved,
    GradesheetRejected,
    GradesheetUploaded,
    SubjectAssigned,
    ResultGenerated
}

// ─── SystemConfig ─────────────────────────────────────────────────────────────

public class SystemConfig
{
    public int Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = "0";
}

// ─── User ─────────────────────────────────────────────────────────────────────

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<SubjectConfiguration> AssignedSubjects { get; set; } = new List<SubjectConfiguration>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

// ─── Program ──────────────────────────────────────────────────────────────────

public class AcademicProgram
{
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;  // "BS Computer Science"
    public string ProgramCode { get; set; } = string.Empty;  // "CS"

    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}

// ─── Batch ────────────────────────────────────────────────────────────────────

public class Batch
{
    public int BatchId { get; set; }
    public string BatchName { get; set; } = string.Empty;  // "Batch9-CS-FALL-2023"
    public int ProgramId { get; set; }
    public AcademicProgram Program { get; set; } = null!;
    public string SemesterType { get; set; } = string.Empty; // "FALL" or "SPRING"
    public int Year { get; set; }
    public int BatchNumber { get; set; }  // 9, 10, 11 ...
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BatchSemester> Semesters { get; set; } = new List<BatchSemester>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
}

// ─── BatchSemester ────────────────────────────────────────────────────────────
// Represents one semester (1-8) belonging to a batch

public class BatchSemester
{
    public int BatchSemesterId { get; set; }
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public int SemesterNo { get; set; }   // 1 to 8
    public bool IsConfigured { get; set; } = false;
    public bool IsResultGenerated { get; set; } = false;
    public DateTime? ResultGeneratedAt { get; set; }

    public ICollection<SubjectConfiguration> Subjects { get; set; } = new List<SubjectConfiguration>();
}

// ─── SubjectConfiguration ─────────────────────────────────────────────────────

public class SubjectConfiguration
{
    public int ConfigId { get; set; }
    public int BatchSemesterId { get; set; }
    public BatchSemester BatchSemester { get; set; } = null!;
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public int CreditHours { get; set; }             // 1 (Lab), 2 or 3 (Theory)
    public SubjectType SubjectType { get; set; }
    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;
    public int MaxMarks { get; set; } = 100;
    public bool IsLocked { get; set; } = false;      // Locked once gradesheet uploaded
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Gradesheet? Gradesheet { get; set; }
}

// ─── Student ──────────────────────────────────────────────────────────────────

public class Student
{
    public int StudentId { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public int BatchId { get; set; }
    public Batch Batch { get; set; } = null!;
    public int CurrentSemester { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<StudentMark> Marks { get; set; } = new List<StudentMark>();
    public ICollection<SGPAResult> SGPAResults { get; set; } = new List<SGPAResult>();
    public ICollection<CGPAResult> CGPAResults { get; set; } = new List<CGPAResult>();
}

// ─── Gradesheet ───────────────────────────────────────────────────────────────

public class Gradesheet
{
    public int GradesheetId { get; set; }
    public int SubjectConfigId { get; set; }
    public SubjectConfiguration SubjectConfig { get; set; } = null!;
    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;
    public string FilePath { get; set; } = string.Empty;
    public GradesheetStatus Status { get; set; } = GradesheetStatus.Pending;
    public string? HodRemarks { get; set; }
    public int? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    public ICollection<StudentMark> StudentMarks { get; set; } = new List<StudentMark>();
    public string? CustomisedData { get; set; }
}

// ─── StudentMark ──────────────────────────────────────────────────────────────

public class StudentMark
{
    public int MarkId { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int GradesheetId { get; set; }
    public Gradesheet Gradesheet { get; set; } = null!;
    public int SubjectConfigId { get; set; }
    public SubjectConfiguration SubjectConfig { get; set; } = null!;
    public decimal MarksObtained { get; set; }
    public string Grade { get; set; } = string.Empty;        // A, B+, B ...
    public decimal GradePoints { get; set; }                 // 4.0, 3.5, 3.0 ...
}

// ─── SGPAResult ───────────────────────────────────────────────────────────────

public class SGPAResult
{
    public int SGPAId { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SemesterNo { get; set; }
    public decimal SGPA { get; set; }
    public int TotalCreditHours { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

// ─── CGPAResult ───────────────────────────────────────────────────────────────

public class CGPAResult
{
    public int CGPAId { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int UpToSemester { get; set; }
    public decimal CGPA { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

// ─── Notification ─────────────────────────────────────────────────────────────

public class Notification
{
    public int NotifId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public NotificationType NotifType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ─── AIAlert ─────────────────────────────────────────────────────────────────

public class AIAlert
{
    public int AlertId { get; set; }
    public int GradesheetId { get; set; }
    public Gradesheet Gradesheet { get; set; } = null!;
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";   // Warning | Critical
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
