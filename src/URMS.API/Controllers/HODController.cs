using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using URMS.Models.DTOs;
using URMS.Services.Interfaces;
using URMS.Services.Implementations;
using OfficeOpenXml;
using Microsoft.Extensions.DependencyInjection;

namespace URMS.API.Controllers;

[ApiController]
[Route("api/hod")]
[Authorize(Roles = "HOD")]
public class HODController : ControllerBase
{
    private readonly IHODService _hod;
    private readonly IAuthService _auth;
    private readonly IServiceScopeFactory _scopeFactory;
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public HODController(IHODService hod, IAuthService auth, IServiceScopeFactory scopeFactory)
    {
        _hod = hod;
        _auth = auth;
        _scopeFactory = scopeFactory;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard() =>
        Ok(await _hod.GetDashboardAsync(UserId));

    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches() =>
        Ok(await _hod.GetAllBatchesAsync());

    [HttpGet("semesters/{batchSemesterId:int}/gradesheets")]
    public async Task<IActionResult> GetGradesheets(int batchSemesterId) =>
        Ok(await _hod.GetGradsheetsForSemesterAsync(batchSemesterId));

    [HttpGet("gradesheets/{gradesheetId:int}")]
    public async Task<IActionResult> GetGradesheetDetail(int gradesheetId)
    {
        var r = await _hod.GetGradesheetDetailAsync(gradesheetId);
        return r.Success ? Ok(r) : NotFound(r);
    }

    [HttpPost("gradesheets/{gradesheetId:int}/approve")]
    public async Task<IActionResult> Approve(int gradesheetId)
    {
        var r = await _hod.ApproveGradesheetAsync(gradesheetId, UserId);
        if (!r.Success) return BadRequest(r);

        // Fire-and-forget auto-generate with its own DI scope
        var capturedId = gradesheetId;
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var hodSvc = scope.ServiceProvider.GetRequiredService<IHODService>();
                var resultGen = scope.ServiceProvider.GetRequiredService<ResultGenerationService>();

                var batchSemesterId = await hodSvc.GetBatchSemesterIdForGradesheetAsync(capturedId);
                if (batchSemesterId <= 0) return;

                var allApproved = await hodSvc.AreAllGradsheetsApprovedAsync(batchSemesterId);
                if (!allApproved)
                {
                    Console.WriteLine($"[AUTO-GENERATE] Not all approved yet for semester {batchSemesterId}");
                    return;
                }

                Console.WriteLine($"[AUTO-GENERATE] All approved for semester {batchSemesterId}. Generating...");
                var (success, message) = await resultGen.GenerateResultAsync(batchSemesterId);
                Console.WriteLine($"[AUTO-GENERATE] success={success}, message={message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTO-GENERATE ERROR] {ex.Message}");
            }
        });

        return Ok(r);
    }

    [HttpPost("gradesheets/{gradesheetId:int}/reject")]
    public async Task<IActionResult> Reject(int gradesheetId, [FromBody] RejectRequest req)
    {
        var r = await _hod.RejectGradesheetAsync(gradesheetId, UserId, req.Remarks);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("gradesheets/{gradesheetId:int}/customise")]
    public async Task<IActionResult> Customise(int gradesheetId, [FromBody] CustomiseRequest req)
    {
        var r = await _hod.CustomiseGradesheetAsync(gradesheetId, UserId, req);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpPost("gradesheets/{gradesheetId:int}/customise-grid")]
    public async Task<IActionResult> CustomiseGrid(int gradesheetId, [FromBody] GridCustomiseRequest req)
    {
        var r = await _hod.CustomiseWithGridAsync(gradesheetId, UserId, req.EditedRows, req.RegNoCol, req.TotalCol);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("gradesheets/{gradesheetId:int}/download")]
    public async Task<IActionResult> DownloadGradesheet(int gradesheetId)
    {
        var gs = await _hod.GetGradesheetFileAsync(gradesheetId);
        if (gs == null || string.IsNullOrEmpty(gs.FilePath) || !System.IO.File.Exists(gs.FilePath))
            return NotFound(ApiResponse.Fail("File not found."));
        var bytes = await System.IO.File.ReadAllBytesAsync(gs.FilePath);
        var fileName = System.IO.Path.GetFileName(gs.FilePath);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications() =>
        Ok(await _hod.GetNotificationsAsync(UserId));

    [HttpPatch("notifications/{notifId:int}/read")]
    public async Task<IActionResult> MarkRead(int notifId) =>
        Ok(await _hod.MarkNotificationReadAsync(notifId));

    [HttpPost("create-teacher")]
    public async Task<IActionResult> CreateTeacher([FromBody] CreateTeacherRequest req)
    {
        var r = await _auth.CreateTeacherAccountAsync(req, UserId);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpGet("gradesheets/{gradesheetId:int}/preview")]
    public async Task<IActionResult> PreviewGradesheet(int gradesheetId)
    {
        var gs = await _hod.GetGradesheetFileAsync(gradesheetId);

        if (!string.IsNullOrEmpty(gs.CustomisedData))
        {
            var savedRows = System.Text.Json.JsonSerializer.Deserialize<List<List<string>>>(gs.CustomisedData);
            return Ok(new { success = true, rows = savedRows });
        }

        if (string.IsNullOrEmpty(gs.FilePath) || !System.IO.File.Exists(gs.FilePath))
            return NotFound(ApiResponse.Fail("File not found."));

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new FileInfo(gs.FilePath));
        var ws = pkg.Workbook.Worksheets.FirstOrDefault();
        if (ws == null) return BadRequest(ApiResponse.Fail("Empty file."));

        var rows = new List<List<string>>();
        int lastRow = ws.Dimension?.End.Row ?? 0;
        int lastCol = ws.Dimension?.End.Column ?? 0;

        for (int r = 1; r <= lastRow; r++)
        {
            var row = new List<string>();
            for (int c = 1; c <= lastCol; c++)
                row.Add(ws.Cells[r, c].Text ?? "");
            rows.Add(row);
        }

        return Ok(new { success = true, rows });
    }
}

public class RejectRequest { public string Remarks { get; set; } = ""; }
public class CustomiseRequest { public List<MarkUpdate> UpdatedMarks { get; set; } = new(); }
public class MarkUpdate
{
    public int StudentId { get; set; }
    public decimal MarksObtained { get; set; }
}
public class GridCustomiseRequest
{
    public List<List<string>> EditedRows { get; set; } = new();
    public int RegNoCol { get; set; }
    public int TotalCol { get; set; }
}