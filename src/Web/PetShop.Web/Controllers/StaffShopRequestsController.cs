using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class StaffShopRequestsController(GatewayApiClient api) : Controller
{
    private IActionResult? CheckStaffRole()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!api.HasAnyRole("Staff", "Admin"))
        {
            TempData["Error"] = "Bạn không có quyền truy cập vào nghiệp vụ Staff.";
            return RedirectToAction("Index", "Dashboard");
        }
        return null;
    }

    public async Task<IActionResult> Index(string? keyword, string? status)
    {
        var guard = CheckStaffRole(); if (guard != null) return guard;
        var url = "api/shop-requests?page=1&pageSize=100";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";

        var result = await api.GetAsync<PagedVm<ShopRequestVm>>(url);
        ViewBag.Error = result.Error;
        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        return View(result.Data?.Items ?? []);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var guard = CheckStaffRole(); if (guard != null) return guard;
        var result = await api.GetAsync<ShopRequestVm>($"api/shop-requests/{id}");
        return !result.Success || result.Data is null ? NotFound() : View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid id)
    {
        var guard = CheckStaffRole(); if (guard != null) return guard;
        var r = await api.PostAsync<ShopVm>($"api/shop-requests/{id}/approve");
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã duyệt mở Shop thành công." : r.Error;
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        var guard = CheckStaffRole(); if (guard != null) return guard;
        var r = await api.PostAsync<object>($"api/shop-requests/{id}/reject", new { reason });
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã từ chối yêu cầu mở Shop." : r.Error;
        return RedirectToAction("Index");
    }
}
