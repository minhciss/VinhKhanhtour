using Microsoft.AspNetCore.Mvc;
using VinhKhanhCMS.Data;
using VinhKhanhCMS.Models;

namespace VinhKhanhCMS.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // ────────────────────────────────────────
    // POST /api/auth/login
    // ────────────────────────────────────────
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var user = _context.AppUsers.FirstOrDefault(u =>
            u.Username == req.Username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Sai tên đăng nhập hoặc mật khẩu");

        if (user.Role == "Owner" && user.Status != "Approved")
            return Unauthorized("Tài khoản chưa được Admin phê duyệt");

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.Role,
            user.FullName,
            user.Status
        });
    }

    // ────────────────────────────────────────
    // POST /api/auth/register-owner
    // ────────────────────────────────────────
    [HttpPost("register-owner")]
    public async Task<IActionResult> RegisterOwner([FromBody] RegisterOwnerRequest req)
    {
        if (_context.AppUsers.Any(u => u.Username == req.Username))
            return BadRequest("Tên đăng nhập đã tồn tại");

        if (_context.AppUsers.Any(u => u.Email == req.Email))
            return BadRequest("Email đã được sử dụng");

        var user = new AppUser
        {
            Username = req.Username,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = "Owner",
            Status = "Pending",      // Chờ Admin duyệt
            FullName = req.FullName,
            Phone = req.Phone,
            BusinessName = req.BusinessName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đăng ký thành công. Vui lòng chờ Admin phê duyệt." });
    }

    // ────────────────────────────────────────
    // GET /api/auth/users?role=Owner&status=Pending
    // ────────────────────────────────────────
    [HttpGet("users")]
    public IActionResult GetUsers([FromQuery] string? role, [FromQuery] string? status)
    {
        var query = _context.AppUsers.AsQueryable();
        if (!string.IsNullOrEmpty(role)) query = query.Where(u => u.Role == role);
        if (!string.IsNullOrEmpty(status)) query = query.Where(u => u.Status == status);
        return Ok(query.OrderByDescending(u => u.CreatedAt).ToList());
    }

    // ────────────────────────────────────────
    // PUT /api/auth/users/{id}/approve
    // ────────────────────────────────────────
    [HttpPut("users/{id}/approve")]
    public async Task<IActionResult> ApproveUser(int id, [FromBody] ApproveRequest req)
    {
        var user = _context.AppUsers.Find(id);
        if (user == null) return NotFound();

        user.Status = req.Approve ? "Approved" : "Rejected";
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    // ────────────────────────────────────────
    // PUT /api/auth/users/{id}/toggle
    // ────────────────────────────────────────
    [HttpPut("users/{id}/toggle")]
    public async Task<IActionResult> ToggleUser(int id)
    {
        var user = _context.AppUsers.Find(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    // ────────────────────────────────────────
    // POST /api/auth/seed-admin
    // Chỉ dùng lần đầu để tạo admin gốc — yêu cầu header X-Setup-Key
    // ────────────────────────────────────────
    [HttpPost("seed-admin")]
    public async Task<IActionResult> SeedAdmin([FromHeader(Name = "X-Setup-Key")] string? setupKey)
    {
        // Bảo vệ bằng secret key cố định — chỉ người biết key mới tạo được admin
        if (setupKey != "vinhkhanh-setup-2026")
            return Unauthorized("Thiếu hoặc sai X-Setup-Key header");

        if (_context.AppUsers.Any(u => u.Role == "Admin"))
            return BadRequest("Admin đã tồn tại");

        var admin = new AppUser
        {
            Username = "admin",
            Email = "admin@vinhkhanh.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            Status = "Approved",
            FullName = "Quản Trị Viên",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.AppUsers.Add(admin);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Admin mặc định đã được tạo. Username: admin / Password: Admin@123" });
    }
}

// ── DTOs ──────────────────────────────────
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterOwnerRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
}

public class ApproveRequest
{
    public bool Approve { get; set; }
}
