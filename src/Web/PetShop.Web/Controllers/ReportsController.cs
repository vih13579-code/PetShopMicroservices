using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class ReportsController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Shop(DateTime? from, DateTime? to)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!api.HasAnyRole("ShopOwner", "Admin"))
        {
            TempData["Error"] = "Bạn không có quyền xem báo cáo doanh thu Shop.";
            return RedirectToAction("Index", "Dashboard");
        }

        var fromStr = from?.ToString("yyyy-MM-dd");
        var toStr = to?.ToString("yyyy-MM-dd");
        var url = $"api/orders/reports/owner-revenue?from={fromStr}&to={toStr}";
        var result = await api.GetAsync<RevenueReportVm>(url);
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Error = result.Error;
        return View(result.Data ?? new RevenueReportVm(0, 0, 0, 0, from, to));
    }

    public async Task<IActionResult> Admin(DateTime? from, DateTime? to)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!api.IsInRole("Admin"))
        {
            TempData["Error"] = "Bạn không có quyền xem báo cáo hệ thống.";
            return RedirectToAction("Index", "Dashboard");
        }

        var fromStr = from?.ToString("yyyy-MM-dd");
        var toStr = to?.ToString("yyyy-MM-dd");
        var url = $"api/orders/reports/system-revenue?from={fromStr}&to={toStr}";
        var result = await api.GetAsync<RevenueReportVm>(url);
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Error = result.Error;
        return View(result.Data ?? new RevenueReportVm(0, 0, 0, 0, from, to));
    }
}
