using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class OwnerOrdersController(GatewayApiClient api) : Controller
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
        var r = await api.GetAsync<PagedVm<OrderVm>>("api/orders/owner?page=1&pageSize=100");
        ViewBag.Error = r.Error;
        return View(r.Data?.Items ?? []);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.GetAsync<OrderVm>($"api/orders/{id}");
        if (!r.Success || r.Data is null) return NotFound();

        var paymentRes = await api.GetAsync<PaymentVm>($"api/payments/order/{id}");
        ViewBag.Payment = paymentRes.Data;
        return View(r.Data);
    }

    [HttpPost] public Task<IActionResult> Confirm(Guid id) => Move(id, "confirm");
    [HttpPost] public Task<IActionResult> Preparing(Guid id) => Move(id, "preparing");
    [HttpPost] public Task<IActionResult> Shipping(Guid id) => Move(id, "shipping");
    [HttpPost] public Task<IActionResult> Complete(Guid id) => Move(id, "complete");
    [HttpPost] public Task<IActionResult> Cancel(Guid id) => Move(id, "cancel");

    [HttpPost]
    public async Task<IActionResult> ConfirmCodPayment(Guid orderId)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var paymentRes = await api.GetAsync<PaymentVm>($"api/payments/order/{orderId}");
        if (!paymentRes.Success || paymentRes.Data is null)
        {
            TempData["Error"] = paymentRes.Error ?? "Không tìm thấy thông tin thanh toán.";
            return RedirectToAction("Index");
        }
        var r = await api.PostAsync<object>($"api/payments/{paymentRes.Data.Id}/confirm-cod");
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã xác nhận thanh toán COD thành công." : r.Error;
        return RedirectToAction("Index");
    }

    private async Task<IActionResult> Move(Guid id, string action)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.PostAsync<object>($"api/orders/owner/{id}/{action}", new { note = $"Cập nhật từ MVC: {action}" });
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã cập nhật đơn." : r.Error;
        return RedirectToAction("Index");
    }
}
