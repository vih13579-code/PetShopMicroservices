using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class HomeController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        try
        {
            var result = await api.GetAsync<PagedVm<ProductVm>>("api/catalog/products?page=1&pageSize=8");
            return View(result.Data?.Items ?? []);
        }
        catch (HttpRequestException)
        {
            // Home vẫn hiển thị được khi Gateway/Catalog tạm thời chưa khởi động.
            return View(Array.Empty<ProductVm>());
        }
        catch (TaskCanceledException)
        {
            return View(Array.Empty<ProductVm>());
        }
    }
    public IActionResult Error() => View();
}
