using URMS.Services.Interfaces;
using OfficeOpenXml;
using URMS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using URMS.Data;

namespace URMS.Services.Implementations;

/// <summary>
/// Parses KICSIT gradesheet Excel files uploaded by teachers.
/// Also supports bulk student import.
/// </summary>
public class ExcelImportService
{
    private readonly IGpaCalculationService _gpa;

    public ExcelImportService(IGpaCalculationService gpa)
    {
        _gpa = gpa;
    }

    // ===========================
    // 1. GRADESHEET PARSER
    // ===========================
    public ExcelParseResult ParseGradesheet(
     byte[] fileBytes,
     SubjectConfiguration subjectConfig,
     List<Student> batchStudents)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var result = new ExcelParseResult();

        using var ms = new MemoryStream(fileBytes);
        using var pkg = new ExcelPackage(ms);
        var ws = pkg.Workbook.Worksheets.FirstOrDefault();

        if (ws is null) { result.Errors.Add("No worksheet found."); return result; }

        int lastRow = ws.Dimension?.End.Row ?? 0;
        int lastCol = ws.Dimension?.End.Column ?? 0;

        Console.WriteLine($"[EXCEL] Sheet: {ws.Name}, Rows: {lastRow}, Cols: {lastCol}");

        if (lastRow < 2) { result.Errors.Add("No data rows."); return result; }

        var studentLookup = batchStudents
            .GroupBy(s => s.RegistrationNo.Trim().ToUpper())
            .ToDictionary(g => g.Key, g => g.First());

        // ── Step 1: Find header row by scanning for "Reg" ─────────────────────
        int headerRow = -1;
        int regNoCol = -1;

        for (int r = 1; r <= Math.Min(15, lastRow); r++)
        {
            for (int c = 1; c <= lastCol; c++)
            {
                var cellText = ws.Cells[r, c].Text.Trim().ToLower();
                if (cellText.Contains("reg"))
                {
                    headerRow = r;
                    regNoCol = c;
                    Console.WriteLine($"[EXCEL] Header row={r}, RegNo col={c} (value='{ws.Cells[r, c].Text}')");
                    break;
                }
            }
            if (headerRow > 0) break;
        }

        if (headerRow < 0 || regNoCol < 0)
        {
            result.Errors.Add("Could not find header row with 'Reg' column.");
            return result;
        }

        // ── Step 2: Find Grade and Total columns in header rows ───────────────
        // Check header row AND the sub-header row below it
        int gradeCol = -1;
        int totalCol = -1;

        for (int checkRow = headerRow; checkRow <= Math.Min(headerRow + 2, lastRow); checkRow++)
        {
            for (int c = 1; c <= lastCol; c++)
            {
                var h = ws.Cells[checkRow, c].Text.Trim().ToLower();

                if (gradeCol < 0 && h == "grade")
                {
                    gradeCol = c;
                    Console.WriteLine($"[EXCEL] Grade col={c} at row {checkRow}");
                }

                if (totalCol < 0 &&
                    (h.Contains("total") || h.Contains("/100") || h.Contains("100")))
                {
                    totalCol = c;
                    Console.WriteLine($"[EXCEL] Total col={c} at row {checkRow} (value='{ws.Cells[checkRow, c].Text}')");
                }
            }
            if (gradeCol > 0 && totalCol > 0) break;
        }

        // Fallback: grade = last col, total = second to last
        if (gradeCol < 0)
        {
            gradeCol = lastCol;
            Console.WriteLine($"[EXCEL] Grade col not found — using last col {gradeCol}");
        }
        if (totalCol < 0)
        {
            totalCol = lastCol - 1;
            Console.WriteLine($"[EXCEL] Total col not found — using col {totalCol}");
        }

        // ── Step 3: Log what row 1 sample looks like for debugging ────────────
        // Data starts 2 rows after header (header + sub-header row)
        int dataStartRow = headerRow + 2;
        Console.WriteLine($"[EXCEL] Data starts at row {dataStartRow}");

        // Log first data row for sanity check
        if (dataStartRow <= lastRow)
        {
            Console.WriteLine($"[EXCEL] First data row sample: " +
                $"Col{regNoCol}='{ws.Cells[dataStartRow, regNoCol].Text}' " +
                $"Col{gradeCol}='{ws.Cells[dataStartRow, gradeCol].Text}' " +
                $"Col{totalCol}='{ws.Cells[dataStartRow, totalCol].Text}'");
        }

