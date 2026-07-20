using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Inventory.Api.Contracts;
using PetShop.Inventory.Api.Domain;
using PetShop.Inventory.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Inventory.Api.Controllers;

[ApiController]
[Route("internal/inventory")]
public sealed class InternalInventoryController(InventoryDbContext db, IConfiguration configuration) : ControllerBase
{
    private static readonly Guid SystemActorId = new("00000000-0000-0000-0000-000000000001");

    [HttpGet("availability/{productId:guid}")]
    public async Task<IActionResult> GetAvailability(Guid productId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var item = await db.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.ProductId == productId);
        if (item is null) return Ok(new { productId, availableQuantity = 100 });

        var activeReserved = await db.StockReservations
            .Where(r => r.ProductId == productId && !r.IsReleased && !r.IsCommitted)
            .SumAsync(r => (int?)r.Quantity) ?? 0;

        var available = Math.Max(0, item.Quantity - activeReserved);
        return Ok(new { productId, availableQuantity = available });
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve(ReserveStockRequest request)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        if (request.Items.Count == 0 || request.Items.Any(x => x.Quantity <= 0))
            return BadRequest(new { message = "Danh sách giữ hàng không hợp lệ." });

        if (await db.StockReservations.AnyAsync(x => x.OrderId == request.OrderId && !x.IsReleased)) return NoContent();

        // Release stale reservations older than 15 minutes that were never committed or explicitly released
        var staleThreshold = DateTime.UtcNow.AddMinutes(-15);
        var staleReservations = await db.StockReservations
            .Where(r => !r.IsReleased && !r.IsCommitted && r.CreatedAt < staleThreshold)
            .ToListAsync();

        if (staleReservations.Count > 0)
        {
            foreach (var stale in staleReservations) stale.IsReleased = true;
            await db.SaveChangesAsync();
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var requested in request.Items)
        {
            var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.ProductId == requested.ProductId);
            if (item is null)
            {
                item = new InventoryItem
                {
                    ShopId = request.ShopId,
                    ProductId = requested.ProductId,
                    Quantity = 100,
                    ReservedQuantity = 0
                };
                db.InventoryItems.Add(item);
            }
            else if (item.ShopId == Guid.Empty || item.ShopId != request.ShopId)
            {
                item.ShopId = request.ShopId;
            }

            // Self-healing: recalculate active reserved quantity from database
            var activeReserved = await db.StockReservations
                .Where(r => r.ProductId == requested.ProductId && !r.IsReleased && !r.IsCommitted && r.OrderId != request.OrderId)
                .SumAsync(r => (int?)r.Quantity) ?? 0;

            item.ReservedQuantity = activeReserved;
            var available = item.Quantity - item.ReservedQuantity;

            if (available <= 0)
            {
                await tx.RollbackAsync();
                return Conflict(new { message = "Sản phẩm đã hết hàng trong kho." });
            }

            if (available < requested.Quantity)
            {
                await tx.RollbackAsync();
                return Conflict(new { message = $"Sản phẩm chỉ còn lại {available} trong kho (bạn đang chọn {requested.Quantity} trong giỏ)." });
            }

            item.ReservedQuantity += requested.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
            db.StockReservations.Add(new StockReservation
            {
                OrderId = request.OrderId,
                ShopId = request.ShopId,
                ProductId = requested.ProductId,
                Quantity = requested.Quantity
            });
            db.StockTransactions.Add(new StockTransaction
            {
                OrderId = request.OrderId,
                ShopId = request.ShopId,
                ProductId = requested.ProductId,
                QuantityChange = 0,
                Type = StockTransactionType.Reserve,
                Reason = $"Giữ {requested.Quantity} sản phẩm cho đơn hàng.",
                PerformedBy = SystemActorId
            });
        }

        try
        {
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            return Conflict(new { message = "Xảy ra tranh chấp tồn kho do xử lý đồng thời. Vui lòng thử lại." });
        }
    }

    [HttpPost("orders/{orderId:guid}/commit")]
    public async Task<IActionResult> Commit(Guid orderId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var reservations = await db.StockReservations.Where(x => x.OrderId == orderId && !x.IsReleased && !x.IsCommitted).ToListAsync();
        if (reservations.Count == 0) return NoContent();
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var reservation in reservations)
        {
            var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.ProductId == reservation.ProductId);
            if (item is not null)
            {
                item.Quantity = Math.Max(0, item.Quantity - reservation.Quantity);
                item.ReservedQuantity = Math.Max(0, item.ReservedQuantity - reservation.Quantity);
                item.UpdatedAt = DateTime.UtcNow;
            }
            reservation.IsCommitted = true;
            db.StockTransactions.Add(new StockTransaction
            {
                OrderId = orderId,
                ShopId = reservation.ShopId,
                ProductId = reservation.ProductId,
                QuantityChange = -reservation.Quantity,
                Type = StockTransactionType.Commit,
                Reason = "Trừ kho khi Shop xác nhận đơn.",
                PerformedBy = SystemActorId
            });
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/release")]
    public async Task<IActionResult> Release(Guid orderId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var reservations = await db.StockReservations.Where(x => x.OrderId == orderId && !x.IsReleased && !x.IsCommitted).ToListAsync();
        foreach (var reservation in reservations)
        {
            var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.ProductId == reservation.ProductId);
            if (item is not null)
            {
                item.ReservedQuantity = Math.Max(0, item.ReservedQuantity - reservation.Quantity);
                item.UpdatedAt = DateTime.UtcNow;
            }
            reservation.IsReleased = true;
            db.StockTransactions.Add(new StockTransaction
            {
                OrderId = orderId,
                ShopId = reservation.ShopId,
                ProductId = reservation.ProductId,
                QuantityChange = 0,
                Type = StockTransactionType.Release,
                Reason = "Hủy giữ hàng.",
                PerformedBy = SystemActorId
            });
        }
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/return")]
    public async Task<IActionResult> Return(Guid orderId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var reservations = await db.StockReservations.Where(x => x.OrderId == orderId && x.IsCommitted && !x.IsReleased).ToListAsync();
        foreach (var reservation in reservations)
        {
            var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.ProductId == reservation.ProductId);
            if (item is not null)
            {
                item.Quantity += reservation.Quantity;
                item.UpdatedAt = DateTime.UtcNow;
            }
            reservation.IsReleased = true;
            db.StockTransactions.Add(new StockTransaction
            {
                OrderId = orderId,
                ShopId = reservation.ShopId,
                ProductId = reservation.ProductId,
                QuantityChange = reservation.Quantity,
                Type = StockTransactionType.Return,
                Reason = "Hoàn kho do đơn hàng bị hủy sau khi đã xác nhận.",
                PerformedBy = SystemActorId
            });
        }
        await db.SaveChangesAsync();
        return NoContent();
    }
}
