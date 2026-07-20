using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class CatalogController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? keyword, Guid? shopId, Guid? categoryId, int page = 1)
    {
        var url = $"api/catalog/products?page={page}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (shopId.HasValue) url += $"&shopId={shopId}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId}";
        var result = await api.GetAsync<PagedVm<ProductVm>>(url);
        ViewBag.Error = result.Error; ViewBag.Keyword = keyword;
        return View(result.Data ?? new PagedVm<ProductVm>([], 1, 20, 0, 0));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var result = await api.GetAsync<ProductVm>($"api/catalog/products/{id}");
        return !result.Success || result.Data is null ? NotFound() : View(result.Data);
    }
}
