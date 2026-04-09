using Microsoft.AspNetCore.Mvc;
using VinhKhanhCMS.Data;
using VinhKhanhCMS.Models;
using VinhKhanhCMS.Services;

namespace VinhKhanhCMS.Controllers;

[ApiController]
[Route("api/pois/{poiId}/translations")]
public class TranslationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TtsService _tts;
    private readonly IConfiguration _config;

    public TranslationController(AppDbContext context, TtsService tts, IConfiguration config)
    {
        _context = context;
        _tts = tts;
        _config = config;
    }

    // ✅ Lấy base URL từ env var API_BASE_URL (Render) hoặc fallback về config/request
    private string GetBaseUrl() =>
        Environment.GetEnvironmentVariable("API_BASE_URL")
        ?? _config["ApiBaseUrl"]
        ?? $"{Request.Scheme}://{Request.Host}";

    [HttpPut("{id}")]
    public IActionResult Update(int poiId, int id, PoiTranslation updated)
    {
        var trans = _context.PoiTranslations
            .FirstOrDefault(x => x.Id == id && x.PoiId == poiId);

        if (trans == null) return NotFound();

        trans.Title = updated.Title;
        trans.Description = updated.Description;
        trans.AudioUrl = updated.AudioUrl;
        trans.LanguageCode = updated.LanguageCode;

        _context.SaveChanges();

        return Ok(trans);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int poiId, int id)
    {
        var trans = _context.PoiTranslations
            .FirstOrDefault(x => x.Id == id && x.PoiId == poiId);

        if (trans == null) return NotFound();

        _context.PoiTranslations.Remove(trans);
        _context.SaveChanges();

        return Ok();
    }

    [HttpGet]
    public IActionResult Get(int poiId)
    {
        var list = _context.PoiTranslations
            .Where(x => x.PoiId == poiId)
            .ToList();

        return Ok(list);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(int poiId, GenerateTranslationRequest req)
    {
        var poi = _context.Pois.Find(poiId);
        if (poi == null) return NotFound("POI not found");

        // 🔥 XÓA TOÀN BỘ TRANSLATION CŨ CỦA POI
        var old = _context.PoiTranslations.Where(x => x.PoiId == poiId);
        _context.PoiTranslations.RemoveRange(old);
        await _context.SaveChangesAsync();

        // ✅ Dùng URL động thay vì hardcode IP
        var baseUrl = GetBaseUrl();

        var langs = new[]
        {
            "vi","en","es","fr","de",
            "zh","ja","ko","ru","it",
            "pt","hi"
        };

        var result = new List<PoiTranslation>();

        foreach (var lang in langs)
        {
            var translatedTitle = lang == "vi"
                ? req.Title
                : await RealTranslate(req.Title, lang);

            var translatedDesc = lang == "vi"
                ? req.Description
                : await RealTranslate(req.Description, lang);

            var text = $"{translatedTitle}. {translatedDesc}";
            var audioBytes = await _tts.GenerateAudio(text);

            var fileName = Guid.NewGuid() + ".mp3";
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/audio");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, audioBytes);

            result.Add(new PoiTranslation
            {
                PoiId = poiId,
                LanguageCode = lang,
                Title = translatedTitle,
                Description = translatedDesc,
                AudioUrl = baseUrl + "/audio/" + fileName
            });
        }

        _context.PoiTranslations.AddRange(result);
        await _context.SaveChangesAsync();

        return Ok(result);
    }

    [HttpDelete("delete-all")]
    public IActionResult DeleteAll(int poiId)
    {
        var translations = _context.PoiTranslations
            .Where(x => x.PoiId == poiId)
            .ToList();

        if (!translations.Any())
            return NotFound("Không có dữ liệu để xóa");

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/audio");

        foreach (var item in translations)
        {
            if (!string.IsNullOrEmpty(item.AudioUrl))
            {
                var fileName = Path.GetFileName(item.AudioUrl);
                var filePath = Path.Combine(folderPath, fileName);

                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
        }

        _context.PoiTranslations.RemoveRange(translations);
        _context.SaveChanges();

        return Ok("Đã xóa toàn bộ translation + audio");
    }

    private async Task<string> RealTranslate(string text, string targetLang)
    {
        try
        {
            var client = new HttpClient();
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";
            var response = await client.GetStringAsync(url);
            var json = System.Text.Json.JsonDocument.Parse(response);
            var translated = json.RootElement[0][0][0].GetString();
            return translated ?? text;
        }
        catch
        {
            return text;
        }
    }
}