using URMS.Data;
using URMS.Models.Entities;
using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public async Task SendAsync(int userId, string title, string message, NotificationType type)
    {
        _db.Notifications.Add(new Notification
        {
            UserId    = userId,
            Title     = title,
            Message   = message,
            NotifType = type
        });
        await _db.SaveChangesAsync();
    }

    public async Task SendToTeacherOnSubjectAssigned(int teacherId, string subjectName, string batchSemester)
    {
        await SendAsync(
            teacherId,
            "New Subject Assigned",
            $"You have been assigned to teach '{subjectName}' for {batchSemester}. " +
            $"Please upload the grade sheet once marks are finalised.",
            NotificationType.SubjectAssigned
        );
    }

    public async Task SendToExaminerOnAllApproved(int examinerId, string batchSemester)
    {
        await SendAsync(
            examinerId,
            "Semester Ready for Result Generation",
            $"All grade sheets for {batchSemester} have been approved by the HOD. " +
            $"You can now generate results.",
            NotificationType.ResultGenerated
        );
    }
}
