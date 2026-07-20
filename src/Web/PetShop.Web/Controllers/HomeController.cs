using Microsoft.AspNetCore.Mvc;

namespace PetShop.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Error() => View();
}
