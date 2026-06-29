using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Models.DTOs;
using URMS.Services.Implementations;
using URMS.Services.Interfaces;

namespace URMS.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Examiner,HOD")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analytics;
    public AnalyticsController(AnalyticsService analytics) => _analytics = analytics;

    [HttpGet("semester/{batchSemesterId:int}")]
    public async Task<IActionResult> GetSemesterAnalytics(int batchSemesterId)
    {
        var result = await _analytics.GetAnalyticsAsync(batchSemesterId);
        if (!string.IsNullOrEmpty(result.Error))
            return NotFound(ApiResponse.Fail(result.Error));

        var dto = new BatchAnalyticsDto
        {
            BatchName = result.BatchName,
            SemesterNo = result.SemesterNo,
            TotalStudents = result.TotalStudents,
            PassedStudents = result.PassedStudents,
            FailedStudents = result.FailedStudents,
            ClassAvgSGPA = result.ClassAvgSGPA,
            AtRiskCount = result.AtRiskCount,
            AISummary = result.AISummary,
            Anomalies = result.Anomalies,
            SubjectStats = result.SubjectStats.Select(s => new SubjectStatDto
            {
                SubjectName = s.SubjectName,
                SubjectType = s.SubjectType,
                CreditHours = s.CreditHours,
                AverageMarks = s.AverageMarks,
                FailCount = s.FailCount,
                PassRate = s.PassRate
            }).ToList(),
            TeacherStats = result.TeacherStats.Select(t => new TeacherStatDto
            {
                TeacherName = t.TeacherName,
                SubjectName = t.SubjectName,
                CreditHours = t.CreditHours,
                PassRate = t.PassRate
            }).ToList(),
            AtRiskStudents = result.AtRiskStudents.Select(a => new AtRiskStudentDto
            {
                RegNo = a.RegNo,
                Name = a.Name,
                CGPA = a.CGPA,
                SGPA = a.CGPA,
                RiskReason = a.RiskReason
            }).ToList(),
            TopStudents = result.TopStudents.Select(t => new TopStudentDto
            {
                Rank = t.Rank,
                RegNo = t.RegNo,
                Name = t.Name,
                SGPA = t.SGPA,
                CGPA = 0
            }).ToList(),
        };

        return Ok(ApiResponse<BatchAnalyticsDto>.Ok(dto));
    }
}