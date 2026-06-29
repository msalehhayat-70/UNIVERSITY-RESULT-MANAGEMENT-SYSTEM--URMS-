using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using URMS.Data;
using URMS.Models.DTOs;
using URMS.Models.Entities;

namespace URMS.API.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = "Examiner")]
public class StudentController : ControllerBase
{
    private readonly AppDbContext _db;
    public StudentController(AppDbContext db) => _db = db;

    /// <summary>Get all students for a batch</summary>
    [HttpGet("batch/{batchId:int}")]
    public async Task<IActionResult> GetByBatch(int batchId)
    {
        var students = await _db.Students
            .Include(s => s.Batch)
            .Where(s => s.BatchId == batchId && s.IsActive)
            .OrderBy(s => s.RegistrationNo)
            .Select(s => new StudentDto
            {
                StudentId = s.StudentId,
                RegistrationNo = s.RegistrationNo,
                FullName = s.FullName,
                FatherName = s.FatherName,
                BatchName = s.Batch.BatchName,
                CurrentSemester = s.CurrentSemester,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(ApiResponse<List<StudentDto>>.Ok(students));
    }

    /// <summary>Add a single student to a batch</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RegistrationNo))
            return BadRequest(ApiResponse.Fail("Registration number is required."));

        // Block only if an ACTIVE student with same reg no exists
        if (await _db.Students.AnyAsync(s => s.RegistrationNo == req.RegistrationNo && s.IsActive))
            return BadRequest(ApiResponse.Fail($"Registration No '{req.RegistrationNo}' already exists."));

        // If previously soft-deleted, reactivate and update instead of duplicating
        var existing = await _db.Students.FirstOrDefaultAsync(s => s.RegistrationNo == req.RegistrationNo);
        if (existing != null)
        {
            existing.FullName = req.FullName.Trim();
            existing.FatherName = req.FatherName.Trim();
            existing.BatchId = req.BatchId;
            existing.CurrentSemester = 1;
            existing.IsActive = true;
        }
        else
        {
            _db.Students.Add(new Student
            {
                RegistrationNo = req.RegistrationNo.Trim(),
                FullName = req.FullName.Trim(),
                FatherName = req.FatherName.Trim(),
                BatchId = req.BatchId,
                CurrentSemester = 1,
                IsActive = true
            });
        }

        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok($"Student '{req.FullName}' added successfully."));
    }

    /// <summary>Bulk import students for a batch</summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportStudentsRequest req)
    {
        int added = 0, skipped = 0;
        var warnings = new List<string>();

        foreach (var s in req.Students)
        {
            if (string.IsNullOrWhiteSpace(s.RegistrationNo))
            { warnings.Add("Skipped row — empty registration number."); skipped++; continue; }

            // Skip only if active duplicate
            if (await _db.Students.AnyAsync(x => x.RegistrationNo == s.RegistrationNo && x.IsActive))
            { warnings.Add($"Skipped '{s.RegistrationNo}' — already exists."); skipped++; continue; }

            // Reactivate if soft-deleted
            var existing = await _db.Students.FirstOrDefaultAsync(x => x.RegistrationNo == s.RegistrationNo);
            if (existing != null)
            {
                existing.FullName = s.FullName.Trim();
                existing.FatherName = s.FatherName.Trim();
                existing.BatchId = req.BatchId;
                existing.CurrentSemester = 1;
                existing.IsActive = true;
            }
            else
            {
                _db.Students.Add(new Student
                {
                    RegistrationNo = s.RegistrationNo.Trim(),
                    FullName = s.FullName.Trim(),
                    FatherName = s.FatherName.Trim(),
                    BatchId = req.BatchId,
                    CurrentSemester = 1,
                    IsActive = true
                });
            }
            added++;
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, added, skipped, warnings });
    }

    /// <summary>Advance student's current semester after result generation</summary>
    [HttpPatch("{studentId:int}/advance-semester")]
    public async Task<IActionResult> AdvanceSemester(int studentId)
    {
        var student = await _db.Students.FindAsync(studentId);
        if (student is null) return NotFound(ApiResponse.Fail("Student not found."));
        if (student.CurrentSemester >= 8) return BadRequest(ApiResponse.Fail("Already at semester 8."));
        student.CurrentSemester++;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok($"Advanced to semester {student.CurrentSemester}."));
    }

    /// <summary>Hard-delete a student from DB</summary>
    [HttpDelete("{studentId:int}")]
    public async Task<IActionResult> Delete(int studentId)
    {
        var student = await _db.Students.FindAsync(studentId);
        if (student is null) return NotFound(ApiResponse.Fail("Student not found."));
        _db.Students.Remove(student);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Student deleted successfully."));
    }
}