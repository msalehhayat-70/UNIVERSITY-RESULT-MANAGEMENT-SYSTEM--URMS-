using URMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using URMS.Data;
using URMS.Models.Entities;
using URMS.Models.DTOs;

namespace URMS.Services.Implementations;

public class AnalyticsService
{
    private readonly AppDbContext _db;
    private readonly IGpaCalculationService _gpa;

    public AnalyticsService(AppDbContext db, IGpaCalculationService gpa)
    {
        _db = db;
        _gpa = gpa;
    }

    public async Task<BatchAnalyticsResult> GetAnalyticsAsync(int batchSemesterId)
    {
        var semester = await _db.BatchSemesters
            .Include(bs => bs.Batch)
            .Include(bs => bs.Subjects)
                .ThenInclude(sc => sc.Gradesheet)
                    .ThenInclude(g => g!.StudentMarks)
                        .ThenInclude(sm => sm.Student)
            .Include(bs => bs.Subjects)
                .ThenInclude(sc => sc.Teacher)
            .AsSplitQuery()
            .FirstOrDefaultAsync(bs => bs.BatchSemesterId == batchSemesterId);

        if (semester is null) return new BatchAnalyticsResult { Error = "Semester not found." };

        var result = new BatchAnalyticsResult
        {
            BatchName = semester.Batch.BatchName,
            SemesterNo = semester.SemesterNo,
        };

        // ── Subject stats ─────────────────────────────────────────────────────
        foreach (var subj in semester.Subjects)
        {
            if (subj.Gradesheet?.StudentMarks is null || !subj.Gradesheet.StudentMarks.Any())
                continue;

            var marks = subj.Gradesheet.StudentMarks.ToList();
            var avg = marks.Average(m => m.MarksObtained);
            var top = marks.Max(m => m.MarksObtained);
            var bottom = marks.Min(m => m.MarksObtained);
            var fails = marks.Count(m => m.Grade == "F");

            result.SubjectStats.Add(new SubjectStat
            {
                SubjectName = subj.SubjectName,
                SubjectCode = subj.SubjectCode,
                SubjectType = subj.SubjectType.ToString(),
                CreditHours = subj.CreditHours,
                TeacherName = subj.Teacher.FullName,
                AverageMarks = Math.Round(avg / (decimal)subj.MaxMarks * 100, 2),
                HighestMarks = top,
                LowestMarks = bottom,
                FailCount = fails,
                PassRate = Math.Round((double)(marks.Count - fails) / marks.Count * 100, 1),
            });

            if (top - avg > 35)
                result.Anomalies.Add(
                    $"{subj.SubjectName}: gap between average ({avg:F1}) and highest ({top}) is " +
                    $"{(top - avg):F1} marks — please verify.");

            var dupes = marks.GroupBy(m => m.MarksObtained).Where(g => g.Count() >= 5).ToList();
            if (dupes.Any())
                result.Anomalies.Add(
                    $"{subj.SubjectName}: {dupes.First().Count()} students have identical marks " +
                    $"({dupes.First().Key}) — possible data entry issue.");

            double failPct = (double)fails / marks.Count * 100;
            if (failPct > 40)
                result.Anomalies.Add(
                    $"{subj.SubjectName}: high failure rate ({failPct:F0}%) — academic support may be needed.");
        }

        // ── Student SGPA/CGPA summary ─────────────────────────────────────────
        var students = await _db.Students
            .Where(s => s.BatchId == semester.BatchId && s.IsActive)
            .Include(s => s.SGPAResults)
            .Include(s => s.CGPAResults)
            .AsSplitQuery()
            .ToListAsync();

        var sgpaThisSem = await _db.SGPAResults
            .Where(r => r.SemesterNo == semester.SemesterNo &&
                        students.Select(s => s.StudentId).Contains(r.StudentId))
            .ToListAsync();

        if (sgpaThisSem.Any())
        {
            result.TotalStudents = sgpaThisSem.Count;
            result.ClassAvgSGPA = Math.Round((double)sgpaThisSem.Average(r => r.SGPA), 2);
            result.PassedStudents = sgpaThisSem.Count(r => r.SGPA >= 1.0m);
            result.FailedStudents = sgpaThisSem.Count(r => r.SGPA < 1.0m);
        }

        // ── At-risk detection ─────────────────────────────────────────────────
        foreach (var student in students)
        {
            var cgpaRecord = student.CGPAResults
                .OrderByDescending(r => r.UpToSemester)
                .FirstOrDefault();

            if (cgpaRecord is null) continue;

            var cgpa = cgpaRecord.CGPA;

            if (cgpa < 2.0m)
            {
                result.AtRiskStudents.Add(new AtRiskStudent
                {
                    StudentId = student.StudentId,
                    Name = student.FullName,
                    RegNo = student.RegistrationNo,
                    CGPA = cgpa,
                    RiskReason = $"CGPA {cgpa:F2} is below 2.0 — Warning status"
                });
                continue;
            }

            var sgpaHistory = student.SGPAResults
                .OrderBy(r => r.SemesterNo)
                .TakeLast(3)
                .Select(r => r.SGPA)
                .ToList();

            if (sgpaHistory.Count >= 2)
            {
                bool declining = true;
                for (int i = 1; i < sgpaHistory.Count; i++)
                    if (sgpaHistory[i] >= sgpaHistory[i - 1]) { declining = false; break; }

                if (declining)
                    result.AtRiskStudents.Add(new AtRiskStudent
                    {
                        StudentId = student.StudentId,
                        Name = student.FullName,
                        RegNo = student.RegistrationNo,
                        CGPA = cgpa,
                        RiskReason = $"Declining SGPA for {sgpaHistory.Count} consecutive semesters"
                    });
            }
        }

        result.AtRiskCount = result.AtRiskStudents.Count;

        // ── Teacher performance ───────────────────────────────────────────────
        foreach (var stat in result.SubjectStats)
            result.TeacherStats.Add(new TeacherStat
            {
                TeacherName = stat.TeacherName,
                SubjectName = stat.SubjectName,
                CreditHours = stat.CreditHours,
                PassRate = stat.PassRate
            });

        // ── Top students by SGPA (with ties, sequential positions) ────────────
        var rankedSgpa = sgpaThisSem
            .OrderByDescending(r => r.SGPA)
            .ToList();

        int position = 0;
        decimal? prevSgpa = null;

        foreach (var r in rankedSgpa)
        {
            if (r.SGPA != prevSgpa)
                position++;

            if (position > 3) break;

            var stu = students.FirstOrDefault(s => s.StudentId == r.StudentId);
            if (stu == null) continue;

            result.TopStudents.Add(new TopStudentDto
            {
                Rank = position,
                RegNo = stu.RegistrationNo,
                Name = stu.FullName,
                SGPA = r.SGPA,
                CGPA = 0
            });

            prevSgpa = r.SGPA;
        }

        // ── AI summary ────────────────────────────────────────────────────────
        result.AISummary = BuildAISummary(result);

        return result;
    }

