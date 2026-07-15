using System.Security.Claims;
using MaxVerse.API.Data;
using MaxVerse.API.DTOs;
using MaxVerse.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaxVerse.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MaxVerseDbContext _context;
    private readonly JwtHelper _jwtHelper;

    public AuthController(MaxVerseDbContext context, JwtHelper jwtHelper)
    {
        _context = context;
        _jwtHelper = jwtHelper;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Email và mật khẩu là bắt buộc." });

        if (dto.Password.Length < 6)
            return BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự." });

        var existing = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (existing)
            return BadRequest(new { message = "Email này đã được sử dụng." });

        var user = new Models.User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber,
            RoleId = 2 // Customer mặc định
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.Role = await _context.Roles.FindAsync(user.RoleId);
        var token = _jwtHelper.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role?.RoleName ?? "Customer"
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Tài khoản đã bị khóa." });

        var token = _jwtHelper.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role?.RoleName ?? "Customer"
        });
    }
    // GET /api/auth/profile — lấy thông tin hồ sơ
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.Address
        });
    }

    // PUT /api/auth/profile — cập nhật hồ sơ
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Address = dto.Address;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật hồ sơ thành công." });
    }
}
