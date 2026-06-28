using Microsoft.EntityFrameworkCore;
using URMS.Data;
using URMS.Models.DTOs;
using URMS.Models.Entities;
using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

public class ExaminerService : IExaminerService
{
    private readonly AppDbContext _db;
    private readonly IGpaCalculationService _gpa;
    private readonly INotificationService _notif;

    public ExaminerService(AppDbContext db, IGpaCalculationService gpa, INotificationService notif)
    {
        _db    = db;
        _gpa   = gpa;
        _notif = notif;
    }

    // ─── Dashboard ────────────────────────────────────────────────────────────

    public async Task<ApiResponse<ExaminerDashboardDto>> GetDashboardAsync(int examinerId)
    {
        var batches      = await GetAllBatchesAsync();
        var totalTeachers = await _db.Users.CountAsync(u => u.Role == UserRole.Teacher && u.IsActive);
        var unreadNotifs  = await _db.Notifications.CountAsync(n => n.UserId == examinerId && !n.IsRead);

        int pendingApprovals = 0;
        int readyForResult   = 0;

        foreach (var batch in batches.Data ?? new())
        {
            foreach (var sem in batch.Semesters)
            {
                pendingApprovals += sem.UploadedSubjects - sem.ApprovedSubjects;
                if (sem.CanGenerateResult) readyForResult++;
            }
        }

        return ApiResponse<ExaminerDashboardDto>.Ok(new ExaminerDashboardDto
        {
            TotalBatches             = batches.Data?.Count ?? 0,
            TotalTeachers            = totalTeachers,
            PendingApprovals         = pendingApprovals,
            SemestersReadyForResult  = readyForResult,
            UnreadNotifications      = unreadNotifs,
            Batches                  = batches.Data ?? new()
        });
    }

    // ─── Batch Management ─────────────────────────────────────────────────────