    private static string BuildAISummary(BatchAnalyticsResult r)
    {
        if (r.TotalStudents == 0)
            return "Result data is not yet available for this semester.";

        var bestSubject = r.SubjectStats.OrderByDescending(s => s.AverageMarks).FirstOrDefault();
        var worstSubject = r.SubjectStats.OrderByDescending(s => s.FailCount).FirstOrDefault();
        var bestTeacher = r.TeacherStats.OrderByDescending(t => t.PassRate).FirstOrDefault();
        var passRate = r.TotalStudents > 0
            ? Math.Round((double)r.PassedStudents / r.TotalStudents * 100, 1) : 0;

        var summary = $"In Semester {r.SemesterNo}, {r.BatchName}, " +
                      $"{r.PassedStudents} of {r.TotalStudents} students passed ({passRate}% pass rate). " +
                      $"Class average SGPA is {r.ClassAvgSGPA:F2}. ";

        if (bestSubject is not null)
            summary += $"The strongest subject is {bestSubject.SubjectName} " +
                       $"with a class average of {bestSubject.AverageMarks:F1}%. ";

        if (worstSubject?.FailCount > 0)
            summary += $"The most challenging subject is {worstSubject.SubjectName} " +
                       $"with {worstSubject.FailCount} student failures. ";

        if (bestTeacher is not null)
            summary += $"Best teacher performance: {bestTeacher.TeacherName} " +
                       $"({bestTeacher.SubjectName}) with {bestTeacher.PassRate:F0}% pass rate. ";

        if (r.AtRiskCount > 0)
            summary += $"{r.AtRiskCount} student(s) are flagged as at-risk and may need academic intervention.";

        return summary.Trim();
    }
}

// ── Analytics result models ───────────────────────────────────────────────────

public class BatchAnalyticsResult
{
    public string Error { get; set; } = "";
    public string BatchName { get; set; } = "";
    public int SemesterNo { get; set; }
    public int TotalStudents { get; set; }
    public int PassedStudents { get; set; }
    public int FailedStudents { get; set; }
    public double ClassAvgSGPA { get; set; }
    public int AtRiskCount { get; set; }
    public string AISummary { get; set; } = "";
    public List<SubjectStat> SubjectStats { get; set; } = new();
    public List<TeacherStat> TeacherStats { get; set; } = new();
    public List<AtRiskStudent> AtRiskStudents { get; set; } = new();
    public List<string> Anomalies { get; set; } = new();
    public List<TopStudentDto> TopStudents { get; set; } = new();
}

public class SubjectStat
{
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string SubjectType { get; set; } = "";
    public int CreditHours { get; set; }
    public string TeacherName { get; set; } = "";
    public decimal AverageMarks { get; set; }
    public decimal HighestMarks { get; set; }
    public decimal LowestMarks { get; set; }
    public int FailCount { get; set; }
    public double PassRate { get; set; }
}

public class TeacherStat
{
    public string TeacherName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public int CreditHours { get; set; }
    public double PassRate { get; set; }
}

public class AtRiskStudent
{
    public int StudentId { get; set; }
    public string Name { get; set; } = "";
    public string RegNo { get; set; } = "";
    public decimal CGPA { get; set; }
    public string RiskReason { get; set; } = "";
}