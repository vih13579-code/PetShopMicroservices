using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class ShopsController(GatewayApiClient api) : Controller
{
    // Public Action: Danh sách các Shop công khai cho Guest / Customer
    public async Task<IActionResult> Index()
    {
        var r = await api.GetAsync<IReadOnlyCollection<ShopVm>>("api/shops/public");
        ViewBag.Error = r.Error;
        return View(r.Data ?? []);
    }

    private IActionResult? CheckStaffOrAdminRole()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!api.HasAnyRole("Staff", "Admin"))
        {
            TempData["Error"] = "Bạn không có quyền truy cập vào phần Quản lý Shop này.";
            return RedirectToAction("Index", "Dashboard");
        }
        return null;
    }

    private IActionResult? CheckShopOwnerRole()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!api.HasAnyRole("ShopOwner", "Admin"))
        {
            TempData["Error"] = "Bạn không có quyền truy cập vào thông tin Shop cá nhân.";
            return RedirectToAction("Index", "Dashboard");
        }
        return null;
    }

    public async Task<IActionResult> Manage(string? keyword, string? status)
    {
        var guard = CheckStaffOrAdminRole(); if (guard != null) return guard;
        var url = "api/shops?page=1&pageSize=100";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";

        var r = await api.GetAsync<PagedVm<ShopVm>>(url);
        ViewBag.Error = r.Error;
        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        return View(r.Data?.Items ?? []);
    }

    [HttpPost]
    public async Task<IActionResult> Lock(Guid id)
    {
        var guard = CheckStaffOrAdminRole(); if (guard != null) return guard;
        await api.PatchAsync<object>($"api/shops/{id}/lock");
        return RedirectToAction("Manage");
    }

    [HttpPost]
    public async Task<IActionResult> Unlock(Guid id)
    {
        var guard = CheckStaffOrAdminRole(); if (guard != null) return guard;
        await api.PatchAsync<object>($"api/shops/{id}/unlock");
        return RedirectToAction("Manage");
    }

    public async Task<IActionResult> Mine()
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.GetAsync<ShopVm>("api/shops/mine");
        ViewBag.Error = r.Error;
        if (r.Data is null) return View((ShopEditVm?)null);
        return View(new ShopEditVm { Id = r.Data.Id, OwnerUserId = r.Data.OwnerUserId, Name = r.Data.Name, Description = r.Data.Description, Phone = r.Data.Phone, Email = r.Data.Email, Address = r.Data.Address, TaxCode = r.Data.TaxCode });
    }

    [HttpPost]
    public async Task<IActionResult> Mine(ShopEditVm model)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.PutAsync<ShopVm>("api/shops/mine", new { name = model.Name, model.Description, model.Phone, model.Email, model.Address, model.TaxCode });
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã cập nhật Shop." : r.Error;
        return RedirectToAction("Mine");
    }
}
