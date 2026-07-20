using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class OwnerCatalogController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Categories()
    {
        var result = await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories");
        ViewBag.Error = result.Error; return View(result.Data ?? []);
    }
    [HttpPost] public async Task<IActionResult> CreateCategory(CategoryFormVm model) { var r=await api.PostAsync<CategoryVm>("api/owner/catalog/categories",model); TempData[r.Success?"Success":"Error"]=r.Success?"Đã tạo danh mục.":r.Error; return RedirectToAction("Categories"); }
    [HttpPost] public async Task<IActionResult> DeleteCategory(Guid id) { var r=await api.DeleteAsync<object>($"api/owner/catalog/categories/{id}"); TempData[r.Success?"Success":"Error"]=r.Success?"Đã xóa danh mục.":r.Error; return RedirectToAction("Categories"); }
    public async Task<IActionResult> Products()
    {
        var result = await api.GetAsync<PagedVm<ProductVm>>("api/owner/catalog/products?page=1&pageSize=100");
        ViewBag.Error = result.Error; return View(result.Data?.Items ?? []);
    }
    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        var categories=await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories"); ViewBag.Categories=categories.Data??[]; return View(new ProductFormVm());
    }
    [HttpPost]
    public async Task<IActionResult> CreateProduct(ProductFormVm model)
    {
        var categories=await api.GetAsync<IReadOnlyCollection<CategoryVm>>("api/owner/catalog/categories"); ViewBag.Categories=categories.Data??[];
        if(!ModelState.IsValid)return View(model);
        var r=await api.PostAsync<ProductVm>("api/owner/catalog/products",new{model.CategoryId,model.Name,model.Description,model.Price,model.ImageUrl,model.IsActive,variants=Array.Empty<object>()});
        if(!r.Success){ModelState.AddModelError(string.Empty,r.Error??"Không thể tạo sản phẩm.");return View(model);} return RedirectToAction("Products");
    }
    [HttpPost] public async Task<IActionResult> DeleteProduct(Guid id) { await api.DeleteAsync<object>($"api/owner/catalog/products/{id}"); return RedirectToAction("Products"); }
}
