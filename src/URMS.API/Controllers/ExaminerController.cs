using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Security.Claims;
using URMS.Models.DTOs;
using URMS.Services.Implementations;
using URMS.Services.Interfaces;

namespace URMS.API.Controllers;

[ApiController]
[Route("api/examiner")]
[Authorize(Roles = "Examiner")]
public class ExaminerController : ControllerBase
{
    private readonly IExaminerService _examiner;
    private readonly PdfService _pdf;
    private readonly ResultGenerationService _resultGeneration;
    private readonly URMS.Data.AppDbContext _db;
    private readonly URMS.Services.Implementations.GradeExtractionService _gradeExtraction;
    private int UserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    public ExaminerController(IExaminerService examiner, PdfService pdf, ResultGenerationService resultGeneration, URMS.Data.AppDbContext db, URMS.Services.Implementations.GradeExtractionService gradeExtraction)
    {
        _examiner = examiner;
        _pdf = pdf;
        _resultGeneration = resultGeneration;
        _db = db;
        _gradeExtraction = gradeExtraction;
    }

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches() => Ok(await _examiner.GetAllBatchesAsync());

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard() => Ok(await _examiner.GetDashboardAsync(UserId));

    [HttpGet("batches/{batchId:int}")]
    public async Task<IActionResult> GetBatch(int batchId)
    { var r = await _examiner.GetBatchAsync(batchId); return r.Success ? Ok(r) : NotFound(r); }

    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest request)
    { var r = await _examiner.CreateBatchAsync(request); return r.Success ? Ok(r) : BadRequest(r); }

    [HttpGet("semesters/{batchSemesterId:int}/subjects")]
    public async Task<IActionResult> GetSubjects(int batchSemesterId) =>
        Ok(await _examiner.GetSubjectsForSemesterAsync(batchSemesterId));

    [HttpPost("subjects")]
    public async Task<IActionResult> AddSubject([FromBody] CreateSubjectConfigRequest request)
    { var r = await _examiner.AddSubjectAsync(request); return r.Success ? Ok(r) : BadRequest(r); }

    [HttpPut("subjects/{configId:int}")]
    public async Task<IActionResult> UpdateSubject(int configId, [FromBody] UpdateSubjectConfigRequest request)
    { var r = await _examiner.UpdateSubjectAsync(configId, request); return r.Success ? Ok(r) : BadRequest(r); }

    [HttpDelete("subjects/{configId:int}")]
    public async Task<IActionResult> DeleteSubject(int configId)
    { var r = await _examiner.DeleteSubjectAsync(configId); return r.Success ? Ok(r) : BadRequest(r); }

    [HttpGet("test-extract/{gradesheetId:int}")]
    public async Task<IActionResult> TestExtract(int gradesheetId)
    {
        var gs = await _db.Gradesheets.FindAsync(gradesheetId);
        if (gs == null) return NotFound("Gradesheet not found");
        if (string.IsNullOrEmpty(gs.CustomisedData)) return BadRequest("No CustomisedData");
        var extracted = _gradeExtraction.ExtractGradesFromJson(gs.CustomisedData);
        return Ok(new { total = extracted.Count, grades = extracted.Take(5) });
    }

    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers() => Ok(await _examiner.GetAllTeachersAsync());

    [HttpPost("generate-result/{batchSemesterId:int}")]
    public async Task<IActionResult> GenerateResult(int batchSemesterId)
    {
        var (success, message) = await _resultGeneration.GenerateResultAsync(batchSemesterId);
        return success ? Ok(new { success = true, message }) : BadRequest(new { success = false, message });
    }

    [HttpGet("result/{batchSemesterId:int}/class-pdf")]
    public async Task<IActionResult> DownloadClassPdf(int batchSemesterId)
    {
        var summary = await _resultGeneration.GetResultSummaryAsync(batchSemesterId);
        if (summary == null || !summary.StudentResults.Any())
            return BadRequest(new { error = "No results found" });

        var cd = BuildClassData(summary);
        var (pdfBytes, fileName) = _pdf.GenerateClassResultSheet(cd, 50); // ← UPDATED
        return File(pdfBytes, "application/pdf", fileName);                // ← UPDATED
    }

    [HttpGet("result/{batchSemesterId:int}/student/{studentId:int}/pdf")]
    public async Task<IActionResult> DownloadStudentCard(int batchSemesterId, int studentId)
    {
        var sr = await _examiner.GetResultSummaryAsync(batchSemesterId);
        if (!sr.Success || sr.Data is null) return BadRequest(sr);
        var st = sr.Data.StudentResults.FirstOrDefault(s => s.StudentId == studentId);
        if (st is null) return NotFound(ApiResponse.Fail("Student not found."));
        var bytes = _pdf.GenerateStudentResultCard(BuildCardData(sr.Data, st));
        return File(bytes, "application/pdf", $"ResultCard_{st.RegistrationNo}.pdf");
    }

    [HttpGet("result/{batchSemesterId:int}/all-cards-zip")]
    public async Task<IActionResult> DownloadAllCardsZip(int batchSemesterId)
    {
        var sr = await _examiner.GetResultSummaryAsync(batchSemesterId);
        if (!sr.Success || sr.Data is null) return BadRequest(sr);
        var summary = sr.Data;
        using var zipMs = new MemoryStream();
        using (var arch = new ZipArchive(zipMs, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var st in summary.StudentResults)
            {
                var bytes = _pdf.GenerateStudentResultCard(BuildCardData(summary, st));
                var entry = arch.CreateEntry($"ResultCard_{st.RegistrationNo}_{st.FullName}.pdf", CompressionLevel.Fastest);
                using var es = entry.Open();
                await es.WriteAsync(bytes);
            }
        }
        zipMs.Seek(0, SeekOrigin.Begin);
        return File(zipMs.ToArray(), "application/zip", $"ResultCards_{summary.BatchName}_Sem{summary.SemesterNo}.zip");
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications() => Ok(await _examiner.GetNotificationsAsync(UserId));

    [HttpPatch("notifications/{notifId:int}/read")]
    public async Task<IActionResult> MarkRead(int notifId) => Ok(await _examiner.MarkNotificationReadAsync(notifId));

    // ── helpers ───────────────────────────────────────────────────────────────

    private static StudentResultCardData BuildCardData(ResultSummaryDto sum, StudentResultDto st)
    {
        // ── Rank by SGPA (current semester position) ──────────────────────────
        var sgpaRanked = sum.StudentResults
            .OrderByDescending(s => s.SGPA).ToList();
        int currentPos = GetRank(sgpaRanked.Select(s => (double)s.SGPA).ToList(),
            sgpaRanked.FindIndex(s => s.StudentId == st.StudentId));

        // ── Rank by CGPA (overall position) ───────────────────────────────────
        var cgpaRanked = sum.StudentResults
            .OrderByDescending(s => s.CGPA).ToList();
        int overallPos = GetRank(cgpaRanked.Select(s => (double)s.CGPA).ToList(),
            cgpaRanked.FindIndex(s => s.StudentId == st.StudentId));

        return new StudentResultCardData
        {
            StudentName = st.FullName,
            RegNo = st.RegistrationNo,
            ClassName = sum.BatchName,
            SemesterLabel = Ord(sum.SemesterNo),
            ResultTitle = $"Result – {Season()} – {DateTime.UtcNow.Year}",
            SGPA_2dp = st.SGPA.ToString("F2"),
            CGPA_2dp = st.CGPA.ToString("F2"),
            AcademicStatus = Status((double)st.CGPA),
            CurrentPos = currentPos,
            OverallPos = overallPos,
            TotalStudents = sum.TotalStudents,
            Subjects = st.SubjectMarks.Select(sm => new SubjectLine
            {
                Code = sm.SubjectCode,
                Name = sm.SubjectName,
                CreditHours = sm.CreditHours,
                Grade = sm.Grade
            }).ToList()
        };
    }

    // ── Returns 1-based rank with ties sharing same position ──────────────────
    private static int GetRank(List<double> sortedValues, int index)
    {
        if (index < 0) return 0;
        double myValue = sortedValues[index];
        // Rank = number of distinct students ranked above me + 1
        int rank = sortedValues.Take(index).Count(v => v > myValue) + 1;
        return rank;
    }

    private static ClassResultData BuildClassData(ResultSummaryDto sum)
    {
        var first = sum.StudentResults.FirstOrDefault();

        var subjs = first?.SubjectMarks
            .Select(sm => new SubjectHeader
            {
                Code = sm.SubjectCode,
                Name = sm.SubjectName,
                Type = sm.SubjectType,
                CreditHours = sm.CreditHours
            })
            .ToList() ?? new();

        return new ClassResultData
        {
            University = "Dr. A. Q. Khan Institute of Computer Sciences & Information Technology",
            Program = sum.Program,                // ← e.g. "CS"
            BatchName = sum.BatchName,
            Section = sum.Section,                // ← e.g. "9" (BatchNumber)
            SemesterNo = sum.SemesterNo,
            SemesterLabel = $"{Season()} {DateTime.UtcNow.Year}",
            ExaminerName = "Faheem Ahmed",
            ExaminerTitle = "Deputy Controller of Examinations KICSIT",
            JEName = "Muhammad Latif",
            JETitle = "JE (Exams) KICSIT",
            Subjects = subjs,
            Students = sum.StudentResults
                .OrderBy(s => s.RegistrationNo)
                .Select(s => new StudentRow
                {
                    RegNo = s.RegistrationNo,
                    FatherName = s.FatherName ?? "",
                    Name = s.FullName,
                    SGPA_9dp = (double)s.SGPA,
                    CGPA_9dp = (double)s.CGPA,
                    SGPA_2dp = (double)Math.Round(s.SGPA, 2),
                    CGPA_2dp = (double)Math.Round(s.CGPA, 2),
                    AcademicStatus = Status((double)s.CGPA),
                    SubjectMarks = s.SubjectMarks
                        .Select(sm => new MarkEntry { Grade = sm.Grade, GradePoints = sm.GradePoints })
                        .ToList()
                })
                .ToList()
        };
    }

    private static string Ord(int n) => n switch { 1 => "1st Semester", 2 => "2nd Semester", 3 => "3rd Semester", _ => $"{n}th Semester" };
    private static string Season() => DateTime.UtcNow.Month is >= 2 and <= 6 ? "Spring" : "Fall";
    private static string Status(double c) => c switch { >= 3.5 => "Excellent", >= 3.0 => "Very Good", >= 2.5 => "Good", >= 2.0 => "Satisfactory", >= 1.5 => "Fair", >= 1.0 => "Warning", _ => "Extended Temporary Enrollment" };
}