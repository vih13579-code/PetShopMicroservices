using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class AccountController(GatewayApiClient api) : Controller
{
    [HttpGet] public IActionResult Login() => View(new LoginVm());
    [HttpPost]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<TokenVm>("api/auth/login", model);
        if (!result.Success || result.Data is null) { ModelState.AddModelError(string.Empty, result.Error ?? "Đăng nhập thất bại."); return View(model); }
        api.SetToken(result.Data.AccessToken, result.Data.RefreshToken, JsonSerializer.Serialize(result.Data.User));
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet] public IActionResult Register() => View(new RegisterVm());
    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<TokenVm>("api/auth/register", model);
        if (!result.Success || result.Data is null) { ModelState.AddModelError(string.Empty, result.Error ?? "Đăng ký thất bại."); return View(model); }
        api.SetToken(result.Data.AccessToken, result.Data.RefreshToken, JsonSerializer.Serialize(result.Data.User));
        return RedirectToAction("Index", "Dashboard");
    }

    public async Task<IActionResult> Logout()
    {
        var refresh = HttpContext.Session.GetString("RefreshToken");
        if (!string.IsNullOrWhiteSpace(refresh)) await api.PostAsync<object>("api/auth/logout", new { refreshToken = refresh });
        api.ClearToken(); return RedirectToAction("Index", "Home");
    }
}
