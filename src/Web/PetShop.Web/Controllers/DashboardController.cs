using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class DashboardController(GatewayApiClient api) : Controller
{
    public IActionResult Index()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var user = JsonSerializer.Deserialize<UserVm>(api.CurrentUserJson ?? "{}", new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return View(user);
    }
}
