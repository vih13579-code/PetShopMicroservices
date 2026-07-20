using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class OwnerOrdersController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var r = await api.GetAsync<PagedVm<OrderVm>>("api/orders/owner?page=1&pageSize=100");
        ViewBag.Error = r.Error; return View(r.Data?.Items ?? []);
    }
    [HttpPost] public Task<IActionResult> Confirm(Guid id) => Move(id, "confirm");
    [HttpPost] public Task<IActionResult> Preparing(Guid id) => Move(id, "preparing");
    [HttpPost] public Task<IActionResult> Shipping(Guid id) => Move(id, "shipping");
    [HttpPost] public Task<IActionResult> Complete(Guid id) => Move(id, "complete");
    [HttpPost] public Task<IActionResult> Cancel(Guid id) => Move(id, "cancel");
    private async Task<IActionResult> Move(Guid id, string action)
    {
        var r = await api.PostAsync<object>($"api/orders/owner/{id}/{action}", new { note = $"Cập nhật từ MVC: {action}" });
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã cập nhật đơn." : r.Error;
        return RedirectToAction("Index");
    }
}
