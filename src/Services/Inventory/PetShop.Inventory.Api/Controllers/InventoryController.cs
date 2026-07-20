using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Inventory.Api.Application;
using PetShop.Inventory.Api.Contracts;
using PetShop.Inventory.Api.Domain;
using PetShop.Inventory.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Inventory.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(InventoryDbContext db, ShopClient shopClient) : ControllerBase
{
    [HttpGet("availability/{productId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<InventoryResponse>> Availability(Guid productId)
    {
        var item = await db.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId);
        return item is null ? Ok(new InventoryResponse(productId, Guid.Empty, 0, 0, 0, DateTime.UtcNow)) : Ok(Map(item));
    }

    [HttpPost("availability/batch")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<InventoryResponse>>> AvailabilityBatch([FromBody] Guid[] productIds)
    {
        var items = await db.InventoryItems.AsNoTracking().Where(x => productIds.Contains(x.ProductId)).ToListAsync();
        var map = items.ToDictionary(x => x.ProductId);
        return Ok(productIds.Distinct().Select(id => map.TryGetValue(id, out var item)
            ? Map(item) : new InventoryResponse(id, Guid.Empty, 0, 0, 0, DateTime.UtcNow)));
    }

    [HttpGet("owner")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<ActionResult<IReadOnlyCollection<InventoryResponse>>> OwnerList()
    {
        var shop = await RequiredShopAsync();
        var items = await db.InventoryItems.AsNoTracking().Where(x => x.ShopId == shop.Id).OrderBy(x => x.ProductId).ToListAsync();
        return Ok(items.Select(Map));
    }

    [HttpPut("owner/set")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<ActionResult<InventoryResponse>> Set(SetStockRequest request)
    {
        var shop = await RequiredShopAsync();
        var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.ShopId == shop.Id && x.ProductId == request.ProductId);
        if (item is null)
        {
            item = new InventoryItem { ShopId = shop.Id, ProductId = request.ProductId, Quantity = request.Quantity };
            db.InventoryItems.Add(item);
            db.StockTransactions.Add(new StockTransaction { ShopId = shop.Id, ProductId = request.ProductId,
                QuantityChange = request.Quantity, Type = StockTransactionType.Initial, Reason = request.Reason, PerformedBy = User.GetRequiredUserId() });
        }
        else
        {
            if (request.Quantity < item.ReservedQuantity) return BadRequest(new { message = "Số lượng mới không thể nhỏ hơn số đang được giữ." });
            var delta = request.Quantity - item.Quantity;
            item.Quantity = request.Quantity; item.UpdatedAt = DateTime.UtcNow;
            db.StockTransactions.Add(new StockTransaction { ShopId = shop.Id, ProductId = request.ProductId,
                QuantityChange = delta, Type = StockTransactionType.ManualAdjust, Reason = request.Reason, PerformedBy = User.GetRequiredUserId() });
        }
        await db.SaveChangesAsync(); return Ok(Map(item));
    }

    [HttpPost("owner/adjust")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<ActionResult<InventoryResponse>> Adjust(AdjustStockRequest request)
    {
        if (request.QuantityChange == 0) return BadRequest(new { message = "QuantityChange phải khác 0." });
        var shop = await RequiredShopAsync();
        var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.ShopId == shop.Id && x.ProductId == request.ProductId);
        if (item is null)
        {
            if (request.QuantityChange < 0) return BadRequest(new { message = "Không thể giảm kho chưa tồn tại." });
            item = new InventoryItem { ShopId = shop.Id, ProductId = request.ProductId, Quantity = request.QuantityChange };
            db.InventoryItems.Add(item);
        }
        else
        {
            var next = item.Quantity + request.QuantityChange;
            if (next < item.ReservedQuantity || next < 0) return BadRequest(new { message = "Số lượng sau điều chỉnh không hợp lệ." });
            item.Quantity = next; item.UpdatedAt = DateTime.UtcNow;
        }
        db.StockTransactions.Add(new StockTransaction { ShopId = shop.Id, ProductId = request.ProductId,
            QuantityChange = request.QuantityChange, Type = request.QuantityChange > 0 ? StockTransactionType.Import : StockTransactionType.ManualAdjust,
            Reason = request.Reason.Trim(), PerformedBy = User.GetRequiredUserId() });
        await db.SaveChangesAsync(); return Ok(Map(item));
    }

    private async Task<OwnedShop> RequiredShopAsync()
    {
        var shop = await shopClient.GetByOwnerAsync(User.GetRequiredUserId());
        if (shop is null) throw new KeyNotFoundException("Không tìm thấy Shop của tài khoản.");
        if (!shop.IsActive) throw new InvalidOperationException("Shop đang bị khóa hoặc ngừng hoạt động.");
        return shop;
    }

    internal static InventoryResponse Map(InventoryItem x) => new(x.ProductId, x.ShopId, x.Quantity,
        x.ReservedQuantity, x.Quantity - x.ReservedQuantity, x.UpdatedAt);
}
