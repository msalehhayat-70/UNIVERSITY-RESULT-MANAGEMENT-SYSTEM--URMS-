using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using URMS.Models.DTOs;
using URMS.Services.Interfaces;

namespace URMS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Returns number of registered accounts (0 or 1 = show register button, 2+ = hide it)</summary>
    [HttpGet("registration-status")]
    public async Task<IActionResult> GetRegistrationStatus()
    {
        var count = await _auth.GetRegisteredAccountsCountAsync();
        return Ok(new { registeredAccounts = count, canRegister = count < 2 });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>First account = Examiner, Second account = HOD. Button hidden after 2 accounts.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _auth.RegisterFirstAccountAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Only Examiner or HOD can create teacher accounts</summary>
    [HttpPost("create-teacher")]
    [Authorize(Roles = "Examiner,HOD")]
    public async Task<IActionResult> CreateTeacher([FromBody] CreateTeacherRequest request)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await _auth.CreateTeacherAccountAsync(request, userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
