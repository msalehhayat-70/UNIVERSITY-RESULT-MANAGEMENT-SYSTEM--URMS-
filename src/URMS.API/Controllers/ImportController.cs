using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using URMS.Data;
using URMS.Services.Implementations;

namespace URMS.API.Controllers;

[ApiController]
[Route("api/import")]
[Authorize(Roles = "Examiner")]
public class ImportController : ControllerBase
{
    private readonly ExcelImportService _excelService;
    private readonly AppDbContext _db;
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public ImportController(ExcelImportService excelService, AppDbContext db)
    {
        _excelService = excelService;
        _db = db;
    }

    /// <summary>Import students from Excel file for bulk registration</summary>
    [HttpPost("students")]
    public async Task<IActionResult> ImportStudentsFromExcel(IFormFile file, [FromQuery] int batchId)
    {
        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file uploaded." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, message = "Only .xlsx or .xls files are supported." });

        try
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                // Call ExcelImportService to import students
                var result = await _excelService.ImportStudentsAsync(stream, batchId, _db);

                if (!result.Success)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Import failed",
                        errors = result.Errors
                    });

                return Ok(new
                {
                    success = true,
                    message = $"Successfully imported {result.AddedCount} students.",
                    added = result.AddedCount,
                    warnings = result.Warnings,
                    totalProcessed = result.AddedCount + result.Warnings.Count
                });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Error processing file: {ex.Message}"
            });
        }
    }
}
