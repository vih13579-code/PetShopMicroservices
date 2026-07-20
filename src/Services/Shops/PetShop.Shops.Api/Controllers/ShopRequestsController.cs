using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Contracts;
using PetShop.ServiceDefaults;
using PetShop.Shops.Api.Application;
using PetShop.Shops.Api.Contracts;
using PetShop.Shops.Api.Domain;
using PetShop.Shops.Api.Infrastructure;

namespace PetShop.Shops.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/shop-requests")]
public sealed class ShopRequestsController(
    ShopsDbContext db,
    IdentityClient identityClient,
    NotificationClient notificationClient) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ShopRequestResponse>> Create(CreateShopRequest request)
    {
        var userId = User.GetRequiredUserId();
        if (await db.Shops.AnyAsync(x => x.OwnerUserId == userId && x.Status != ShopStatus.Closed))
            return Conflict(new { message = "Tài khoản đã sở hữu Shop." });
        if (await db.ShopRegistrationRequests.AnyAsync(x => x.UserId == userId && x.Status == ShopRequestStatus.Pending))
            return Conflict(new { message = "Bạn đang có một yêu cầu chờ xử lý." });

        var entity = new ShopRegistrationRequest
        {
            UserId = userId,
            OwnerName = User.GetDisplayName(),
            ShopName = request.ShopName.Trim(),
            Description = request.Description?.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Address = request.Address.Trim(),
            TaxCode = request.TaxCode?.Trim()
        };
        db.ShopRegistrationRequests.Add(entity);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetDetail), new { id = entity.Id }, Map(entity));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<ActionResult<IReadOnlyCollection<ShopRequestResponse>>> Mine()
    {
        var userId = User.GetRequiredUserId();
        var items = await db.ShopRegistrationRequests.AsNoTracking()
            .Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(items.Select(Map));
    }

    [HttpGet]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<PagedResult<ShopRequestResponse>>> GetAll(
        string? keyword, ShopRequestStatus? status, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ShopRegistrationRequests.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.ShopName.Contains(value) || x.OwnerName.Contains(value) || x.Email.Contains(value));
        }
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<ShopRequestResponse>(items.Select(Map).ToArray(), page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShopRequestResponse>> GetDetail(Guid id)
    {
        var entity = await db.ShopRegistrationRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null) return NotFound();
        var userId = User.GetRequiredUserId();
        if (!User.IsInRole("Staff") && entity.UserId != userId) return Forbid();
        return Ok(Map(entity));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ShopResponse>> Approve(Guid id)
    {
        var entity = await db.ShopRegistrationRequests.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null) return NotFound();
        if (entity.Status != ShopRequestStatus.Pending) return Conflict(new { message = "Yêu cầu đã được xử lý." });
        if (await db.Shops.AnyAsync(x => x.OwnerUserId == entity.UserId)) return Conflict(new { message = "Người dùng đã có Shop." });

        var shop = new Shop
        {
            OwnerUserId = entity.UserId, Name = entity.ShopName, Description = entity.Description,
            Phone = entity.Phone, Email = entity.Email, Address = entity.Address, TaxCode = entity.TaxCode
        };
        entity.Status = ShopRequestStatus.Approved;
        entity.ProcessedAt = DateTime.UtcNow;
        entity.ProcessedBy = User.GetRequiredUserId();
        db.Shops.Add(shop);
        await db.SaveChangesAsync();

        await identityClient.GrantShopOwnerAsync(entity.UserId);
        await notificationClient.SendAsync(entity.UserId, "Yêu cầu mở Shop đã được duyệt",
            $"Shop {shop.Name} đã được kích hoạt.", "ShopApproved");
        return Ok(Map(shop));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> Reject(Guid id, RejectShopRequest request)
    {
        var entity = await db.ShopRegistrationRequests.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null) return NotFound();
        if (entity.Status != ShopRequestStatus.Pending) return Conflict(new { message = "Yêu cầu đã được xử lý." });
        entity.Status = ShopRequestStatus.Rejected;
        entity.RejectionReason = request.Reason.Trim();
        entity.ProcessedAt = DateTime.UtcNow;
        entity.ProcessedBy = User.GetRequiredUserId();
        await db.SaveChangesAsync();
        await notificationClient.SendAsync(entity.UserId, "Yêu cầu mở Shop bị từ chối",
            entity.RejectionReason, "ShopRejected");
        return NoContent();
    }

    private static ShopRequestResponse Map(ShopRegistrationRequest x) => new(x.Id, x.UserId, x.OwnerName,
        x.ShopName, x.Description, x.Phone, x.Email, x.Address, x.TaxCode, x.Status,
        x.RejectionReason, x.CreatedAt, x.ProcessedAt);
    private static ShopResponse Map(Shop x) => new(x.Id, x.OwnerUserId, x.Name, x.Description, x.Phone,
        x.Email, x.Address, x.TaxCode, x.Status, x.CreatedAt, x.UpdatedAt);
}
