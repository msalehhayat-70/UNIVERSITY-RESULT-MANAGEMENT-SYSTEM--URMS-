using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using URMS.Data;
using URMS.Models.DTOs;

namespace URMS.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public ProfileController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users.FindAsync(UserId);
        if (user is null) return NotFound();
        return Ok(ApiResponse<object>.Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            Role = user.Role.ToString()
        }));
    }

    [HttpPut("name")]
    public async Task<IActionResult> UpdateName([FromBody] UpdateNameRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName))
            return BadRequest(ApiResponse.Fail("Name cannot be empty."));

        var user = await _db.Users.FindAsync(UserId);
        if (user is null) return NotFound();
        user.FullName = req.FullName.Trim();
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Name updated successfully."));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        if (req.NewPassword.Length < 6)
            return BadRequest(ApiResponse.Fail("New password must be at least 6 characters."));

        var user = await _db.Users.FindAsync(UserId);
        if (user is null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(ApiResponse.Fail("Current password is incorrect."));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Password changed successfully."));
    }
}

public class UpdateNameRequest    { public string FullName        { get; set; } = ""; }
public class ChangePasswordRequest{ public string CurrentPassword { get; set; } = "";
                                    public string NewPassword     { get; set; } = ""; }
