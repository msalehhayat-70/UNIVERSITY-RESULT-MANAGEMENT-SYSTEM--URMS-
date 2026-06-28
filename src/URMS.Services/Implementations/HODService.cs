using Microsoft.EntityFrameworkCore;
using URMS.Data;
using URMS.Models.DTOs;
using URMS.Models.Entities;
using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

public class HODService : IHODService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notif;
    private readonly IGpaCalculationService _gpa;

    public HODService(AppDbContext db, INotificationService notif, IGpaCalculationService gpa)
    { _db = db; _notif = notif; _gpa = gpa; }

    public async Task<ApiResponse<HODDashboardDto>> GetDashboardAsync(int hodId)
    {
        var batches = (await GetAllBatchesAsync()).Data ?? new();
        var pending = await _db.Gradesheets.CountAsync(g => g.Status == GradesheetStatus.Pending);
        var approved = await _db.Gradesheets.CountAsync(g => g.Status == GradesheetStatus.Approved && g.ReviewedAt.HasValue && g.ReviewedAt.Value.Date == DateTime.UtcNow.Date);
        var rejected = await _db.Gradesheets.CountAsync(g => g.Status == GradesheetStatus.Rejected);
        var unread = await _db.Notifications.CountAsync(n => n.UserId == hodId && !n.IsRead);
        return ApiResponse<HODDashboardDto>.Ok(new HODDashboardDto
        {
            TotalBatches = batches.Count,
            PendingReview = pending,
            ApprovedToday = approved,
            RejectedTotal = rejected,
            UnreadNotifications = unread,
            Batches = batches
        });
    }

    public async Task<ApiResponse<List<BatchDto>>> GetAllBatchesAsync()
    {
        var batches = await _db.Batches
            .Include(b => b.Program)
            .Include(b => b.Semesters)
                .ThenInclude(s => s.Subjects)
                    .ThenInclude(sc => sc.Gradesheet)
            .Where(b => b.IsActive)
            .AsSplitQuery()
            .OrderByDescending(b => b.Year).ThenBy(b => b.BatchNumber)
            .ToListAsync();

        var dtos = batches.Select(batch => new BatchDto
        {
            BatchId = batch.BatchId,
            BatchName = batch.BatchName,
            ProgramName = batch.Program.ProgramName,
            ProgramCode = batch.Program.ProgramCode,
            SemesterType = batch.SemesterType,
            Year = batch.Year,
            BatchNumber = batch.BatchNumber,
            Semesters = batch.Semesters.OrderBy(s => s.SemesterNo).Select(s =>
            {
                var total = s.Subjects.Count;
                var uploaded = s.Subjects.Count(sc => sc.Gradesheet != null);
                var approved = s.Subjects.Count(sc => sc.Gradesheet?.Status == GradesheetStatus.Approved);
                return new BatchSemesterDto
                {
                    BatchSemesterId = s.BatchSemesterId,
                    SemesterNo = s.SemesterNo,
                    IsConfigured = s.IsConfigured,
                    IsResultGenerated = s.IsResultGenerated,
                    TotalSubjects = total,
                    UploadedSubjects = uploaded,
                    ApprovedSubjects = approved,
                    CanGenerateResult = total > 0 && approved == total && !s.IsResultGenerated
                };
            }).ToList()
        }).ToList();

        return ApiResponse<List<BatchDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<List<GradesheetReviewDto>>> GetGradsheetsForSemesterAsync(int batchSemesterId)
    {
        var subjects = await _db.SubjectConfigurations
            .Include(sc => sc.Teacher)
            .Include(sc => sc.Gradesheet)
                .ThenInclude(g => g!.StudentMarks)
                    .ThenInclude(sm => sm.Student)
            .Where(sc => sc.BatchSemesterId == batchSemesterId)
            .AsSplitQuery()
            .ToListAsync();

        var dtos = subjects.Select(sc => new GradesheetReviewDto
        {
            SubjectConfigId = sc.ConfigId,
            SubjectName = sc.SubjectName,
            SubjectCode = sc.SubjectCode,
            SubjectType = sc.SubjectType.ToString(),
            CreditHours = sc.CreditHours,
            TeacherName = sc.Teacher.FullName,
            TeacherEmail = sc.Teacher.Email,
            GradesheetId = sc.Gradesheet?.GradesheetId ?? 0,
            Status = sc.Gradesheet?.Status.ToString() ?? "Not Uploaded",
            HodRemarks = sc.Gradesheet?.HodRemarks,
            UploadedAt = sc.Gradesheet?.UploadedAt ?? DateTime.MinValue,
            ReviewedAt = sc.Gradesheet?.ReviewedAt,
            StudentMarks = sc.Gradesheet?.StudentMarks.Select(sm => new StudentMarkReviewDto
            {
                StudentId = sm.StudentId,
                StudentName = sm.Student.FullName,
                RegistrationNo = sm.Student.RegistrationNo,
                MarksObtained = sm.MarksObtained,
                Grade = sm.Grade,
                GradePoints = sm.GradePoints
            }).ToList() ?? new()
        }).ToList();

        return ApiResponse<List<GradesheetReviewDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<GradesheetReviewDto>> GetGradesheetDetailAsync(int gradesheetId)
    {
        // Guard: gradesheetId 0 means not uploaded yet
        if (gradesheetId <= 0)
            return ApiResponse<GradesheetReviewDto>.Fail("Gradesheet not uploaded yet.");

        var gs = await _db.Gradesheets
            .Include(g => g.SubjectConfig)
                .ThenInclude(sc => sc.Teacher)
            .Include(g => g.StudentMarks)
                .ThenInclude(sm => sm.Student)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.GradesheetId == gradesheetId);

        if (gs is null)
            return ApiResponse<GradesheetReviewDto>.Fail("Gradesheet not found.");

        var dto = new GradesheetReviewDto
        {
            GradesheetId = gs.GradesheetId,
            SubjectConfigId = gs.SubjectConfigId,
            SubjectName = gs.SubjectConfig.SubjectName,
            SubjectCode = gs.SubjectConfig.SubjectCode,
            SubjectType = gs.SubjectConfig.SubjectType.ToString(),
            CreditHours = gs.SubjectConfig.CreditHours,
            TeacherName = gs.SubjectConfig.Teacher.FullName,
            TeacherEmail = gs.SubjectConfig.Teacher.Email,
            Status = gs.Status.ToString(),
            HodRemarks = gs.HodRemarks,
            UploadedAt = gs.UploadedAt,
            ReviewedAt = gs.ReviewedAt,
            StudentMarks = gs.StudentMarks
                .OrderBy(sm => sm.Student.RegistrationNo)
                .Select(sm => new StudentMarkReviewDto
                {
                    StudentId = sm.StudentId,
                    StudentName = sm.Student.FullName,
                    RegistrationNo = sm.Student.RegistrationNo,
                    MarksObtained = sm.MarksObtained,
                    Grade = sm.Grade,
                    GradePoints = sm.GradePoints
                }).ToList()
        };

        return ApiResponse<GradesheetReviewDto>.Ok(dto);
    }

    public async Task<ApiResponse> ApproveGradesheetAsync(int gradesheetId, int hodId)
    {
        var gs = await _db.Gradesheets
            .Include(g => g.SubjectConfig)
                .ThenInclude(sc => sc.BatchSemester)
                    .ThenInclude(bs => bs.Batch)
            .FirstOrDefaultAsync(g => g.GradesheetId == gradesheetId);

        if (gs is null) return ApiResponse.Fail("Gradesheet not found.");
        if (gs.Status == GradesheetStatus.Approved) return ApiResponse.Fail("Already approved.");

        gs.Status = GradesheetStatus.Approved;
        gs.ReviewedAt = DateTime.UtcNow;
        gs.ReviewedById = hodId;
        await _db.SaveChangesAsync();

        await _notif.SendAsync(gs.TeacherId, "Gradesheet Approved",
            $"Your gradesheet for '{gs.SubjectConfig.SubjectName}' has been approved by HOD.",
            NotificationType.GradesheetApproved);

        var sem = gs.SubjectConfig.BatchSemester;
        var allApproved = await _db.SubjectConfigurations
            .Include(sc => sc.Gradesheet)
            .Where(sc => sc.BatchSemesterId == sem.BatchSemesterId)
            .AllAsync(sc => sc.Gradesheet != null && sc.Gradesheet.Status == GradesheetStatus.Approved);

        if (allApproved)
        {
            var examiners = await _db.Users
                .Where(u => u.Role == UserRole.Examiner && u.IsActive)
                .ToListAsync();
            foreach (var ex in examiners)
                await _notif.SendToExaminerOnAllApproved(ex.UserId,
                    $"{sem.Batch.BatchName} — Semester {sem.SemesterNo}");
        }

        return ApiResponse.Ok("Gradesheet approved.");
    }

    public async Task<ApiResponse> RejectGradesheetAsync(int gradesheetId, int hodId, string remarks)
    {
        var gs = await _db.Gradesheets
            .Include(g => g.SubjectConfig)
                .ThenInclude(sc => sc.BatchSemester)
                    .ThenInclude(bs => bs.Batch)
            .FirstOrDefaultAsync(g => g.GradesheetId == gradesheetId);

        if (gs is null) return ApiResponse.Fail("Not found.");
        if (string.IsNullOrWhiteSpace(remarks)) return ApiResponse.Fail("Remarks required.");

        gs.Status = GradesheetStatus.Rejected;
        gs.HodRemarks = remarks;
        gs.ReviewedAt = DateTime.UtcNow;
        gs.ReviewedById = hodId;
        await _db.SaveChangesAsync();

        await _notif.SendAsync(gs.TeacherId, "Gradesheet Rejected",
            $"Your gradesheet for '{gs.SubjectConfig.SubjectName}' was rejected. Remarks: {remarks}",
            NotificationType.GradesheetRejected);

        return ApiResponse.Ok("Rejected. Teacher notified.");
    }

    public async Task<ApiResponse> CustomiseGradesheetAsync(int gradesheetId, int hodId, object requestObj)
    {
        var gs = await _db.Gradesheets
            .Include(g => g.StudentMarks)
            .Include(g => g.SubjectConfig)
            .FirstOrDefaultAsync(g => g.GradesheetId == gradesheetId);

        if (gs is null) return ApiResponse.Fail("Not found.");

        dynamic? req = requestObj;
        try
        {
            if (req?.UpdatedMarks != null)
            {
                foreach (var update in req.UpdatedMarks)
                {
                    int studentId = (int)update.StudentId;
                    decimal marks = (decimal)update.MarksObtained;
                    var mark = gs.StudentMarks.FirstOrDefault(m => m.StudentId == studentId);
                    if (mark is null) continue;
                    var pct = gs.SubjectConfig.MaxMarks > 0
                        ? (marks / gs.SubjectConfig.MaxMarks) * 100m : marks;
                    mark.MarksObtained = marks;
                    mark.Grade = CalcGrade(pct);
                    mark.GradePoints = CalcGradePoints(pct);
                }
            }
        }
        catch { /* approve as-is if cast fails */ }

        gs.Status = GradesheetStatus.Approved;
        gs.ReviewedAt = DateTime.UtcNow;
        gs.ReviewedById = hodId;
        await _db.SaveChangesAsync();

        await _notif.SendAsync(gs.TeacherId, "Gradesheet Customised & Approved",
            $"Your gradesheet for '{gs.SubjectConfig.SubjectName}' was reviewed and customised by HOD.",
            NotificationType.GradesheetApproved);

        return ApiResponse.Ok("Customised and forwarded to Examiner.");
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(int userId)
    {
        var items = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                NotifId = n.NotifId,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                NotifType = n.NotifType.ToString(),
                CreatedAt = n.CreatedAt
            }).ToListAsync();

        return ApiResponse<List<NotificationDto>>.Ok(items);
    }

    public async Task<ApiResponse> MarkNotificationReadAsync(int notifId)
    {
        var n = await _db.Notifications.FindAsync(notifId);
        if (n is null) return ApiResponse.Fail("Not found.");
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return ApiResponse.Ok();
    }

    public async Task<ApiResponse> CustomiseWithGridAsync(int gradesheetId, int hodId, List<List<string>> editedRows, int regNoCol, int totalCol)
    {
        var gs = await _db.Gradesheets.Include(g=>g.SubjectConfig)
            .FirstOrDefaultAsync(g=>g.GradesheetId==gradesheetId);
        if(gs is null) return ApiResponse.Fail("Not found.");

        // Save full edited grid as JSON (include header row from original)
        gs.CustomisedData = System.Text.Json.JsonSerializer.Serialize(editedRows);
        gs.Status         = GradesheetStatus.Approved;
        gs.ReviewedAt     = DateTime.UtcNow;
        gs.ReviewedById   = hodId;
        await _db.SaveChangesAsync();

        await _notif.SendAsync(gs.TeacherId, "Gradesheet Customised & Approved",
            $"Your gradesheet for '{gs.SubjectConfig.SubjectName}' was reviewed and customised by HOD.",
            NotificationType.GradesheetApproved);
        return ApiResponse.Ok("Customised and approved.");
    }

    public async Task<Gradesheet?> GetGradesheetFileAsync(int gradesheetId)
        => await _db.Gradesheets.FirstOrDefaultAsync(g => g.GradesheetId == gradesheetId);

    static string CalcGrade(decimal pct) => pct switch
    {
        >= 85 => "A",
        >= 80 => "A-",
        >= 75 => "B+",
        >= 71 => "B",
        >= 68 => "B-",
        >= 64 => "C+",
        >= 61 => "C",
        >= 58 => "C-",
        >= 54 => "D+",
        >= 50 => "D",
        _ => "F"
    };

    static decimal CalcGradePoints(decimal pct) => pct switch
    {
        >= 85 => 4.00m,
        >= 80 => 3.67m,
        >= 75 => 3.33m,
        >= 71 => 3.00m,
        >= 68 => 2.67m,
        >= 64 => 2.33m,
        >= 61 => 2.00m,
        >= 58 => 1.67m,
        >= 54 => 1.33m,
        >= 50 => 1.00m,
        _ => 0.00m
    };

    public async Task<int> GetBatchSemesterIdForGradesheetAsync(int gradesheetId)
    {
        var gs = await _db.Gradesheets
            .Include(g => g.SubjectConfig)
            .FirstOrDefaultAsync(g => g.GradesheetId == gradesheetId);
        return gs?.SubjectConfig?.BatchSemesterId ?? 0;
    }

    public async Task<bool> AreAllGradsheetsApprovedAsync(int batchSemesterId)
    {
        var total = await _db.Gradesheets
            .Include(g => g.SubjectConfig)
            .CountAsync(g => g.SubjectConfig.BatchSemesterId == batchSemesterId);

        var approved = await _db.Gradesheets
            .Include(g => g.SubjectConfig)
            .CountAsync(g => g.SubjectConfig.BatchSemesterId == batchSemesterId &&
                             g.Status == GradesheetStatus.Approved);

        return total > 0 && total == approved;
    }
}