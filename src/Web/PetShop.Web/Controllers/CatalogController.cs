using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class CatalogController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? keyword, Guid? shopId, Guid? categoryId, decimal? minPrice, decimal? maxPrice, int page = 1)
    {
        var url = $"api/catalog/products?page={page}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (shopId.HasValue) url += $"&shopId={shopId}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId}";
        if (minPrice.HasValue) url += $"&minPrice={minPrice}";
        if (maxPrice.HasValue) url += $"&maxPrice={maxPrice}";

        var shops = await api.GetAsync<IReadOnlyCollection<ShopVm>>("api/shops/public");
        ViewBag.Shops = shops.Data ?? [];
        ViewBag.Keyword = keyword;
        ViewBag.ShopId = shopId;
        ViewBag.CategoryId = categoryId;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;

        var result = await api.GetAsync<PagedVm<ProductVm>>(url);
        ViewBag.Error = result.Error;
        return View(result.Data ?? new PagedVm<ProductVm>([], 1, 20, 0, 0));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var result = await api.GetAsync<ProductVm>($"api/catalog/products/{id}");
        if (!result.Success || result.Data is null) return NotFound();

        var inventory = await api.GetAsync<InventoryVm>($"api/inventory/availability/{id}");
        ViewBag.Inventory = inventory.Data;

        var reviews = await api.GetAsync<IReadOnlyCollection<ReviewVm>>($"api/catalog/products/{id}/reviews");
        ViewBag.Reviews = reviews.Data ?? [];
        return View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> AddReview(Guid productId, int rating, string? comment)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var r = await api.PostAsync<ReviewVm>($"api/catalog/products/{productId}/reviews", new { rating, comment });

        if (!r.Success && (r.StatusCode == 409 || (r.Error != null && r.Error.Contains("đã đánh giá"))))
        {
            r = await api.PutAsync<ReviewVm>($"api/catalog/products/{productId}/reviews/mine", new { rating, comment });
        }

        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã lưu đánh giá sản phẩm thành công!" : r.Error;
        return RedirectToAction("Details", new { id = productId });
    }
}
