using Microsoft.EntityFrameworkCore;
using URMS.Data;
using URMS.Models.DTOs;
using URMS.Models.Entities;
using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

public class ResultGenerationService
{
    private readonly AppDbContext _db;
    private readonly ResultProcessingService _resultProcessing;
    private readonly IGpaCalculationService _gpa;

    public ResultGenerationService(
        AppDbContext db,
        ResultProcessingService resultProcessing,
        IGpaCalculationService gpa)
    {
        _db = db;
        _resultProcessing = resultProcessing;
        _gpa = gpa;
    }

    public async Task<(bool success, string message)> GenerateResultAsync(int batchSemesterId)
    {
        try
        {
            Console.WriteLine($"\n=== GENERATE RESULT START ===");
            Console.WriteLine($"BatchSemesterId: {batchSemesterId}");

            var batchSemester = await _db.BatchSemesters
    .Include(bs => bs.Batch)
        .ThenInclude(b => b.Program)   // ← ADDED
    .Include(bs => bs.Subjects)
    .FirstOrDefaultAsync(bs => bs.BatchSemesterId == batchSemesterId);

            if (batchSemester == null)
                return (false, "Batch semester not found");

            Console.WriteLine($"Batch: {batchSemester.Batch.BatchName}");

            var students = await _db.Students
                .Where(s => s.BatchId == batchSemester.BatchId)
                .ToListAsync();

            Console.WriteLine($"Total Students: {students.Count}");

            // Step 1: Import grades
            Console.WriteLine("[RESULT] Importing grades from gradesheets...");
            var (importSuccess, importMsg, marksCount) =
                await _resultProcessing.ImportGradesForBatchSemesterAsync(batchSemesterId);
            Console.WriteLine($"[RESULT] Import done: success={importSuccess}, marks={marksCount}, msg={importMsg}");

            if (!importSuccess || marksCount == 0)
                return (false, $"Grade import failed: {importMsg}");

            // Step 2: Calculate SGPA
            Console.WriteLine($"\n--- Calculating SGPA for {students.Count} students ---");
            int successCount = 0;
            foreach (var student in students)
            {
                var (success, sgpa) = await _resultProcessing.CalculateSGPAForStudentAsync(
                    student.StudentId, batchSemester.SemesterNo, batchSemesterId);
                if (success) successCount++;
                Console.WriteLine($"  Student {student.RegistrationNo}: SGPA={sgpa}, OK={success}");
            }
            Console.WriteLine($"SGPA done: {successCount}/{students.Count}");

            // Step 3: Calculate CGPA
            Console.WriteLine($"\n--- Calculating CGPA for {students.Count} students ---");
            successCount = 0;
            foreach (var student in students)
            {
                var (success, cgpa) = await _resultProcessing.CalculateCGPAForStudentAsync(
                    student.StudentId, batchSemester.SemesterNo);
                if (success) successCount++;
                Console.WriteLine($"  Student {student.RegistrationNo}: CGPA={cgpa}, OK={success}");
            }
            Console.WriteLine($"CGPA done: {successCount}/{students.Count}");

            batchSemester.IsResultGenerated = true;
            batchSemester.ResultGeneratedAt = DateTime.UtcNow;
            _db.BatchSemesters.Update(batchSemester);
            await _db.SaveChangesAsync();

            Console.WriteLine($"=== GENERATE RESULT COMPLETE ===\n");
            return (true, $"Result generated for {students.Count} students");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FATAL ERROR] {ex.Message}");
            Console.WriteLine($"[STACK] {ex.StackTrace}");
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<ResultSummaryDto> GetResultSummaryAsync(int batchSemesterId)
    {
        var batchSemester = await _db.BatchSemesters
            .Include(bs => bs.Batch)
            .Include(bs => bs.Subjects)
            .FirstOrDefaultAsync(bs => bs.BatchSemesterId == batchSemesterId);

        if (batchSemester == null)
            return new ResultSummaryDto();

        var students = await _db.Students
            .Where(s => s.BatchId == batchSemester.BatchId)
            .ToListAsync();

        var studentResults = new List<StudentResultDto>();

        foreach (var student in students)
        {
            var marks = await _db.StudentMarks
                .Include(m => m.SubjectConfig)
                .Where(m => m.StudentId == student.StudentId &&
                            m.SubjectConfig.BatchSemesterId == batchSemesterId)
                .ToListAsync();

            var sgpa = await _db.SGPAResults
                .FirstOrDefaultAsync(s => s.StudentId == student.StudentId &&
                                          s.SemesterNo == batchSemester.SemesterNo);

            var cgpa = await _db.CGPAResults
                .FirstOrDefaultAsync(c => c.StudentId == student.StudentId &&
                                          c.UpToSemester == batchSemester.SemesterNo);

            var subjectMarks = marks.Select(m => new SubjectMarkDto
            {
                SubjectCode = m.SubjectConfig.SubjectCode,
                SubjectName = m.SubjectConfig.SubjectName,
                SubjectType = m.SubjectConfig.SubjectType.ToString(),
                CreditHours = m.SubjectConfig.CreditHours,
                MarksObtained = m.MarksObtained,
                Grade = m.Grade,
                GradePoints = m.GradePoints
            }).ToList();

            studentResults.Add(new StudentResultDto
            {
                StudentId = student.StudentId,
                RegistrationNo = student.RegistrationNo,
                FullName = student.FullName,
                FatherName = student.FatherName,  // ← FIXED
                SGPA = sgpa?.SGPA ?? 0,
                CGPA = cgpa?.CGPA ?? 0,
                SubjectMarks = subjectMarks,
                IsPass = subjectMarks.All(sm => sm.Grade != "F"),
                TotalCreditHours = sgpa?.TotalCreditHours ?? 0
            });
        }

        return new ResultSummaryDto
        {
            BatchSemesterId = batchSemesterId,
            BatchName = batchSemester.Batch.BatchName,
            SemesterNo = batchSemester.SemesterNo,
            Program = batchSemester.Batch.Program?.ProgramCode ?? "",
            Section = batchSemester.Batch.BatchNumber.ToString(),
            TotalStudents = students.Count,
            PassedStudents = studentResults.Count(r => r.IsPass),
            FailedStudents = studentResults.Count(r => !r.IsPass),
            ClassAverageSGPA = studentResults.Any()
                ? Math.Round(studentResults.Average(r => r.SGPA), 2) : 0,
            StudentResults = studentResults.OrderByDescending(s => s.CGPA).ToList()
        };
    }
}