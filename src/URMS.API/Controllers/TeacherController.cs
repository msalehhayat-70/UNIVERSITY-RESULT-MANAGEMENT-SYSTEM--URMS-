using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using URMS.Models.DTOs;
using URMS.Services.Interfaces;

namespace URMS.API.Controllers;

[ApiController]
[Route("api/teacher")]
[Authorize(Roles = "Teacher")]
public class TeacherController : ControllerBase
{
    private readonly ITeacherService _teacher;
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public TeacherController(ITeacherService teacher) => _teacher = teacher;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard() =>
        Ok(await _teacher.GetDashboardAsync(UserId));

    /// <summary>Get all subjects assigned to this teacher (set by Examiner)</summary>
    [HttpGet("my-subjects")]
    public async Task<IActionResult> GetMySubjects() =>
        Ok(await _teacher.GetAssignedSubjectsAsync(UserId));

    /// <summary>
    /// Upload grade sheet Excel for an assigned subject.
    /// System locks the subject config after upload.
    /// </summary>
    [HttpPost("upload-gradesheet/{subjectConfigId:int}")]
    public async Task<IActionResult> UploadGradesheet(
        int subjectConfigId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("No file uploaded."));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var result = await _teacher.UploadGradesheetAsync(subjectConfigId, UserId, bytes, file.FileName);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Re-upload after HOD rejection</summary>
    [HttpPost("reupload-gradesheet/{gradesheetId:int}")]
    public async Task<IActionResult> ReuploadGradesheet(
        int gradesheetId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("No file uploaded."));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var result = await _teacher.ReuploadGradesheetAsync(gradesheetId, UserId, bytes, file.FileName);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications() =>
        Ok(await _teacher.GetNotificationsAsync(UserId));

    [HttpPatch("notifications/{notifId:int}/read")]
    public async Task<IActionResult> MarkRead(int notifId) =>
        Ok(await _teacher.MarkNotificationReadAsync(notifId));
}
