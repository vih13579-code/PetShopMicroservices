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
        if (!result.Success || result.Data is null) return NotFound();
        var reviews = await api.GetAsync<IReadOnlyCollection<ReviewVm>>($"api/catalog/products/{id}/reviews");
        return View(new ProductDetailsVm { Product = result.Data, Reviews = reviews.Data ?? [] });
    }

    [HttpPost]
    public async Task<IActionResult> Review(ReviewFormVm model)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Vui lòng chọn từ 1 đến 5 sao."; return RedirectToAction("Details", new { id = model.ProductId }); }
        var result = await api.PostAsync<ReviewVm>($"api/catalog/products/{model.ProductId}/reviews", new { model.Rating, model.Comment });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Cảm ơn bạn đã đánh giá sản phẩm." : result.Error;
        return RedirectToAction("Details", new { id = model.ProductId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateReview(ReviewFormVm model)
    {
        var result = await api.PutAsync<ReviewVm>($"api/catalog/products/{model.ProductId}/reviews/mine", new { model.Rating, model.Comment });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Đã cập nhật đánh giá." : result.Error;
        return RedirectToAction("Details", new { id = model.ProductId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteReview(Guid productId)
    {
        var result = await api.DeleteAsync<object>($"api/catalog/products/{productId}/reviews/mine");
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Đã xóa đánh giá." : result.Error;
        return RedirectToAction("Details", new { id = productId });
    }
}
