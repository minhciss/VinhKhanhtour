using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VinhKhanhadmin.Models;

public class PublicController : Controller
{
    private readonly HttpClient _http;
    private readonly string _cmsBaseUrl;

    public PublicController(IHttpClientFactory factory)
    {
        // ✅ Dùng named HttpClient "CmsApi" — URL được config từ env var CMS_API_URL
        _http = factory.CreateClient("CmsApi");
        _cmsBaseUrl = Environment.GetEnvironmentVariable("CMS_API_URL") ?? "http://localhost:5137";
    }

    // Trang chủ web app — danh sách các điểm tham quan
    [HttpGet("/tour")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var pois = await _http.GetFromJsonAsync<List<Poi>>("api/pois?status=Approved") ?? new();
            ViewBag.CmsBaseUrl = _cmsBaseUrl;
            return View(pois);
        }
        catch
        {
            return View(new List<Poi>());
        }
    }

    public async Task<IActionResult> Poi(int id)
    {
        try 
        {
            var poi = await _http.GetFromJsonAsync<Poi>($"api/pois/{id}");
            var translations = await _http.GetFromJsonAsync<List<PoiTranslation>>(
                $"api/pois/{id}/translations");

            ViewBag.Translations = translations ?? new List<PoiTranslation>();
            ViewBag.CmsBaseUrl = _cmsBaseUrl;
                
            return View(poi);
        }
        catch (HttpRequestException ex)
        {
            return Content($"Hệ thống đang quá tải hoặc máy chủ API chưa sẵn sàng. Xin vui lòng thử lại sau ít phút.\nChi tiết lỗi kỹ thuật: {ex.StatusCode} - {ex.Message}");
        }
        catch (Exception ex)
        {
            return Content($"Có lỗi không mong muốn xảy ra: {ex.Message}");
        }
    }
}