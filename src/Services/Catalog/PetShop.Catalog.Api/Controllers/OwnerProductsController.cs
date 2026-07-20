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
        return Ok(new PagedResult<ProductResponse>(items.Select(x => PublicCatalogController.Map(x)).ToArray(), page, pageSize, total));
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

        var normalizedSkus = request.Variants.Select(x => x.Sku.Trim()).ToArray();
        if (normalizedSkus.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalizedSkus.Length)
            return BadRequest(new { message = "SKU trong danh sách phân loại không được trùng nhau." });

        // Browsers reindex collection fields after a row is removed. Reconcile an omitted id by SKU so an
        // existing variant is updated instead of being deleted and recreated with a new identity.
        foreach (var variantRequest in request.Variants.Where(x => !x.Id.HasValue))
        {
            var existingVariant = product.Variants.SingleOrDefault(x =>
                string.Equals(x.Sku, variantRequest.Sku.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existingVariant is not null) variantRequest.Id = existingVariant.Id;
        }

        var requestIds = request.Variants.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToArray();
        if (requestIds.Distinct().Count() != requestIds.Length || requestIds.Any(variantId => product.Variants.All(x => x.Id != variantId)))
            return BadRequest(new { message = "Phân loại cập nhật không thuộc sản phẩm." });
        var removedIds = request.RemovedVariantIds.Distinct().ToHashSet();
        if (removedIds.Overlaps(requestIds) || removedIds.Any(variantId => product.Variants.All(x => x.Id != variantId)))
            return BadRequest(new { message = "Danh sách phân loại cần xóa không hợp lệ." });
        if (await db.ProductVariants.AnyAsync(x => normalizedSkus.Contains(x.Sku) && x.ProductId != product.Id))
            return Conflict(new { message = "Một hoặc nhiều SKU đã được sử dụng bởi sản phẩm khác." });

        await using var transaction = await db.Database.BeginTransactionAsync();
        var retainedIds = requestIds.ToHashSet();
        var existingById = product.Variants.ToDictionary(x => x.Id);

        try
        {
            // Release changed SKU values first so swaps and replacements cannot violate the unique index.
            var changedVariants = request.Variants.Where(x => x.Id.HasValue)
                .Where(x => !string.Equals(existingById[x.Id!.Value].Sku, x.Sku.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(x => existingById[x.Id!.Value]).ToArray();
            foreach (var variant in changedVariants) variant.Sku = $"__tmp__{Guid.NewGuid():N}";
            if (changedVariants.Length > 0) await db.SaveChangesAsync();

            var removedVariants = product.Variants.Where(x => removedIds.Contains(x.Id) || !retainedIds.Contains(x.Id)).ToArray();
            db.ProductVariants.RemoveRange(removedVariants);
            if (removedVariants.Length > 0) await db.SaveChangesAsync();

            product.CategoryId = category.Id; product.Category = category; product.Name = request.Name.Trim();
            product.Description = request.Description?.Trim(); product.Price = request.Price;
            product.ImageUrl = request.ImageUrl?.Trim(); product.IsActive = request.IsActive; product.UpdatedAt = DateTime.UtcNow;

            foreach (var variantRequest in request.Variants)
            {
                var variant = variantRequest.Id.HasValue
                    ? existingById[variantRequest.Id.Value]
                    : new ProductVariant { ProductId = product.Id };
                variant.Name = variantRequest.Name.Trim();
                variant.Sku = variantRequest.Sku.Trim();
                variant.AdditionalPrice = variantRequest.AdditionalPrice;
                variant.IsActive = variantRequest.IsActive;
                if (!variantRequest.Id.HasValue) product.Variants.Add(variant);
            }
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "Không thể lưu phân loại vì SKU đã được sử dụng. Vui lòng nhập SKU khác." });
        }

        // Reload after deletions so the response cannot contain variants left in the tracked navigation collection.
        db.ChangeTracker.Clear();
        var updated = await db.Products.Include(x => x.Category).Include(x => x.Variants)
            .AsNoTracking().SingleAsync(x => x.Id == product.Id);
        return Ok(PublicCatalogController.Map(updated));
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
