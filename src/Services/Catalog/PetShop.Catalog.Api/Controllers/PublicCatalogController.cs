using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Catalog.Api.Contracts;
using PetShop.Catalog.Api.Domain;
using PetShop.Catalog.Api.Infrastructure;
using PetShop.Contracts;

namespace PetShop.Catalog.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/catalog")]
public sealed class PublicCatalogController(CatalogDbContext db) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> Categories(Guid? shopId)
    {
        var query = db.Categories.AsNoTracking().Where(x => x.IsActive);
        if (shopId.HasValue) query = query.Where(x => x.ShopId == shopId.Value);
        var items = await query.OrderBy(x => x.Name).ToListAsync();
        return Ok(items.Select(Map));
    }

    [HttpGet("products")]
    public async Task<ActionResult<PagedResult<ProductResponse>>> Products(Guid? shopId, Guid? categoryId,
        string? keyword, decimal? minPrice, decimal? maxPrice, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.Include(x => x.Category).Include(x => x.Variants)
            .AsNoTracking().Where(x => x.IsActive && x.Category.IsActive);
        if (shopId.HasValue) query = query.Where(x => x.ShopId == shopId.Value);
        if (categoryId.HasValue) query = query.Where(x => x.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Name.Contains(keyword.Trim()) || (x.Description ?? "").Contains(keyword.Trim()));
        if (minPrice.HasValue) query = query.Where(x => x.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(x => x.Price <= maxPrice.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<ProductResponse>(items.Select(Map).ToArray(), page, pageSize, total));
    }

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Product(Guid id)
    {
        var item = await db.Products.Include(x => x.Category).Include(x => x.Variants)
            .AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
        return item is null ? NotFound() : Ok(Map(item));
    }

    internal static CategoryResponse Map(Category x) => new(x.Id, x.ShopId, x.Name, x.Description, x.IsActive, x.CreatedAt);
    internal static ProductResponse Map(Product x) => new(x.Id, x.ShopId, x.CategoryId, x.Category.Name, x.Name,
        x.Description, x.Price, x.ImageUrl, x.IsActive, x.CreatedAt,
        x.Variants.Select(v => new VariantResponse(v.Id, v.Name, v.Sku, v.AdditionalPrice, v.IsActive)).ToArray());
}
