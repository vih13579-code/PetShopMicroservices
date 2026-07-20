using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class ShopRegistrationController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var result = await api.GetAsync<IReadOnlyCollection<ShopRequestVm>>("api/shop-requests/mine");
        ViewBag.Error = result.Error; return View(result.Data ?? []);
    }
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (api.IsInRole("ShopOwner"))
        {
            TempData["Error"] = "Tài khoản của bạn đã sở hữu cửa hàng.";
            return RedirectToAction("Index");
        }
        var existing = await api.GetAsync<IReadOnlyCollection<ShopRequestVm>>("api/shop-requests/mine");
        if (existing.Data?.Any(x => x.Status == "Pending") == true)
        {
            TempData["Error"] = "Bạn đang có yêu cầu mở Shop chờ duyệt. Không thể gửi thêm yêu cầu mới.";
            return RedirectToAction("Index");
        }
        return View(new ShopRequestFormVm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ShopRequestFormVm model)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<ShopRequestVm>("api/shop-requests", model);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error ?? "Không thể gửi yêu cầu."); return View(model); }
        return RedirectToAction("Index");
    }
}
