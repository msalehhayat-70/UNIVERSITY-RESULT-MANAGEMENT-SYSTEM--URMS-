using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using URMS.Data;
using URMS.Models.DTOs;
using URMS.Models.Entities;
using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<int> GetRegisteredAccountsCountAsync()
    {
        var config = await _db.SystemConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == "registered_accounts");
        return config is null ? 0 : int.Parse(config.ConfigValue);
    }

    private async Task IncrementAccountCountAsync()
    {
        var config = await _db.SystemConfigs
            .FirstAsync(c => c.ConfigKey == "registered_accounts");
        config.ConfigValue = (int.Parse(config.ConfigValue) + 1).ToString();
        await _db.SaveChangesAsync();
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResponse<LoginResponse>.Fail("Invalid email or password.");

        var token = GenerateJwtToken(user);
        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            UserId = user.UserId
        });
    }

    public async Task<ApiResponse<LoginResponse>> RegisterFirstAccountAsync(RegisterRequest request)
    {
        var count = await GetRegisteredAccountsCountAsync();
        if (count >= 2)
            return ApiResponse<LoginResponse>.Fail("Registration is closed. System already has its initial accounts.");

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return ApiResponse<LoginResponse>.Fail("Email already in use.");

        // First account = Examiner, Second account = HOD
        var role = count == 0 ? UserRole.Examiner : UserRole.HOD;

        var user = new User
        {
            FullName     = request.FullName,
            Email        = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await IncrementAccountCountAsync();

        var token = GenerateJwtToken(user);
        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            UserId = user.UserId
        }, $"Account created as {role}.");
    }

    public async Task<ApiResponse> CreateTeacherAccountAsync(CreateTeacherRequest request, int createdByUserId)
    {
        var creator = await _db.Users.FindAsync(createdByUserId);
        if (creator is null || (creator.Role != UserRole.Examiner && creator.Role != UserRole.HOD))
            return ApiResponse.Fail("Only Examiner or HOD can create teacher accounts.");

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return ApiResponse.Fail("Email already in use.");

        var teacher = new User
        {
            FullName     = request.FullName,
            Email        = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = UserRole.Teacher,
            CreatedById  = createdByUserId
        };

        _db.Users.Add(teacher);
        await _db.SaveChangesAsync();
        return ApiResponse.Ok($"Teacher account created. Credentials: {request.Email} / {request.Password}");
    }

    private string GenerateJwtToken(User user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(7);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims:   claims,
            expires:  expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