        // ── Step 4: Parse student rows ─────────────────────────────────────────
        for (int row = dataStartRow; row <= lastRow; row++)
        {
            var regNo = ws.Cells[row, regNoCol].Text.Trim();

            // Skip empty or non-student rows
            if (string.IsNullOrWhiteSpace(regNo)) continue;
            if (!System.Text.RegularExpressions.Regex.IsMatch(regNo, @"^\d{7,}$"))
            {
                Console.WriteLine($"[EXCEL] Row {row}: skipping non-student regNo '{regNo}'");
                continue;
            }

            var gradeText = ws.Cells[row, gradeCol].Text.Trim().ToUpper();
            var marksText = ws.Cells[row, totalCol].Text.Trim();

            Console.WriteLine($"[EXCEL] Row {row}: RegNo={regNo}, Grade={gradeText}, Marks={marksText}");

            if (string.IsNullOrWhiteSpace(gradeText))
            {
                result.Warnings.Add($"Row {row}: No grade for {regNo}.");
                continue;
            }

            decimal.TryParse(marksText,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal marks);

            if (marks > subjectConfig.MaxMarks) marks = subjectConfig.MaxMarks;

            var gradePoints = _gpa.GradeToPoints(gradeText);

            if (!studentLookup.TryGetValue(regNo.ToUpper(), out var student))
            {
                result.Warnings.Add($"Row {row}: Student {regNo} not in batch.");
                continue;
            }

            result.Marks.Add(new StudentMark
            {
                StudentId = student.StudentId,
                SubjectConfigId = subjectConfig.ConfigId,
                MarksObtained = marks,
                Grade = gradeText,
                GradePoints = gradePoints
            });
        }

        Console.WriteLine($"[EXCEL] Total marks parsed: {result.Marks.Count}");

        // ── Step 5: Stats ──────────────────────────────────────────────────────
        if (result.Marks.Any())
        {
            result.Stats = new GradesheetStats
            {
                TotalStudents = result.Marks.Count,
                AverageMarks = Math.Round(result.Marks.Average(m => m.MarksObtained), 2),
                HighestMarks = result.Marks.Max(m => m.MarksObtained),
                LowestMarks = result.Marks.Min(m => m.MarksObtained),
                FailCount = result.Marks.Count(m => m.Grade == "F")
            };

            double failPct = (double)result.Stats.FailCount / result.Marks.Count * 100;
            if (failPct > 40)
                result.AnomalyAlert = $"High failure rate: {failPct:F0}% students failed.";
        }

        return result;
    }
    // ===========================
    // 2. STUDENT IMPORT
    // ===========================
    public async Task<StudentImportResult> ImportStudentsAsync(
        Stream fileStream,
        int batchId,
        AppDbContext db)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var result = new StudentImportResult();

        try
        {
            using var pkg = new ExcelPackage(fileStream);

            var ws = pkg.Workbook.Worksheets.FirstOrDefault();

            if (ws is null)
            {
                result.Errors.Add("Excel file contains no worksheets.");
                return result;
            }

            int lastRow = ws.Dimension?.End.Row ?? 0;

            if (lastRow < 2)
            {
                result.Errors.Add("No data rows found.");
                return result;
            }

            var existingRegNos = await db.Students
                .Where(s => s.BatchId == batchId)
                .Select(s => s.RegistrationNo.ToUpper())
                .ToListAsync();

            var studentsToAdd = new List<Student>();
            var processed = new HashSet<string>();

            for (int row = 2; row <= lastRow; row++)
            {
                var regNo = ws.Cells[row, 1].Text.Trim();
                var fullName = ws.Cells[row, 2].Text.Trim();
                var fatherName = ws.Cells[row, 3].Text.Trim();

                if (string.IsNullOrWhiteSpace(regNo) ||
                    string.IsNullOrWhiteSpace(fullName))
                {
                    continue;
                }

                var regUpper = regNo.ToUpper();

                // Duplicate in DB
                if (existingRegNos.Contains(regUpper))
                {
                    result.Warnings.Add($"Duplicate DB record: {regNo}");
                    continue;
                }

                // Duplicate in current file
                if (processed.Contains(regUpper))
                {
                    result.Warnings.Add($"Duplicate in file: {regNo}");
                    continue;
                }

                processed.Add(regUpper);

                studentsToAdd.Add(new Student
                {
                    RegistrationNo = regNo,
                    FullName = fullName,
                    FatherName = fatherName,
                    BatchId = batchId,
                    CurrentSemester = 1,
                    IsActive = true
                });
            }

            if (studentsToAdd.Count > 0)
            {
                db.Students.AddRange(studentsToAdd);
                await db.SaveChangesAsync();

                result.AddedCount = studentsToAdd.Count;
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }

        return result;
    }
}

// ===========================
// RESULT CLASSES
// ===========================

public class ExcelParseResult
{
    public List<StudentMark> Marks { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? AnomalyAlert { get; set; }
    public GradesheetStats? Stats { get; set; }

    public bool IsSuccess => !Errors.Any();
}

public class GradesheetStats
{
    public int TotalStudents { get; set; }
    public decimal AverageMarks { get; set; }
    public decimal HighestMarks { get; set; }
    public decimal LowestMarks { get; set; }
    public int FailCount { get; set; }
}

public class StudentImportResult
{
    public bool Success { get; set; }
    public int AddedCount { get; set; }

    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}