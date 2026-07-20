using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Catalog.Api.Application;
using PetShop.Catalog.Api.Contracts;
using PetShop.Catalog.Api.Domain;
using PetShop.Catalog.Api.Infrastructure;
using PetShop.Contracts;
using PetShop.ServiceDefaults;

namespace PetShop.Catalog.Api.Controllers;

[ApiController]
[Authorize(Roles = "ShopOwner")]
[Route("api/owner/catalog/products")]
public sealed class OwnerProductsController(CatalogDbContext db, ShopClient shopClient) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetAll(string? keyword, Guid? categoryId,
        bool? isActive, int page = 1, int pageSize = 20)
    {
        var shop = await RequiredShopAsync(); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.Include(x => x.Category).Include(x => x.Variants).AsNoTracking().Where(x => x.ShopId == shop.Id);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Name.Contains(keyword.Trim()));
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<ProductResponse>(items.Select(PublicCatalogController.Map).ToArray(), page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Detail(Guid id)
    {
        var shop = await RequiredShopAsync();
        var item = await db.Products.Include(x => x.Category).Include(x => x.Variants).AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.ShopId == shop.Id);
        return item is null ? NotFound() : Ok(PublicCatalogController.Map(item));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        var shop = await RequiredShopAsync();
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == request.CategoryId && x.ShopId == shop.Id);
        if (category is null) return BadRequest(new { message = "Danh mục không thuộc Shop." });
        if (request.Variants.Select(x => x.Sku.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Variants.Count)
            return BadRequest(new { message = "SKU trong danh sách phân loại không được trùng nhau." });
        foreach (var sku in request.Variants.Select(x => x.Sku.Trim()))
            if (await db.ProductVariants.AnyAsync(x => x.Sku == sku)) return Conflict(new { message = $"SKU {sku} đã tồn tại." });

        var product = new Product
        {
            ShopId = shop.Id, CategoryId = category.Id, Name = request.Name.Trim(),
            Description = request.Description?.Trim(), Price = request.Price,
            ImageUrl = request.ImageUrl?.Trim(), IsActive = request.IsActive,
            Variants = request.Variants.Select(v => new ProductVariant
            {
                Name = v.Name.Trim(), Sku = v.Sku.Trim(), AdditionalPrice = v.AdditionalPrice, IsActive = v.IsActive
            }).ToList()
        };
        db.Products.Add(product); await db.SaveChangesAsync();
        await db.Entry(product).Reference(x => x.Category).LoadAsync();
        return CreatedAtAction(nameof(Detail), new { id = product.Id }, PublicCatalogController.Map(product));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, ProductRequest request)
    {
        var shop = await RequiredShopAsync();
        var product = await db.Products.Include(x => x.Category).Include(x => x.Variants)
            .SingleOrDefaultAsync(x => x.Id == id && x.ShopId == shop.Id);
        if (product is null) return NotFound();
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == request.CategoryId && x.ShopId == shop.Id);
        if (category is null) return BadRequest(new { message = "Danh mục không thuộc Shop." });

        product.CategoryId = category.Id; product.Category = category; product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim(); product.Price = request.Price;
        product.ImageUrl = request.ImageUrl?.Trim(); product.IsActive = request.IsActive; product.UpdatedAt = DateTime.UtcNow;

        db.ProductVariants.RemoveRange(product.Variants);
        await db.SaveChangesAsync();

        product.Variants = request.Variants.Select(v => new ProductVariant
        {
            ProductId = product.Id, Name = v.Name.Trim(), Sku = v.Sku.Trim(), AdditionalPrice = v.AdditionalPrice, IsActive = v.IsActive
        }).ToList();
        await db.SaveChangesAsync();
        return Ok(PublicCatalogController.Map(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var shop = await RequiredShopAsync();
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id && x.ShopId == shop.Id);
        if (product is null) return NotFound();
        product.IsActive = false; product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return NoContent();
    }

    private async Task<OwnedShop> RequiredShopAsync()
    {
        var shop = await shopClient.GetByOwnerAsync(User.GetRequiredUserId());
        if (shop is null) throw new KeyNotFoundException("Không tìm thấy Shop của tài khoản.");
        if (!shop.IsActive) throw new InvalidOperationException("Shop đang bị khóa hoặc ngừng hoạt động.");
        return shop;
    }
}
