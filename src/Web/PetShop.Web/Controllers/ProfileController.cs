using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class ProfileController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var result = await api.GetAsync<UserVm>("api/profile");
        ViewBag.Error = result.Error;
        if (result.Data is null) return RedirectToAction("Index", "Dashboard");

        var model = new ProfileFormVm
        {
            Id = result.Data.Id,
            Email = result.Data.Email,
            FullName = result.Data.FullName,
            Phone = result.Data.Phone,
            Address = result.Data.Address,
            Roles = result.Data.Roles,
            CreatedAt = result.Data.CreatedAt
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileFormVm model)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);

        var result = await api.PutAsync<UserVm>("api/profile", new
        {
            fullName = model.FullName.Trim(),
            phone = model.Phone?.Trim(),
            address = model.Address?.Trim()
        });

        if (result.Success && result.Data is not null)
        {
            TempData["Success"] = "Đã cập nhật thông tin cá nhân thành công.";
            // Update session current user
            api.SetToken(
                HttpContext.Session.GetString("AccessToken") ?? "",
                HttpContext.Session.GetString("RefreshToken") ?? "",
                System.Text.Json.JsonSerializer.Serialize(result.Data)
            );
            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật thông tin cá nhân.");
        return View(model);
    }
}
