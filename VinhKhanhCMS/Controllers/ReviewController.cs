using Microsoft.AspNetCore.Mvc;
using VinhKhanhCMS.Data;
using VinhKhanhCMS.Models;

namespace VinhKhanhCMS.Controllers;

[ApiController]
[Route("api/pois/{poiId}/reviews")]
public class ReviewController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewController(AppDbContext context) => _context = context;

    // GET /api/pois/{poiId}/reviews
    [HttpGet]
    public IActionResult GetByPoi(int poiId)
    {
        var reviews = _context.Reviews
            .Where(r => r.PoiId == poiId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Ok(reviews);
    }

    // POST /api/pois/{poiId}/reviews
    [HttpPost]
    public async Task<IActionResult> Create(int poiId, [FromBody] Review review)
    {
        review.PoiId = poiId;
        review.CreatedAt = DateTime.UtcNow;
        if (review.Rating < 1) review.Rating = 1;
        if (review.Rating > 5) review.Rating = 5;

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return Ok(review);
    }

    // DELETE /api/pois/{poiId}/reviews/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int poiId, int id)
    {
        var review = _context.Reviews.FirstOrDefault(r => r.Id == id && r.PoiId == poiId);
        if (review == null) return NotFound();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
