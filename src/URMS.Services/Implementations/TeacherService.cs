using Microsoft.EntityFrameworkCore;
using URMS.Data;
using URMS.Models.DTOs;
using URMS.Models.Entities;
using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

public class TeacherService : ITeacherService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notif;
    private readonly IGpaCalculationService _gpa;
    private readonly ExcelImportService _excel;

    public TeacherService(AppDbContext db, INotificationService notif,
        IGpaCalculationService gpa, ExcelImportService excel)
    { _db=db; _notif=notif; _gpa=gpa; _excel=excel; }

    public async Task<ApiResponse<TeacherDashboardDto>> GetDashboardAsync(int teacherId)
    {
        var subjects = (await GetAssignedSubjectsAsync(teacherId)).Data ?? new();
        var unread   = await _db.Notifications.CountAsync(n => n.UserId == teacherId && !n.IsRead);
        var user     = await _db.Users.FindAsync(teacherId);
        return ApiResponse<TeacherDashboardDto>.Ok(new TeacherDashboardDto
        {
            TeacherName=user?.FullName??"",TotalAssigned=subjects.Count,
            Uploaded=subjects.Count(s=>s.GradesheetStatus!="Not Uploaded"),
            Approved=subjects.Count(s=>s.GradesheetStatus=="Approved"),
            Rejected=subjects.Count(s=>s.GradesheetStatus=="Rejected"),
            Pending=subjects.Count(s=>s.GradesheetStatus=="Pending"),
            UnreadNotifications=unread,AssignedSubjects=subjects
        });
    }

    public async Task<ApiResponse<List<AssignedSubjectDto>>> GetAssignedSubjectsAsync(int teacherId)
    {
        var subs = await _db.SubjectConfigurations
            .Include(sc=>sc.BatchSemester).ThenInclude(bs=>bs.Batch)
            .Include(sc=>sc.Gradesheet)
            .Where(sc=>sc.TeacherId==teacherId)
            .OrderBy(sc=>sc.BatchSemester.Batch.BatchName).ThenBy(sc=>sc.BatchSemester.SemesterNo)
            .ToListAsync();
        return ApiResponse<List<AssignedSubjectDto>>.Ok(subs.Select(sc=>new AssignedSubjectDto
        {
            SubjectConfigId=sc.ConfigId,GradesheetId=sc.Gradesheet?.GradesheetId,
            SubjectName=sc.SubjectName,SubjectCode=sc.SubjectCode,
            SubjectType=sc.SubjectType.ToString(),CreditHours=sc.CreditHours,
            BatchName=sc.BatchSemester.Batch.BatchName,SemesterNo=sc.BatchSemester.SemesterNo,
            GradesheetStatus=sc.Gradesheet?.Status.ToString()??"Not Uploaded",
            HodRemarks=sc.Gradesheet?.HodRemarks,UploadedAt=sc.Gradesheet?.UploadedAt
        }).ToList());
    }

    public async Task<ApiResponse> UploadGradesheetAsync(int subjectConfigId,int teacherId,byte[] fileBytes,string fileName)
    {
        var config = await _db.SubjectConfigurations
            .Include(sc=>sc.Gradesheet).Include(sc=>sc.BatchSemester).ThenInclude(bs=>bs.Batch)
            .FirstOrDefaultAsync(sc=>sc.ConfigId==subjectConfigId);
        if(config is null) return ApiResponse.Fail("Subject not found.");
        if(config.TeacherId!=teacherId) return ApiResponse.Fail("Not assigned to you.");
        if(config.Gradesheet?.Status==GradesheetStatus.Approved) return ApiResponse.Fail("Already approved.");

        var dir=Path.Combine("uploads","gradesheets");
        Directory.CreateDirectory(dir);
        var filePath=Path.Combine(dir,$"{subjectConfigId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{fileName}");
        await File.WriteAllBytesAsync(filePath,fileBytes);

        var batchStudents=await _db.Students.Where(s=>s.BatchId==config.BatchSemester.BatchId&&s.IsActive).ToListAsync();
        var parseResult=_excel.ParseGradesheet(fileBytes,config,batchStudents);
        if(!parseResult.IsSuccess) return ApiResponse.Fail("Excel errors: "+string.Join("; ",parseResult.Errors));

        Gradesheet gs;
        if(config.Gradesheet is null)
        {
            gs=new Gradesheet{SubjectConfigId=subjectConfigId,TeacherId=teacherId,FilePath=filePath,Status=GradesheetStatus.Pending};
            _db.Gradesheets.Add(gs); await _db.SaveChangesAsync();
        }
        else
        {
            gs=config.Gradesheet; gs.FilePath=filePath; gs.Status=GradesheetStatus.Pending;
            gs.HodRemarks=null; gs.UploadedAt=DateTime.UtcNow; gs.ReviewedAt=null;
            var old=await _db.StudentMarks.Where(m=>m.GradesheetId==gs.GradesheetId).ToListAsync();
            _db.StudentMarks.RemoveRange(old); await _db.SaveChangesAsync();
        }

        foreach(var mark in parseResult.Marks){mark.GradesheetId=gs.GradesheetId;_db.StudentMarks.Add(mark);}
        config.IsLocked=true; await _db.SaveChangesAsync();

        if(!string.IsNullOrEmpty(parseResult.AnomalyAlert))
        {
            _db.AIAlerts.Add(new AIAlert{GradesheetId=gs.GradesheetId,AlertType="AnomalyDetected",Message=parseResult.AnomalyAlert,Severity="Warning"});
            await _db.SaveChangesAsync();
        }

        var hods=await _db.Users.Where(u=>u.Role==UserRole.HOD&&u.IsActive).ToListAsync();
        var label=$"{config.BatchSemester.Batch.BatchName} Semester {config.BatchSemester.SemesterNo}";
        foreach(var hod in hods)
            await _notif.SendAsync(hod.UserId,"New Gradesheet Uploaded",
                $"Teacher uploaded gradesheet for '{config.SubjectName}' ({label}). {parseResult.Marks.Count} marks recorded.",
                NotificationType.GradesheetUploaded);

        return ApiResponse.Ok($"Uploaded. {parseResult.Marks.Count} marks. {parseResult.Stats?.FailCount??0} failures.");
    }

    public async Task<ApiResponse> ReuploadGradesheetAsync(int gradesheetId,int teacherId,byte[] fileBytes,string fileName)
    {
        var gs=await _db.Gradesheets.Include(g=>g.SubjectConfig).FirstOrDefaultAsync(g=>g.GradesheetId==gradesheetId);
        if(gs is null) return ApiResponse.Fail("Not found.");
        if(gs.TeacherId!=teacherId) return ApiResponse.Fail("Access denied.");
        if(gs.Status!=GradesheetStatus.Rejected) return ApiResponse.Fail("Only rejected gradesheets can be re-uploaded.");
        return await UploadGradesheetAsync(gs.SubjectConfigId,teacherId,fileBytes,fileName);
    }

    public async Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(int userId)
    {
        var items=await _db.Notifications.Where(n=>n.UserId==userId).OrderByDescending(n=>n.CreatedAt).Take(50)
            .Select(n=>new NotificationDto{NotifId=n.NotifId,Title=n.Title,Message=n.Message,IsRead=n.IsRead,NotifType=n.NotifType.ToString(),CreatedAt=n.CreatedAt}).ToListAsync();
        return ApiResponse<List<NotificationDto>>.Ok(items);
    }

    public async Task<ApiResponse> MarkNotificationReadAsync(int notifId)
    {
        var n=await _db.Notifications.FindAsync(notifId);
        if(n is null) return ApiResponse.Fail("Not found.");
        n.IsRead=true; await _db.SaveChangesAsync(); return ApiResponse.Ok();
    }
}
