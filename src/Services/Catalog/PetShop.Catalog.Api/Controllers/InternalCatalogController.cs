using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Catalog.Api.Contracts;
using PetShop.Catalog.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Catalog.Api.Controllers;

[ApiController]
[Route("internal/catalog")]
public sealed class InternalCatalogController(CatalogDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpGet("products/{productId:guid}")]
    public async Task<ActionResult<ProductSnapshot>> Product(Guid productId, Guid? variantId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var product = await db.Products.Include(x => x.Variants).AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId);
        if (product is null) return NotFound();
        var variant = variantId.HasValue ? product.Variants.SingleOrDefault(x => x.Id == variantId.Value) : null;
        if (variantId.HasValue && variant is null) return BadRequest(new { message = "Phân loại không thuộc sản phẩm." });
        var active = product.IsActive && (variant is null || variant.IsActive);
        return Ok(new ProductSnapshot(product.Id, product.ShopId, product.Name, product.ImageUrl, active,
            product.Price, variant?.Id, variant?.Name, variant?.Sku, product.Price + (variant?.AdditionalPrice ?? 0)));
    }

    [HttpPost("products/batch")]
    public async Task<IActionResult> Batch([FromBody] Guid[] productIds)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var items = await db.Products.AsNoTracking().Where(x => productIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ShopId, x.Name, x.Price, x.ImageUrl, x.IsActive }).ToListAsync();
        return Ok(items);
    }
}
