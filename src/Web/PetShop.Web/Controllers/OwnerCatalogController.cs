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
<<<<<<< HEAD
        ViewBag.Categories = categories.Data ?? [];
        var inventory = await api.GetAsync<IReadOnlyCollection<InventoryVm>>("api/inventory/owner");
        ViewBag.Inventory = inventory.Data ?? [];
        ViewBag.Keyword = keyword;
        ViewBag.CategoryId = categoryId;
        ViewBag.IsActive = isActive;

        var result = await api.GetAsync<PagedVm<ProductVm>>(url);
        ViewBag.Error = result.Error;
        return View(result.Data?.Items ?? []);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStock(Guid productId, int quantity, string? reason)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        if (productId == Guid.Empty || quantity < 0 || quantity > 99999)
        {
            TempData["Error"] = "Sản phẩm hoặc số lượng tồn kho không hợp lệ.";
            return RedirectToAction("Products");
        }
        var result = await api.PutAsync<InventoryVm>("api/inventory/owner/set", new
        {
            productId, quantity,
            reason = string.IsNullOrWhiteSpace(reason) ? "Chủ Shop cập nhật từ danh sách sản phẩm" : reason.Trim()
        });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? $"Đã cập nhật tồn kho thành {quantity}." : result.Error;
        return RedirectToAction("Products");
    }

=======
        ViewBag.Error = products.Error ?? categories.Error;
        return View(new OwnerProductsPageVm(products.Data ?? new([], 1, 12, 0, 0), categories.Data ?? [], keyword, categoryId, isActive));
    }

    [HttpGet]
>>>>>>> origin/ThinhNPCE170008_Features_CatalogManagement
    public async Task<IActionResult> ProductDetails(Guid id)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var product = await api.GetAsync<ProductVm>($"api/owner/catalog/products/{id}");
        if (!product.Success || product.Data is null)
        {
            TempData["Error"] = product.Error ?? "Không tìm thấy sản phẩm trong Shop của bạn.";
            return RedirectToAction(nameof(Products));
        }

        var reviews = await api.GetAsync<IReadOnlyCollection<ReviewVm>>($"api/catalog/products/{id}/reviews");
        ViewBag.Reviews = reviews.Data ?? [];
        ViewBag.ReviewsError = reviews.Success ? null : reviews.Error;
        return View(product.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ProductForm(Guid? id)
    {
<<<<<<< HEAD
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Categories = categories.Data ?? [];
        ViewBag.CategoryError = categories.Success ? null : categories.Error;
        return View(new ProductFormVm());
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(ProductFormVm model)
    {
        var guard = CheckShopOwnerRole(); if (guard != null) return guard;
        var categories = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Categories = categories.Data ?? [];
        ViewBag.CategoryError = categories.Success ? null : categories.Error;
        if (model.CategoryId == Guid.Empty)
            ModelState.AddModelError(nameof(model.CategoryId), "Vui lòng tạo và chọn một danh mục trước khi tạo sản phẩm.");
        if (!ModelState.IsValid) return View(model);

        var enteredVariants = model.Variants?.Where(v => !string.IsNullOrWhiteSpace(v.Name) || !string.IsNullOrWhiteSpace(v.Sku)).ToList() ?? [];
        var variantsPayload = enteredVariants.Count > 0
            ? enteredVariants.Select(v => new { name = string.IsNullOrWhiteSpace(v.Name) ? "Mặc định" : v.Name.Trim(), sku = string.IsNullOrWhiteSpace(v.Sku) ? $"SKU-{Guid.NewGuid():N}"[..20] : v.Sku.Trim(), additionalPrice = v.AdditionalPrice, isActive = true }).Cast<object>().ToArray()
            : new object[] { new { name = "Mặc định", sku = $"SKU-{Guid.NewGuid():N}"[..20], additionalPrice = 0m, isActive = true } };

        var r = await api.PostAsync<ProductVm>("api/owner/catalog/products", new { model.CategoryId, model.Name, model.Description, model.Price, model.ImageUrl, model.IsActive, variants = variantsPayload });
        if (!r.Success) { ModelState.AddModelError(string.Empty, r.Error ?? "Không thể tạo sản phẩm."); return View(model); }
        if (r.Data is not null)
        {
            var stock = await api.PutAsync<InventoryVm>("api/inventory/owner/set", new
            {
                productId = r.Data.Id,
                quantity = model.InitialQuantity,
                reason = "Thiết lập tồn kho khi tạo sản phẩm"
            });
            if (!stock.Success)
            {
                TempData["Error"] = $"Sản phẩm đã được tạo nhưng chưa cập nhật được tồn kho: {stock.Error}";
                return RedirectToAction("Index", "Inventory");
            }
        }
        TempData["Success"] = $"Đã tạo sản phẩm mới với số lượng ban đầu là {model.InitialQuantity}.";
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
=======
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var model = new ProductFormVm();
        if (id.HasValue)
>>>>>>> origin/ThinhNPCE170008_Features_CatalogManagement
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
<<<<<<< HEAD

        var variantsPayload = model.Variants != null && model.Variants.Count > 0
            ? model.Variants.Select(v => new { name = v.Name?.Trim() ?? "Mặc định", sku = v.Sku?.Trim() ?? $"SKU-{Guid.NewGuid():N}"[..20], additionalPrice = v.AdditionalPrice, isActive = v.IsActive }).Cast<object>().ToArray()
            : new object[] { new { name = "Mặc định", sku = $"SKU-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}", additionalPrice = 0m, isActive = true } };

        var r = await api.PutAsync<ProductVm>($"api/owner/catalog/products/{id}", new { model.CategoryId, model.Name, model.Description, model.Price, model.ImageUrl, model.IsActive, variants = variantsPayload });
        if (!r.Success) { ModelState.AddModelError(string.Empty, r.Error ?? "Không thể cập nhật sản phẩm."); return View(model); }
        TempData["Success"] = "Đã cập nhật sản phẩm.";
        return RedirectToAction("Products");
=======
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
>>>>>>> origin/ThinhNPCE170008_Features_CatalogManagement
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
