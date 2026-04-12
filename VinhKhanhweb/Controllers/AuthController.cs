using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using VinhKhanhadmin.Models;

public class AuthController : Controller
{
    private readonly HttpClient _http;

    public AuthController(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("CmsApi");
    }

    // ─────────────────────────────────────────
    // GET / (Index Redirect)
    // ─────────────────────────────────────────
    public IActionResult Index() => RedirectToAction("Login");

    // ─────────────────────────────────────────
    // GET /Login
    // ─────────────────────────────────────────
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("UserId") != null)
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Owner" ? RedirectToAction("Index", "Owner")
                                   : RedirectToAction("Index", "Admin");
        }
        return View();
    }

    // ─────────────────────────────────────────
    // POST /Login
    // ─────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", new { username, password });

            if (!res.IsSuccessStatusCode)
            {
                var errorText = await res.Content.ReadAsStringAsync();
                
                // Nếu backend ném Text thay vì JSON
                ViewBag.Error = errorText;

                // Nếu backend trả JSON có "detail"
                try {
                     var problem = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonObject>(errorText);
                     if (problem != null && problem.ContainsKey("detail")) {
                         ViewBag.Error = problem["detail"]?.ToString();
                     } else if (problem != null && problem.ContainsKey("message")) {
                         ViewBag.Error = problem["message"]?.ToString();
                     }
                } catch { }

                if (string.IsNullOrWhiteSpace(ViewBag.Error)) 
                {
                    ViewBag.Error = "Backend trả về lỗi: " + res.StatusCode;
                }

                return View();
            }

            var user = await res.Content.ReadFromJsonAsync<AppUser>();
            if (user == null) { ViewBag.Error = "Lỗi hệ thống Cms Api"; return View(); }


        // Lưu thông tin vào Session
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Role", user.Role);

        return user.Role == "Owner"
            ? RedirectToAction("Index", "Owner")
            : RedirectToAction("Index", "Admin");
        }
        catch (Exception ex)
        {
            ViewBag.Error = "Lỗi kết nối Backend: " + ex.Message;
            return View();
        }
    }

    // ─────────────────────────────────────────
    // GET /Register  (Đăng ký tài khoản Owner)
    // ─────────────────────────────────────────
    public IActionResult Register() => View();

    // ─────────────────────────────────────────
    // POST /Register
    // ─────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Register(
        string username, string password, string email,
        string fullName, string phone, string businessName)
    {
        var payload = new { username, password, email, fullName, phone, businessName };
        var res = await _http.PostAsJsonAsync("api/auth/register-owner", payload);

        if (res.IsSuccessStatusCode)
        {
            ViewBag.Success = "Đăng ký thành công! Vui lòng chờ Admin phê duyệt tài khoản.";
            return View();
        }

        ViewBag.Error = await res.Content.ReadAsStringAsync();
        return View();
    }

    // ─────────────────────────────────────────
    // GET /Logout
    // ─────────────────────────────────────────
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
