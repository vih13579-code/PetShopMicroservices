using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class CatalogController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? keyword, Guid? shopId, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice, int page = 1)
    {
        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            ModelState.AddModelError(string.Empty, "Giá thấp nhất không được lớn hơn giá cao nhất.");
        var url = $"api/catalog/products?page={Math.Max(1, page)}&pageSize=12";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (shopId.HasValue) url += $"&shopId={shopId}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId}";
        if (minPrice.HasValue) url += $"&minPrice={minPrice.Value}";
        if (maxPrice.HasValue) url += $"&maxPrice={maxPrice.Value}";
        var products = await api.GetAsync<PagedVm<ProductVm>>(url);
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/catalog/categories");
        var shops = await api.GetAsync<IReadOnlyCollection<PublicShopVm>>("api/shops/public");
        ViewBag.Error = products.Error ?? categories.Error ?? shops.Error;
        return View(new CatalogPageVm(products.Data ?? new([], 1, 12, 0, 0), categories.Data ?? [], shops.Data ?? [],
            keyword, shopId, categoryId, minPrice, maxPrice));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var product = await api.GetAsync<ProductVm>($"api/catalog/products/{id}");
        if (!product.Success || product.Data is null) return NotFound();
        var reviews = await api.GetAsync<IReadOnlyCollection<ReviewVm>>($"api/catalog/products/{id}/reviews");
        ViewBag.ReviewError = reviews.Error;
        return View(new ProductDetailsPageVm(product.Data, reviews.Data ?? [], new ReviewFormVm()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReview(Guid productId, ReviewFormVm reviewForm)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id = productId });
        }
        var result = await api.PostAsync<ReviewVm>($"api/catalog/products/{productId}/reviews", reviewForm);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Cảm ơn bạn đã đánh giá sản phẩm." : result.Error;
        return RedirectToAction(nameof(Details), new { id = productId });
    }
}
