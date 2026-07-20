using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class PaymentsController(GatewayApiClient api) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid orderId, string method)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var r = await api.PostAsync<PaymentVm>("api/payments", new { orderId, method });
        if (!r.Success || r.Data is null) { TempData["Error"] = r.Error; return RedirectToAction("Details", "Orders", new { id = orderId }); }
        if (r.Data.Status == "Pending" && r.Data.Method == "BankTransferMock")
            return RedirectToAction("Confirm", new { id = r.Data.Id, amount = r.Data.Amount, code = r.Data.TransactionCode });
        TempData["Success"] = "Đã tạo thông tin thanh toán."; return RedirectToAction("Details", "Orders", new { id = orderId });
    }

    [HttpGet]
    public IActionResult Confirm(Guid id, decimal amount, string? code)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        ViewBag.PaymentId = id;
        ViewBag.Amount = amount;
        ViewBag.Code = code;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmPayment(Guid id)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var r = await api.PostAsync<PaymentVm>($"api/payments/{id}/confirm-mock");
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Thanh toán mô phỏng thành công." : r.Error;
        return RedirectToAction("Index", "Orders");
    }
}
