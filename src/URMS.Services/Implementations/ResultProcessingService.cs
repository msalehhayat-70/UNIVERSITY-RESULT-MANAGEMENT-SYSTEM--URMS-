using Microsoft.EntityFrameworkCore;
using URMS.Data;
using URMS.Models.Entities;
using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

public class ResultProcessingService
{
    private readonly AppDbContext _db;
    private readonly GradeExtractionService _gradeExtraction;
    private readonly ExcelImportService _excelImport;
    private readonly StudentMasterImportService _studentMasterImport;
    private readonly IGpaCalculationService _gpa;

    public ResultProcessingService(
        AppDbContext db,
        GradeExtractionService gradeExtraction,
        ExcelImportService excelImport,
        StudentMasterImportService studentMasterImport,
        IGpaCalculationService gpa)
    {
        _db = db;
        _gradeExtraction = gradeExtraction;
        _excelImport = excelImport;
        _studentMasterImport = studentMasterImport;
        _gpa = gpa;
    }

    public async Task<(bool success, string message, int marksImported)>
        ImportGradesForBatchSemesterAsync(int batchSemesterId)
    {
        try
        {
            Console.WriteLine($"\n[IMPORT] ===== START batchSemesterId={batchSemesterId} =====");
            Console.WriteLine($"[IMPORT] Working directory: {Directory.GetCurrentDirectory()}");

            var batchSemester = await _db.BatchSemesters
                .Include(bs => bs.Batch)
                .FirstOrDefaultAsync(bs => bs.BatchSemesterId == batchSemesterId);

            if (batchSemester == null)
                return (false, "Batch semester not found", 0);

            var gradesheets = await _db.Gradesheets
                .Include(g => g.SubjectConfig)
                .Where(g => g.SubjectConfig.BatchSemesterId == batchSemesterId &&
                            g.Status == GradesheetStatus.Approved)
                .ToListAsync();

            Console.WriteLine($"[IMPORT] Found {gradesheets.Count} approved gradesheets");

            if (!gradesheets.Any())
                return (false, "No approved gradesheets found", 0);

            var allStudents = await _db.Students
                .Where(s => s.BatchId == batchSemester.BatchId)
                .ToListAsync();

            Console.WriteLine($"[IMPORT] Found {allStudents.Count} students in batch");

            int totalImported = 0;

            foreach (var gradesheet in gradesheets)
            {
                Console.WriteLine($"\n[IMPORT] ----- Gradesheet {gradesheet.GradesheetId}: " +
                    $"{gradesheet.SubjectConfig.SubjectName} -----");
                Console.WriteLine($"[IMPORT] FilePath (raw): {gradesheet.FilePath}");

                // Remove old marks first
                var existingMarks = await _db.StudentMarks
                    .Where(m => m.GradesheetId == gradesheet.GradesheetId)
                    .ToListAsync();

                if (existingMarks.Any())
                {
                    _db.StudentMarks.RemoveRange(existingMarks);
                    await _db.SaveChangesAsync();
                    Console.WriteLine($"[IMPORT] Cleared {existingMarks.Count} old marks");
                }

                // ── ALWAYS read from Excel file (CustomisedData disabled) ────
                if (string.IsNullOrEmpty(gradesheet.FilePath))
                {
                    Console.WriteLine($"[IMPORT] ERROR: No FilePath for gradesheet {gradesheet.GradesheetId}");
                    continue;
                }

                // Resolve relative → absolute path
                string absolutePath = Path.IsPathRooted(gradesheet.FilePath)
                    ? gradesheet.FilePath
                    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), gradesheet.FilePath));

                Console.WriteLine($"[IMPORT] Resolved path: {absolutePath}");

                if (!File.Exists(absolutePath))
                {
                    var altPath = Path.GetFullPath(
                        Path.Combine(Directory.GetCurrentDirectory(), "..", gradesheet.FilePath));
                    Console.WriteLine($"[IMPORT] Not found, trying: {altPath}");

                    if (!File.Exists(altPath))
                    {
                        Console.WriteLine($"[IMPORT] ERROR: File not found. Skipping.");
                        continue;
                    }
                    absolutePath = altPath;
                }

