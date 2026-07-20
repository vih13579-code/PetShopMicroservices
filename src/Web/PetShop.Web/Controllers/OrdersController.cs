using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class OrdersController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var result = await api.GetAsync<PagedVm<OrderVm>>("api/orders/mine?page=1&pageSize=100");
        ViewBag.Error = result.Error; return View(result.Data?.Items ?? []);
    }
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await api.GetAsync<OrderVm>($"api/orders/{id}");
        return !result.Success || result.Data is null ? NotFound() : View(result.Data);
    }
    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await api.PostAsync<object>($"api/orders/{id}/cancel", new { note = "Hủy từ giao diện khách hàng." });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Đã hủy đơn." : result.Error;
        return RedirectToAction("Index");
    }
}
