using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Contracts;
using PetShop.ServiceDefaults;
using PetShop.Shops.Api.Contracts;
using PetShop.Shops.Api.Domain;
using PetShop.Shops.Api.Infrastructure;

namespace PetShop.Shops.Api.Controllers;

[ApiController]
[Route("api/shops")]
public sealed class ShopsController(ShopsDbContext db) : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ShopResponse>>> PublicList(string? keyword)
    {
        var query = db.Shops.AsNoTracking().Where(x => x.Status == ShopStatus.Active);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Name.Contains(keyword.Trim()));
        var items = await query.OrderBy(x => x.Name).ToListAsync();
        return Ok(items.Select(Map));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<PagedResult<ShopResponse>>> GetAll(string? keyword, ShopStatus? status, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Shops.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Name.Contains(keyword.Trim()) || x.Email.Contains(keyword.Trim()));
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<ShopResponse>(items.Select(Map).ToArray(), page, pageSize, total));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<ActionResult<ShopResponse>> Mine()
    {
        var shop = await db.Shops.AsNoTracking().SingleOrDefaultAsync(x => x.OwnerUserId == User.GetRequiredUserId());
        return shop is null ? NotFound() : Ok(Map(shop));
    }

    [HttpPut("mine")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<ActionResult<ShopResponse>> UpdateMine(UpdateShopRequest request)
    {
        var shop = await db.Shops.SingleOrDefaultAsync(x => x.OwnerUserId == User.GetRequiredUserId());
        if (shop is null) return NotFound();
        shop.Name = request.Name.Trim(); shop.Description = request.Description?.Trim();
        shop.Phone = request.Phone.Trim(); shop.Email = request.Email.Trim().ToLowerInvariant();
        shop.Address = request.Address.Trim(); shop.TaxCode = request.TaxCode?.Trim(); shop.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(Map(shop));
    }

    [HttpPatch("{id:guid}/lock")]
    [Authorize(Roles = "Admin,Staff")]
    public Task<IActionResult> Lock(Guid id) => SetStatus(id, ShopStatus.Locked);

    [HttpPatch("{id:guid}/unlock")]
    [Authorize(Roles = "Admin,Staff")]
    public Task<IActionResult> Unlock(Guid id) => SetStatus(id, ShopStatus.Active);

    private async Task<IActionResult> SetStatus(Guid id, ShopStatus status)
    {
        var shop = await db.Shops.SingleOrDefaultAsync(x => x.Id == id);
        if (shop is null) return NotFound();
        shop.Status = status; shop.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return NoContent();
    }

    private static ShopResponse Map(Shop x) => new(x.Id, x.OwnerUserId, x.Name, x.Description,
        x.Phone, x.Email, x.Address, x.TaxCode, x.Status, x.CreatedAt, x.UpdatedAt);
}