    public async Task<ApiResponse<List<BatchDto>>> GetAllBatchesAsync()
    {
        var batches = await _db.Batches
            .Include(b => b.Program)
    .Include(b => b.Semesters)
        .ThenInclude(s => s.Subjects)
            .ThenInclude(sc => sc.Gradesheet)
    .AsSplitQuery()
    .Where(b => b.IsActive)
            .OrderByDescending(b => b.Year)
            .ThenBy(b => b.BatchNumber)
            .ToListAsync();

        var dtos = batches.Select(MapBatchToDto).ToList();
        return ApiResponse<List<BatchDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<BatchDto>> GetBatchAsync(int batchId)
    {
        var batch = await _db.Batches
            .Include(b => b.Program)
            .Include(b => b.Semesters)
                .ThenInclude(s => s.Subjects)
                    .ThenInclude(sc => sc.Gradesheet)
            .FirstOrDefaultAsync(b => b.BatchId == batchId);

        if (batch is null)
            return ApiResponse<BatchDto>.Fail("Batch not found.");

        return ApiResponse<BatchDto>.Ok(MapBatchToDto(batch));
    }

    public async Task<ApiResponse<BatchDto>> CreateBatchAsync(CreateBatchRequest request)
    {
        var program = await _db.Programs.FindAsync(request.ProgramId);
        if (program is null)
            return ApiResponse<BatchDto>.Fail("Program not found.");

        var batchName = $"Batch{request.BatchNumber}-{program.ProgramCode}-{request.SemesterType}-{request.Year}";

        if (await _db.Batches.AnyAsync(b => b.BatchName == batchName))
            return ApiResponse<BatchDto>.Fail($"Batch '{batchName}' already exists.");

        var batch = new Batch
        {
            ProgramId    = request.ProgramId,
            BatchName    = batchName,
            SemesterType = request.SemesterType,
            Year         = request.Year,
            BatchNumber  = request.BatchNumber
        };

        _db.Batches.Add(batch);
        await _db.SaveChangesAsync();

        // Auto-create 8 semester slots for the batch
        for (int i = 1; i <= 8; i++)
        {
            _db.BatchSemesters.Add(new BatchSemester
            {
                BatchId    = batch.BatchId,
                SemesterNo = i
            });
        }
        await _db.SaveChangesAsync();

        return await GetBatchAsync(batch.BatchId);
    }

    // ─── Subject Configuration ────────────────────────────────────────────────

    public async Task<ApiResponse<List<SubjectConfigDto>>> GetSubjectsForSemesterAsync(int batchSemesterId)
    {
        var subjects = await _db.SubjectConfigurations
            .Include(sc => sc.Teacher)
            .Include(sc => sc.Gradesheet)
            .Where(sc => sc.BatchSemesterId == batchSemesterId)
            .OrderBy(sc => sc.SubjectType)
            .ThenBy(sc => sc.SubjectName)
            .ToListAsync();

        var dtos = subjects.Select(MapSubjectToDto).ToList();
        return ApiResponse<List<SubjectConfigDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<SubjectConfigDto>> AddSubjectAsync(CreateSubjectConfigRequest request)
    {
        var semester = await _db.BatchSemesters
            .Include(bs => bs.Batch)
            .FirstOrDefaultAsync(bs => bs.BatchSemesterId == request.BatchSemesterId);

        if (semester is null)
            return ApiResponse<SubjectConfigDto>.Fail("Semester not found.");

        var teacher = await _db.Users.FindAsync(request.TeacherId);
        if (teacher is null || teacher.Role != UserRole.Teacher)
            return ApiResponse<SubjectConfigDto>.Fail("Invalid teacher selected.");

        if (!new[] { "Theory", "Laboratory" }.Contains(request.SubjectType))
            return ApiResponse<SubjectConfigDto>.Fail("Subject type must be 'Theory' or 'Laboratory'.");

        if (request.CreditHours < 1 || request.CreditHours > 4)
            return ApiResponse<SubjectConfigDto>.Fail("Credit hours must be between 1 and 4.");

        // Duplicate subject code check within the same semester
        if (await _db.SubjectConfigurations.AnyAsync(sc =>
            sc.BatchSemesterId == request.BatchSemesterId &&
            sc.SubjectCode == request.SubjectCode))
        {
            return ApiResponse<SubjectConfigDto>.Fail($"Subject code '{request.SubjectCode}' already exists in this semester.");
        }

        var subjectType = request.SubjectType == "Theory"
            ? SubjectType.Theory
            : SubjectType.Laboratory;

        var subject = new SubjectConfiguration
        {
            BatchSemesterId = request.BatchSemesterId,
            SubjectName     = request.SubjectName,
            SubjectCode     = request.SubjectCode,
            CreditHours     = request.CreditHours,
            SubjectType     = subjectType,
            TeacherId       = request.TeacherId,
            MaxMarks        = request.MaxMarks
        };

        _db.SubjectConfigurations.Add(subject);
        await _db.SaveChangesAsync();

        // Notify teacher
        var semLabel = $"{semester.Batch.BatchName} — Semester {semester.SemesterNo}";
        await _notif.SendToTeacherOnSubjectAssigned(request.TeacherId, request.SubjectName, semLabel);

        var loaded = await _db.SubjectConfigurations
            .Include(sc => sc.Teacher)
            .Include(sc => sc.Gradesheet)
            .FirstAsync(sc => sc.ConfigId == subject.ConfigId);

        return ApiResponse<SubjectConfigDto>.Ok(MapSubjectToDto(loaded), "Subject added successfully.");
    }

    public async Task<ApiResponse<SubjectConfigDto>> UpdateSubjectAsync(int configId, UpdateSubjectConfigRequest request)
    {
        var subject = await _db.SubjectConfigurations
            .Include(sc => sc.Teacher)
            .Include(sc => sc.Gradesheet)
            .FirstOrDefaultAsync(sc => sc.ConfigId == configId);

        if (subject is null)
            return ApiResponse<SubjectConfigDto>.Fail("Subject not found.");

        if (subject.IsLocked)
            return ApiResponse<SubjectConfigDto>.Fail("Subject is locked. A grade sheet has already been uploaded for it.");

        var teacher = await _db.Users.FindAsync(request.TeacherId);
        if (teacher is null || teacher.Role != UserRole.Teacher)
            return ApiResponse<SubjectConfigDto>.Fail("Invalid teacher.");

        subject.SubjectName = request.SubjectName;
        subject.SubjectCode = request.SubjectCode;
        subject.CreditHours = request.CreditHours;
        subject.SubjectType = request.SubjectType == "Theory" ? SubjectType.Theory : SubjectType.Laboratory;
        subject.TeacherId   = request.TeacherId;
        subject.MaxMarks    = request.MaxMarks;

        await _db.SaveChangesAsync();

        var loaded = await _db.SubjectConfigurations
            .Include(sc => sc.Teacher)
            .Include(sc => sc.Gradesheet)
            .FirstAsync(sc => sc.ConfigId == configId);

        return ApiResponse<SubjectConfigDto>.Ok(MapSubjectToDto(loaded), "Subject updated.");
    }

    public async Task<ApiResponse> DeleteSubjectAsync(int configId)
    {
        var subject = await _db.SubjectConfigurations.FindAsync(configId);
        if (subject is null)
            return ApiResponse.Fail("Subject not found.");

        if (subject.IsLocked)
            return ApiResponse.Fail("Cannot delete: a grade sheet has already been uploaded for this subject.");

        _db.SubjectConfigurations.Remove(subject);
        await _db.SaveChangesAsync();
        return ApiResponse.Ok("Subject deleted.");
    }

    // ─── Teachers ─────────────────────────────────────────────────────────────

    public async Task<ApiResponse<List<TeacherDto>>> GetAllTeachersAsync()
    {
        var teachers = await _db.Users
            .Where(u => u.Role == UserRole.Teacher && u.IsActive)
            .Select(u => new TeacherDto
            {
                UserId           = u.UserId,
                FullName         = u.FullName,
                Email            = u.Email,
                AssignedSubjects = u.AssignedSubjects.Count
            })
            .OrderBy(t => t.FullName)
            .ToListAsync();

        return ApiResponse<List<TeacherDto>>.Ok(teachers);
    }

    // ─── Result Generation ────────────────────────────────────────────────────

    public async Task<ApiResponse<ResultSummaryDto>> GenerateResultAsync(int batchSemesterId)
    {
        var semester = await _db.BatchSemesters
              .Include(bs => bs.Batch).ThenInclude(b => b.Program)
              .Include(bs => bs.Subjects).ThenInclude(sc => sc.Gradesheet).ThenInclude(g => g!.StudentMarks).ThenInclude(sm => sm.Student)
              .Include(bs => bs.Subjects).ThenInclude(sc => sc.Teacher)
              .AsSplitQuery()
              .FirstOrDefaultAsync(bs => bs.BatchSemesterId == batchSemesterId);

        if (semester is null)
            return ApiResponse<ResultSummaryDto>.Fail("Semester not found.");

        if (semester.IsResultGenerated)
            return ApiResponse<ResultSummaryDto>.Fail("Result has already been generated for this semester.");

        // Validate all subjects are approved
        var unapproved = semester.Subjects
            .Where(s => s.Gradesheet is null || s.Gradesheet.Status != GradesheetStatus.Approved)
            .Select(s => s.SubjectName)
            .ToList();

        if (unapproved.Any())
            return ApiResponse<ResultSummaryDto>.Fail(
                $"Cannot generate result. The following subjects are not yet approved: {string.Join(", ", unapproved)}");

        // Gather all students in this batch
        var students = await _db.Students
            .Where(s => s.BatchId == semester.BatchId && s.IsActive)
            .ToListAsync();

        var studentResults = new List<StudentResultDto>();

        foreach (var student in students)
        {
            var subjectMarks = new List<SubjectMarkDto>();
            var gpaInputs    = new List<(decimal gradePoints, int creditHours)>();

            foreach (var subject in semester.Subjects)
            {
                var mark = subject.Gradesheet!.StudentMarks
                    .FirstOrDefault(m => m.StudentId == student.StudentId);

                if (mark is null) continue;

                var weighted = mark.GradePoints * subject.CreditHours;

                subjectMarks.Add(new SubjectMarkDto
                {
                    SubjectName    = subject.SubjectName,
                    SubjectCode    = subject.SubjectCode,
                    SubjectType    = subject.SubjectType.ToString(),
                    CreditHours    = subject.CreditHours,
                    MarksObtained  = mark.MarksObtained,
                    MaxMarks       = subject.MaxMarks,
                    Grade          = mark.Grade,
                    GradePoints    = mark.GradePoints,
                    WeightedPoints = weighted
                });

                gpaInputs.Add((mark.GradePoints, subject.CreditHours));
            }

            var sgpa         = _gpa.CalculateSGPA(gpaInputs);
            var totalCredits = semester.Subjects.Sum(s => s.CreditHours);

            // Store SGPA
            var existingSgpa = await _db.SGPAResults
                .FirstOrDefaultAsync(r => r.StudentId == student.StudentId && r.SemesterNo == semester.SemesterNo);

            if (existingSgpa is null)
            {
                _db.SGPAResults.Add(new SGPAResult
                {
                    StudentId        = student.StudentId,
                    SemesterNo       = semester.SemesterNo,
                    SGPA             = sgpa,
                    TotalCreditHours = totalCredits
                });
            }
            else
            {
                existingSgpa.SGPA             = sgpa;
                existingSgpa.TotalCreditHours = totalCredits;
                existingSgpa.CalculatedAt     = DateTime.UtcNow;
            }

            // Calculate CGPA
            var allSGPAs = await _db.SGPAResults
                .Where(r => r.StudentId == student.StudentId && r.SemesterNo <= semester.SemesterNo)
                .Select(r => r.SGPA)
                .ToListAsync();

            // Include the current semester SGPA if not yet saved
            if (!allSGPAs.Contains(sgpa))
                allSGPAs.Add(sgpa);

            var cgpa = _gpa.CalculateCGPA(allSGPAs);

            var existingCgpa = await _db.CGPAResults
                .FirstOrDefaultAsync(r => r.StudentId == student.StudentId && r.UpToSemester == semester.SemesterNo);

            if (existingCgpa is null)
            {
                _db.CGPAResults.Add(new CGPAResult
                {
                    StudentId     = student.StudentId,
                    UpToSemester  = semester.SemesterNo,
                    CGPA          = cgpa
                });
            }
            else
            {
                existingCgpa.CGPA          = cgpa;
                existingCgpa.CalculatedAt  = DateTime.UtcNow;
            }

            studentResults.Add(new StudentResultDto
            {
                StudentId       = student.StudentId,
                RegistrationNo  = student.RegistrationNo,
                FullName        = student.FullName,
                SubjectMarks    = subjectMarks,
                SGPA            = sgpa,
                CGPA            = cgpa,
                IsPass          = subjectMarks.All(sm => sm.Grade != "F"),
                TotalCreditHours = totalCredits
            });
        }

        semester.IsResultGenerated = true;
        semester.ResultGeneratedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Build AI summary
        var passed = studentResults.Count(r => r.IsPass);
        var failed = studentResults.Count(r => !r.IsPass);
        var avgSgpa = studentResults.Any() ? Math.Round(studentResults.Average(r => r.SGPA), 2) : 0m;

        var bestSubject = semester.Subjects
            .Select(s => new
            {
                s.SubjectName,
                Avg = s.Gradesheet!.StudentMarks.Any()
                    ? s.Gradesheet.StudentMarks.Average(m => m.MarksObtained)
                    : 0m
            })
            .OrderByDescending(x => x.Avg)
            .FirstOrDefault();

        var worstSubject = semester.Subjects
            .Select(s => new
            {
                s.SubjectName,
                Fails = s.Gradesheet!.StudentMarks.Count(m => m.Grade == "F")
            })
            .OrderByDescending(x => x.Fails)
            .FirstOrDefault();

        var aiSummary = $"In Semester {semester.SemesterNo}, {semester.Batch.BatchName}, " +
                        $"{passed} of {students.Count} students passed. " +
                        $"Class average SGPA: {avgSgpa}. " +
                        (bestSubject is not null ? $"Strongest subject: {bestSubject.SubjectName} (avg {bestSubject.Avg:F1}%). " : "") +
                        (worstSubject?.Fails > 0 ? $"Most failures: {worstSubject.SubjectName} ({worstSubject.Fails} failures)." : "No subject had failures.");

        return ApiResponse<ResultSummaryDto>.Ok(new ResultSummaryDto
        {
            BatchSemesterId = batchSemesterId,
            BatchName       = semester.Batch.BatchName,
            SemesterNo      = semester.SemesterNo,
            TotalStudents   = students.Count,
            PassedStudents  = passed,
            FailedStudents  = failed,
            ClassAverageSGPA = avgSgpa,
            AISummary       = aiSummary,
            StudentResults  = studentResults.OrderBy(r => r.RegistrationNo).ToList(),
            GeneratedAt     = DateTime.UtcNow
        });
    }

    public async Task<byte[]> GenerateClassResultPdfAsync(int batchSemesterId)
    {
        // PDF generation delegated to PdfGenerationService (iText7)
        // Returns placeholder — wire up iText7 in full implementation
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    public async Task<byte[]> GenerateStudentResultCardPdfAsync(int studentId, int batchSemesterId)
    {
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }

    // ─── Notifications ────────────────────────────────────────────────────────

    public async Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(int userId)
    {
        var notifs = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                NotifId   = n.NotifId,
                Title     = n.Title,
                Message   = n.Message,
                IsRead    = n.IsRead,
                NotifType = n.NotifType.ToString(),
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<NotificationDto>>.Ok(notifs);
    }

    public async Task<ApiResponse> MarkNotificationReadAsync(int notifId)
    {
        var notif = await _db.Notifications.FindAsync(notifId);
        if (notif is null) return ApiResponse.Fail("Notification not found.");
        notif.IsRead = true;
        await _db.SaveChangesAsync();
        return ApiResponse.Ok();
    }

    // ─── Mapping Helpers ──────────────────────────────────────────────────────

    private static BatchDto MapBatchToDto(Batch batch) => new()
    {
        BatchId      = batch.BatchId,
        BatchName    = batch.BatchName,
        ProgramName  = batch.Program.ProgramName,
        ProgramCode  = batch.Program.ProgramCode,
        SemesterType = batch.SemesterType,
        Year         = batch.Year,
        BatchNumber  = batch.BatchNumber,
        Semesters    = batch.Semesters
            .OrderBy(s => s.SemesterNo)
            .Select(s =>
            {
                var total    = s.Subjects.Count;
                var uploaded = s.Subjects.Count(sc => sc.Gradesheet is not null);
                var approved = s.Subjects.Count(sc => sc.Gradesheet?.Status == GradesheetStatus.Approved);
                return new BatchSemesterDto
                {
                    BatchSemesterId  = s.BatchSemesterId,
                    SemesterNo       = s.SemesterNo,
                    IsConfigured     = s.IsConfigured,
                    IsResultGenerated = s.IsResultGenerated,
                    TotalSubjects    = total,
                    UploadedSubjects = uploaded,
                    ApprovedSubjects = approved,
                    CanGenerateResult = total > 0 && approved == total && !s.IsResultGenerated
                };
            }).ToList()
    };

    private static SubjectConfigDto MapSubjectToDto(SubjectConfiguration sc) => new()
    {
        ConfigId        = sc.ConfigId,
        BatchSemesterId = sc.BatchSemesterId,
        SubjectName     = sc.SubjectName,
        SubjectCode     = sc.SubjectCode,
        CreditHours     = sc.CreditHours,
        SubjectType     = sc.SubjectType.ToString(),
        TeacherId       = sc.TeacherId,
        TeacherName     = sc.Teacher.FullName,
        TeacherEmail    = sc.Teacher.Email,
        MaxMarks        = sc.MaxMarks,
        IsLocked        = sc.IsLocked,
        GradesheetStatus = sc.Gradesheet is null ? "Not Uploaded" : sc.Gradesheet.Status.ToString(),
        CreatedAt       = sc.CreatedAt
    };

    // ── Get stored result summary (does not regenerate) ──────────────────────
    public async Task<ApiResponse<ResultSummaryDto>> GetResultSummaryAsync(int batchSemesterId)
    {
        var semester = await _db.BatchSemesters
            .Include(bs => bs.Batch).ThenInclude(b => b.Program)
            .Include(bs => bs.Subjects).ThenInclude(sc => sc.Gradesheet)
            .ThenInclude(g => g!.StudentMarks).ThenInclude(sm => sm.Student)
            .AsSplitQuery()
            .FirstOrDefaultAsync(bs => bs.BatchSemesterId == batchSemesterId);

        if (semester is null)
            return ApiResponse<ResultSummaryDto>.Fail("Semester not found.");
        if (!semester.IsResultGenerated)
            return ApiResponse<ResultSummaryDto>.Fail("Result not generated yet for this semester.");

        var students = await _db.Students
            .Where(s => s.BatchId == semester.BatchId && s.IsActive)
            .ToListAsync();

        var studentResults = new List<StudentResultDto>();
        foreach (var student in students)
        {
            var sgpaRec = await _db.SGPAResults
                .FirstOrDefaultAsync(r => r.StudentId == student.StudentId && r.SemesterNo == semester.SemesterNo);
            var cgpaRec = await _db.CGPAResults
                .FirstOrDefaultAsync(r => r.StudentId == student.StudentId && r.UpToSemester == semester.SemesterNo);
            if (sgpaRec is null) continue;

            var subjectMarks = new List<SubjectMarkDto>();
            foreach (var subj in semester.Subjects)
            {
                var mark = subj.Gradesheet?.StudentMarks.FirstOrDefault(m => m.StudentId == student.StudentId);
                if (mark is null) continue;
                subjectMarks.Add(new SubjectMarkDto
                {
                    SubjectName   = subj.SubjectName,
                    SubjectCode   = subj.SubjectCode,
                    SubjectType   = subj.SubjectType.ToString(),
                    CreditHours   = subj.CreditHours,
                    MarksObtained = mark.MarksObtained,
                    MaxMarks      = subj.MaxMarks,
                    Grade         = mark.Grade,
                    GradePoints   = mark.GradePoints,
                    WeightedPoints = mark.GradePoints * subj.CreditHours
                });
            }

            studentResults.Add(new StudentResultDto
            {
                StudentId        = student.StudentId,
                RegistrationNo   = student.RegistrationNo,
                FullName         = student.FullName,
                SubjectMarks     = subjectMarks,
                SGPA             = sgpaRec.SGPA,
                CGPA             = cgpaRec?.CGPA ?? sgpaRec.SGPA,
                IsPass           = subjectMarks.All(sm => sm.Grade != "F"),
                TotalCreditHours = sgpaRec.TotalCreditHours
            });
        }

        var avgSgpa = studentResults.Any() ? Math.Round((decimal)studentResults.Average(r => (double)r.SGPA), 2) : 0m;
        var passed  = studentResults.Count(r => r.IsPass);

        return ApiResponse<ResultSummaryDto>.Ok(new ResultSummaryDto
        {
            BatchSemesterId  = batchSemesterId,
            BatchName        = semester.Batch.BatchName,
            SemesterNo       = semester.SemesterNo,
            TotalStudents    = students.Count,
            PassedStudents   = passed,
            FailedStudents   = students.Count - passed,
            ClassAverageSGPA = avgSgpa,
            AISummary        = $"Semester {semester.SemesterNo}, {semester.Batch.BatchName}: {passed}/{students.Count} passed. Avg SGPA: {avgSgpa}.",
            StudentResults   = studentResults.OrderBy(r => r.RegistrationNo).ToList(),
            GeneratedAt      = semester.ResultGeneratedAt ?? DateTime.UtcNow
        });
    }

}
