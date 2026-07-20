using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class InventoryController(GatewayApiClient api) : Controller
{
    private IActionResult? CheckShopOwnerRole()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!api.HasAnyRole("ShopOwner", "Admin"))
        {
            TempData["Error"] = "Bạn không có quyền truy cập vào kênh Chủ Shop.";
            return RedirectToAction("Index", "Dashboard");
        }
        return null;
    }

    public async Task<IActionResult> Index()
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var products = await api.GetAsync<PagedVm<ProductVm>>("api/owner/catalog/products?page=1&pageSize=100");
        ViewBag.Products = products.Data?.Items ?? [];

        var result = await api.GetAsync<IReadOnlyCollection<InventoryVm>>("api/inventory/owner");
        ViewBag.Error = result.Error;
        return View(result.Data ?? []);
    }

    [HttpPost]
    public async Task<IActionResult> Set(Guid productId, int quantity, string? reason)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.PutAsync<InventoryVm>("api/inventory/owner/set", new { productId, quantity, reason });
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã cập nhật số lượng tồn kho thành công." : r.Error;
        return RedirectToAction("Index");
    }
}
