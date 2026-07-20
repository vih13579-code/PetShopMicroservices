using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.ServiceDefaults;
using PetShop.Shops.Api.Domain;
using PetShop.Shops.Api.Infrastructure;

namespace PetShop.Shops.Api.Controllers;

[ApiController]
[Route("internal/shops")]
public sealed class InternalShopsController(ShopsDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpGet("by-owner/{userId:guid}")]
    public async Task<IActionResult> ByOwner(Guid userId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var shop = await db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == userId);
        return shop is null ? NotFound() : Ok(new { shop.Id, shop.OwnerUserId, shop.Name, status = shop.Status.ToString(), isActive = shop.Status == ShopStatus.Active });
    }

    [HttpGet("{shopId:guid}")]
    public async Task<IActionResult> ById(Guid shopId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var shop = await db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.Id == shopId);
        return shop is null ? NotFound() : Ok(new { shop.Id, shop.OwnerUserId, shop.Name, status = shop.Status.ToString(), isActive = shop.Status == ShopStatus.Active });
    }
}