                Console.WriteLine($"[IMPORT] Reading file: {absolutePath}");
                var fileBytes = await File.ReadAllBytesAsync(absolutePath);

                var parseResult = _excelImport.ParseGradesheet(
                    fileBytes,
                    gradesheet.SubjectConfig,
                    allStudents);

                if (parseResult.Errors.Any())
                {
                    Console.WriteLine($"[IMPORT] Excel parse ERRORS: {string.Join(", ", parseResult.Errors)}");
                    continue;
                }

                foreach (var w in parseResult.Warnings)
                    Console.WriteLine($"[IMPORT] Warning: {w}");

                Console.WriteLine($"[IMPORT] Excel parsed {parseResult.Marks.Count} marks");

                foreach (var mark in parseResult.Marks)
                {
                    mark.GradesheetId = gradesheet.GradesheetId;
                    _db.StudentMarks.Add(mark);
                    totalImported++;
                }

                await _db.SaveChangesAsync();
                Console.WriteLine($"[IMPORT] Saved {parseResult.Marks.Count} marks for gradesheet {gradesheet.GradesheetId}");
            }

            Console.WriteLine($"\n[IMPORT] ===== DONE: {totalImported} total marks imported =====\n");
            return (true, $"Imported {totalImported} marks", totalImported);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IMPORT ERROR] {ex.Message}");
            Console.WriteLine($"[IMPORT STACK] {ex.StackTrace}");
            return (false, $"Error: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, decimal sgpa)> CalculateSGPAForStudentAsync(
        int studentId, int semesterNo, int batchSemesterId)
    {
        try
        {
            var marks = await _db.StudentMarks
                .Include(m => m.SubjectConfig)
                .Where(m => m.StudentId == studentId &&
                            m.SubjectConfig.BatchSemesterId == batchSemesterId)
                .ToListAsync();

            Console.WriteLine($"[SGPA] Student {studentId}: {marks.Count} marks found");

            if (!marks.Any()) return (false, 0);

            var subjectResults = marks
                .Select(m => (m.GradePoints, m.SubjectConfig.CreditHours))
                .ToList();

            decimal sgpa = _gpa.CalculateSGPA(subjectResults);

            var existingSgpa = await _db.SGPAResults
                .FirstOrDefaultAsync(s => s.StudentId == studentId &&
                                          s.SemesterNo == semesterNo);

            if (existingSgpa != null)
            {
                existingSgpa.SGPA = sgpa;
                existingSgpa.TotalCreditHours = subjectResults.Sum(s => s.CreditHours);
                _db.SGPAResults.Update(existingSgpa);
            }
            else
            {
                _db.SGPAResults.Add(new SGPAResult
                {
                    StudentId = studentId,
                    SemesterNo = semesterNo,
                    SGPA = sgpa,
                    TotalCreditHours = subjectResults.Sum(s => s.CreditHours)
                });
            }

            await _db.SaveChangesAsync();
            return (true, sgpa);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SGPA ERROR] {ex.Message}");
            return (false, 0);
        }
    }

    public async Task<(bool success, decimal cgpa)> CalculateCGPAForStudentAsync(
        int studentId, int upToSemester)
    {
        try
        {
            var sgpaResults = await _db.SGPAResults
                .Where(s => s.StudentId == studentId && s.SemesterNo <= upToSemester)
                .ToListAsync();

            if (!sgpaResults.Any()) return (false, 0);

            decimal cgpa = _gpa.CalculateCGPA(sgpaResults.Select(s => s.SGPA).ToList());

            var existingCgpa = await _db.CGPAResults
                .FirstOrDefaultAsync(c => c.StudentId == studentId &&
                                          c.UpToSemester == upToSemester);

            if (existingCgpa != null)
            {
                existingCgpa.CGPA = cgpa;
                _db.CGPAResults.Update(existingCgpa);
            }
            else
            {
                _db.CGPAResults.Add(new CGPAResult
                {
                    StudentId = studentId,
                    UpToSemester = upToSemester,
                    CGPA = cgpa
                });
            }

            await _db.SaveChangesAsync();
            return (true, cgpa);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CGPA ERROR] {ex.Message}");
            return (false, 0);
        }
    }
}