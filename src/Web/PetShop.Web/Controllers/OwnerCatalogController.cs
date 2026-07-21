using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class OwnerCatalogController(GatewayApiClient api, IWebHostEnvironment environment) : Controller
{
    private IActionResult? CheckShopOwnerRole()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!api.HasAnyRole("ShopOwner", "Admin"))
        {
            TempData["Error"] = "Bạn không có quyền truy cập vào kênh Chủ Shop.";
            return RedirectToAction("Index", "Dashboard");
        }
        return null;
    }

    public async Task<IActionResult> Categories()
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var result = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Error = result.Error;
        return View(result.Data ?? []);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CategoryFormVm model)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.PostAsync<CategoryVm>("api/owner/catalog/categories", model);
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã tạo danh mục." : r.Error;
        return RedirectToAction("Categories");
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(Guid id, CategoryFormVm model)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.PutAsync<CategoryVm>($"api/owner/catalog/categories/{id}", model);
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã cập nhật danh mục." : r.Error;
        return RedirectToAction("Categories");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.DeleteAsync<object>($"api/owner/catalog/categories/{id}");
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã xóa danh mục." : r.Error;
        return RedirectToAction("Categories");
    }

    public async Task<IActionResult> Products(string? keyword, Guid? categoryId, bool? isActive)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var url = "api/owner/catalog/products?page=1&pageSize=100";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId.Value}";
        if (isActive.HasValue) url += $"&isActive={isActive.Value}";

        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Categories = categories.Data ?? [];
        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.IsActive = isActive;

        var result = await api.GetAsync<PagedVm<ProductVm>>(url);
        ViewBag.Error = result.Error;
        return View(result.Data?.Items ?? []);
    }

    public async Task<IActionResult> ProductDetails(Guid id)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var result = await api.GetAsync<ProductVm>($"api/owner/catalog/products/{id}");
        if (!result.Success || result.Data is null) return NotFound();

        var reviews = await api.GetAsync<IReadOnlyCollection<ReviewVm>>($"api/catalog/products/{id}/reviews");
        ViewBag.Reviews = reviews.Data ?? [];
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Categories = categories.Data ?? [];
        return View(new ProductFormVm());
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(ProductFormVm model)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Categories = categories.Data ?? [];
        if (!ModelState.IsValid) return View(model);
        model.ImageUrl = await SaveImageAsync(model.ImageFile, model.ImageUrl);

        var variantsPayload = model.Variants != null && model.Variants.Count > 0
            ? model.Variants.Select(v => new { name = v.Name.Trim(), sku = v.Sku.Trim(), additionalPrice = v.AdditionalPrice, isActive = v.IsActive }).Cast<object>().ToArray()
            : new object[] { new { name = "Mặc định", sku = $"SKU-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}", additionalPrice = 0m, isActive = true } };

        var r = await api.PostAsync<ProductVm>("api/owner/catalog/products", new { model.CategoryId, model.Name, model.Description, model.Price, model.ImageUrl, model.IsActive, variants = variantsPayload });
        if (!r.Success) { ModelState.AddModelError(string.Empty, r.Error ?? "Không thể tạo sản phẩm."); return View(model); }
        TempData["Success"] = "Đã tạo sản phẩm mới.";
        return RedirectToAction("Products");
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(Guid id)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Categories = categories.Data ?? [];

        var result = await api.GetAsync<ProductVm>($"api/owner/catalog/products/{id}");
        if (!result.Success || result.Data is null) return NotFound();

        var model = new ProductFormVm
        {
            Id = result.Data.Id,
            CategoryId = result.Data.CategoryId,
            Name = result.Data.Name,
            Description = result.Data.Description,
            Price = result.Data.Price,
            ImageUrl = result.Data.ImageUrl,
            IsActive = result.Data.IsActive,
            Variants = result.Data.Variants.Select(v => new VariantFormVm
            {
                Name = v.Name,
                Sku = v.Sku,
                AdditionalPrice = v.AdditionalPrice,
                IsActive = v.IsActive
            }).ToList()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditProduct(Guid id, ProductFormVm model)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Categories = categories.Data ?? [];
        if (!ModelState.IsValid) return View(model);
        model.ImageUrl = await SaveImageAsync(model.ImageFile, model.ImageUrl);

        var variantsPayload = model.Variants != null && model.Variants.Count > 0
            ? model.Variants.Select(v => new { name = v.Name.Trim(), sku = v.Sku.Trim(), additionalPrice = v.AdditionalPrice, isActive = v.IsActive }).Cast<object>().ToArray()
            : new object[] { new { name = "Mặc định", sku = $"SKU-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}", additionalPrice = 0m, isActive = true } };

        var r = await api.PutAsync<ProductVm>($"api/owner/catalog/products/{id}", new { model.CategoryId, model.Name, model.Description, model.Price, model.ImageUrl, model.IsActive, variants = variantsPayload });
        if (!r.Success) { ModelState.AddModelError(string.Empty, r.Error ?? "Không thể cập nhật sản phẩm."); return View(model); }
        TempData["Success"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction("Products");
    }

    private async Task<string?> SaveImageAsync(IFormFile? file, string? existingUrl)
    {
        if (file is null || file.Length == 0) return existingUrl;
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(extension) || file.Length > 5 * 1024 * 1024) return existingUrl;
        var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, name));
        await file.CopyToAsync(stream);
        // Catalog API validates ImageUrl with [Url], so send an absolute URL
        // instead of the relative path /uploads/{name}.
        return $"{Request.Scheme}://{Request.Host}/uploads/{name}";
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var r = await api.DeleteAsync<object>($"api/owner/catalog/products/{id}");
        TempData[r.Success ? "Success" : "Error"] = r.Success ? "Đã ngừng bán sản phẩm." : r.Error;
        return RedirectToAction("Products");
    }
}
