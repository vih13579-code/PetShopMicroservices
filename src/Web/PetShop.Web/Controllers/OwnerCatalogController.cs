using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class OwnerCatalogController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Categories(Guid? editId)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var result = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        var items = result.Data ?? [];
        var editing = editId.HasValue ? items.SingleOrDefault(x => x.Id == editId.Value) : null;
        var form = editing is null
            ? new CategoryFormVm()
            : new CategoryFormVm { Id = editing.Id, Name = editing.Name, Description = editing.Description, IsActive = editing.IsActive };
        ViewBag.Error = result.Error;
        return View(new OwnerCategoriesPageVm(items, form));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory([Bind(Prefix = "Form")] CategoryFormVm model)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return await CategoriesWithForm(model);
        var result = model.Id == Guid.Empty
            ? await api.PostAsync<CategoryVm>("api/owner/catalog/categories", model)
            : await api.PutAsync<CategoryVm>($"api/owner/catalog/categories/{model.Id}", model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể lưu danh mục.");
            return await CategoriesWithForm(model);
        }
        TempData["Success"] = model.Id == Guid.Empty ? "Đã tạo danh mục." : "Đã cập nhật danh mục.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var result = await api.DeleteAsync<object>($"api/owner/catalog/categories/{id}");
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Đã xóa danh mục." : result.Error;
        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> Products(string? keyword, Guid? categoryId, bool? isActive, int page = 1)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var url = $"api/owner/catalog/products?page={Math.Max(1, page)}&pageSize=12";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId}";
        if (isActive.HasValue) url += $"&isActive={isActive.Value.ToString().ToLowerInvariant()}";
        var products = await api.GetAsync<PagedVm<ProductVm>>(url);
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Error = products.Error ?? categories.Error;
        return View(new OwnerProductsPageVm(products.Data ?? new([], 1, 12, 0, 0), categories.Data ?? [], keyword, categoryId, isActive));
    }

    [HttpGet]
    public async Task<IActionResult> ProductForm(Guid? id)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var model = new ProductFormVm();
        if (id.HasValue)
        {
            var product = await api.GetAsync<ProductVm>($"api/owner/catalog/products/{id}");
            if (!product.Success || product.Data is null) return NotFound();
            model = MapForm(product.Data);
        }
        var categoriesLoaded = await LoadCategories();
        ViewBag.CanSave = categoriesLoaded;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductForm(ProductFormVm model)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var categoriesLoaded = await LoadCategories();
        ViewBag.CanSave = categoriesLoaded;
        if (!categoriesLoaded)
            ModelState.AddModelError(string.Empty, "Không thể tải danh mục. Sản phẩm chỉ có thể lưu sau khi danh mục được tải thành công.");
        if (!ModelState.IsValid) return View(model);
        var result = model.Id == Guid.Empty
            ? await api.PostAsync<ProductVm>("api/owner/catalog/products", model)
            : await api.PutAsync<ProductVm>($"api/owner/catalog/products/{model.Id}", model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể lưu sản phẩm.");
            return View(model);
        }
        TempData["Success"] = model.Id == Guid.Empty ? "Đã tạo sản phẩm." : "Đã cập nhật sản phẩm.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var result = await api.DeleteAsync<object>($"api/owner/catalog/products/{id}");
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Sản phẩm đã ngừng bán." : result.Error;
        return RedirectToAction(nameof(Products));
    }

    private async Task<IActionResult> CategoriesWithForm(CategoryFormVm form)
    {
        var result = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Error = result.Error;
        return View("Categories", new OwnerCategoriesPageVm(result.Data ?? [], form));
    }

    private async Task<bool> LoadCategories()
    {
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Categories = categories.Data ?? [];
        ViewBag.CategoriesError = categories.Error;
        return categories.Success && categories.Data is { Count: > 0 };
    }

    private static ProductFormVm MapForm(ProductVm product) => new()
    {
        Id = product.Id, CategoryId = product.CategoryId, Name = product.Name, Description = product.Description,
        Price = product.Price, ImageUrl = product.ImageUrl, IsActive = product.IsActive,
        Variants = product.Variants.Select(x => new VariantFormVm
        {
            Id = x.Id, Name = x.Name, Sku = x.Sku, AdditionalPrice = x.AdditionalPrice, IsActive = x.IsActive
        }).ToList()
    };
}
