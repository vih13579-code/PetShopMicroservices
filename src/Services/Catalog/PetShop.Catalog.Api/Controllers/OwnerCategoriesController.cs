using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Catalog.Api.Application;
using PetShop.Catalog.Api.Contracts;
using PetShop.Catalog.Api.Domain;
using PetShop.Catalog.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Catalog.Api.Controllers;

[ApiController]
[Authorize(Roles = "ShopOwner")]
[Route("api/owner/catalog/categories")]
public sealed class OwnerCategoriesController(CatalogDbContext db, ShopClient shopClient) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetAll()
    {
        var shop = await RequiredShopAsync();
        var items = await db.Categories.AsNoTracking().Where(x => x.ShopId == shop.Id).OrderBy(x => x.Name).ToListAsync();
        return Ok(items.Select(PublicCatalogController.Map));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
    {
        var shop = await RequiredShopAsync();
        if (await db.Categories.AnyAsync(x => x.ShopId == shop.Id && x.Name == request.Name.Trim()))
            return Conflict(new { message = "Tên danh mục đã tồn tại trong Shop." });
        var entity = new Category { ShopId = shop.Id, Name = request.Name.Trim(), Description = request.Description?.Trim() };
        db.Categories.Add(entity); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), PublicCatalogController.Map(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(Guid id, CategoryRequest request)
    {
        var shop = await RequiredShopAsync();
        var entity = await db.Categories.SingleOrDefaultAsync(x => x.Id == id && x.ShopId == shop.Id);
        if (entity is null) return NotFound();
        if (await db.Categories.AnyAsync(x => x.ShopId == shop.Id && x.Name == request.Name.Trim() && x.Id != id))
            return Conflict(new { message = "Tên danh mục đã tồn tại." });
        entity.Name = request.Name.Trim(); entity.Description = request.Description?.Trim();
        await db.SaveChangesAsync(); return Ok(PublicCatalogController.Map(entity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var shop = await RequiredShopAsync();
        var entity = await db.Categories.SingleOrDefaultAsync(x => x.Id == id && x.ShopId == shop.Id);
        if (entity is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.CategoryId == id && x.IsActive))
            return Conflict(new { message = "Không thể xóa danh mục đang chứa sản phẩm hoạt động." });
        db.Categories.Remove(entity); await db.SaveChangesAsync(); return NoContent();
    }

    private async Task<OwnedShop> RequiredShopAsync()
    {
        var shop = await shopClient.GetByOwnerAsync(User.GetRequiredUserId());
        if (shop is null) throw new KeyNotFoundException("Không tìm thấy Shop của tài khoản.");
        if (!shop.IsActive) throw new InvalidOperationException("Shop đang bị khóa hoặc ngừng hoạt động.");
        return shop;
    }
}
