using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhCMS.Data;
using VinhKhanhCMS.Models;

namespace VinhKhanhCMS.Controllers;

[ApiController]
[Route("api/pois")]
public class PoiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public PoiController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    private string GetBaseUrl()
    {
        // 1. Ưu tiên biến môi trường (Render)
        var apiUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrEmpty(apiUrl)) return apiUrl.TrimEnd('/');

        // 2. Ưu tiên cấu hình appsettings.json
        apiUrl = _config["ApiBaseUrl"];
        if (!string.IsNullOrEmpty(apiUrl)) return apiUrl.TrimEnd('/');

        // 3. Fallback cuối cùng
        return $"{Request.Scheme}://{Request.Host}";
    }

    /// <summary>
    /// Sửa URL ảnh/audio: thay localhost bằng production URL,
    /// và gắn baseUrl vào nếu chỉ là đường dẫn tương đối.
    /// </summary>
    private string FixUrl(string? url, string baseUrl)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        // Tự động thay localhost/127.0.0.1 bằng URL server thử
        if (url.Contains("localhost") || url.Contains("127.0.0.1"))
        {
            var uri = new Uri(url);
            url = baseUrl + uri.PathAndQuery;
            return url;
        }

        // Gắn baseUrl nếu đường dẫn tương đối
        if (!url.StartsWith("http"))
        {
            url = url.StartsWith("/") ? baseUrl + url : baseUrl + "/images/" + url;
        }

        return url;
    }

    // 🔍 Lấy tất cả POI — có thể lọc theo ?status=Pending&ownerId=5
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? status, [FromQuery] int? ownerId)
    {
        var query = _context.Pois
            .Include(p => p.Translations)
            .AsQueryable();

        // Lọc theo status (Pending / Approved / Rejected)
        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        // Lọc theo ownerId (Owner chỉ xem POI của mình)
        if (ownerId.HasValue)
            query = query.Where(p => p.OwnerId == ownerId.Value);

        var data = query.ToList();
        var baseUrl = GetBaseUrl();

        foreach (var poi in data)
        {
            poi.ImageUrl = FixUrl(poi.ImageUrl, baseUrl);

            if (poi.Translations != null)
                foreach (var t in poi.Translations)
                    t.AudioUrl = FixUrl(t.AudioUrl, baseUrl);
        }

        return Ok(data);
    }

    [HttpPut("{id}/toggle")]
    public IActionResult Toggle(int id)
    {
        var poi = _context.Pois.Find(id);
        if (poi == null) return NotFound();

        poi.IsActive = !poi.IsActive;
        _context.SaveChanges();

        return Ok(poi);
    }

    // ✅ Admin duyệt / từ chối POI
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovePoiRequest req)
    {
        var poi = _context.Pois.Find(id);
        if (poi == null) return NotFound();

        poi.Status = req.Approve ? "Approved" : "Rejected";
        if (req.Approve) poi.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(poi);
    }

    // 🔍 Lấy 1 POI theo id
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var poi = _context.Pois
            .Include(p => p.Translations)
            .FirstOrDefault(p => p.Id == id);

        if (poi == null) return NotFound();

        var baseUrl = GetBaseUrl();

        poi.ImageUrl = FixUrl(poi.ImageUrl, baseUrl);

        if (poi.Translations != null)
            foreach (var t in poi.Translations)
                t.AudioUrl = FixUrl(t.AudioUrl, baseUrl);

        return Ok(poi);
    }

    // ➕ Tạo POI
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Poi poi)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var owner = await _context.AppUsers.FindAsync(poi.OwnerId);
        if (owner == null) return NotFound("Không tìm thấy Owner");

        // Bắt buộc phải mua gói mới được tạo POI (Gói phải còn hạn)
        if (owner.SubscriptionExpiryDate == null || owner.SubscriptionExpiryDate < DateTime.UtcNow)
        {
            return StatusCode(403, "Bạn phải đăng ký mua Gói Dịch Vụ / VIP để có thể đăng địa điểm mới.");
        }

        // Tự động Approved vì đã đóng tiền
        poi.Status = "Approved";
        poi.IsActive = true;

        _context.Pois.Add(poi);
        await _context.SaveChangesAsync();

        return Ok(poi);
    }

    // ✏️ Update POI
    [HttpPut("{id}")]
    public IActionResult Update(int id, Poi updated)
    {
        var poi = _context.Pois.Find(id);
        if (poi == null) return NotFound();

        poi.Name = updated.Name;
        poi.Description = updated.Description;
        poi.Address = updated.Address;
        poi.Latitude = updated.Latitude;
        poi.Longitude = updated.Longitude;
        poi.ImageUrl = updated.ImageUrl;
        // ⚠️ IsActive và Status chỉ được Admin thay đổi — không cập nhật từ body
        _context.SaveChanges();

        return Ok(poi);
    }
    
    // 📸 Upload Image for POI
    [HttpPost("{id}/upload-image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile imageFile)
    {
        var poi = _context.Pois.Find(id);
        if (poi == null) return NotFound();

        if (imageFile == null || imageFile.Length == 0)
            return BadRequest("No file provided");

        var extension = Path.GetExtension(imageFile.FileName);
        var filename = $"poi_{id}_{DateTime.UtcNow.Ticks}{extension}";
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "pois");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var filepath = Path.Combine(folder, filename);
        using (var stream = new FileStream(filepath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }

        poi.ImageUrl = "/images/pois/" + filename;
        await _context.SaveChangesAsync();

        return Ok(new { ImageUrl = poi.ImageUrl });
    }

    // ❌ Xóa POI
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var poi = _context.Pois.Find(id);
        if (poi == null) return NotFound();

        _context.Pois.Remove(poi);
        _context.SaveChanges();

        return Ok();
    }
}

public class ApprovePoiRequest
{
    public bool Approve { get; set; }
}