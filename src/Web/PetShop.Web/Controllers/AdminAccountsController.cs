using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class AdminAccountsController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? keyword)
    {
        var url = "api/admin/accounts?page=1&pageSize=100" + (!string.IsNullOrWhiteSpace(keyword) ? $"&keyword={Uri.EscapeDataString(keyword)}" : "");
        var result = await api.GetAsync<PagedVm<UserVm>>(url); ViewBag.Error = result.Error; ViewBag.Keyword = keyword;
        return View(result.Data?.Items ?? []);
    }
    [HttpGet]
    public IActionResult CreateStaff() => View(new StaffFormVm());

    [HttpPost]
    public async Task<IActionResult> CreateStaff(StaffFormVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<UserVm>("api/admin/accounts/staff", model);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo Staff."); return View(model); }
        TempData["Success"] = "Đã tạo tài khoản Staff.";
        return RedirectToAction("Index");
    }

    [HttpPost] public async Task<IActionResult> Lock(Guid id) { await api.PatchAsync<object>($"api/admin/accounts/{id}/lock"); return RedirectToAction("Index"); }
    [HttpPost] public async Task<IActionResult> Unlock(Guid id) { await api.PatchAsync<object>($"api/admin/accounts/{id}/unlock"); return RedirectToAction("Index"); }
}
