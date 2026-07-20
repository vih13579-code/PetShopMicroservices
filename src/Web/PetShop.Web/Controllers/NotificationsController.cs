using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class NotificationsController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<IReadOnlyCollection<NotificationVm>>("api/notifications?take=100");
        ViewBag.Error = result.Error; return View(result.Data ?? []);
    }
    [HttpPost] public async Task<IActionResult> Read(Guid id) { await api.PatchAsync<object>($"api/notifications/{id}/read"); return RedirectToAction("Index"); }
}
