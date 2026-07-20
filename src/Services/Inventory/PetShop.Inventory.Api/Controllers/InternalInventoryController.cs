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
    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve(ReserveStockRequest request)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        if (request.Items.Count == 0 || request.Items.Any(x => x.Quantity <= 0)) return BadRequest(new { message = "Danh sách giữ hàng không hợp lệ." });
        if (await db.StockReservations.AnyAsync(x => x.OrderId == request.OrderId && !x.IsReleased)) return NoContent();

        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var requested in request.Items)
        {
            var item = await db.InventoryItems.SingleOrDefaultAsync(x => x.ShopId == request.ShopId && x.ProductId == requested.ProductId);
            if (item is null || item.Quantity - item.ReservedQuantity < requested.Quantity)
                return Conflict(new { message = $"Sản phẩm {requested.ProductId} không đủ tồn kho." });
            item.ReservedQuantity += requested.Quantity; item.UpdatedAt = DateTime.UtcNow;
            db.StockReservations.Add(new StockReservation { OrderId = request.OrderId, ShopId = request.ShopId,
                ProductId = requested.ProductId, Quantity = requested.Quantity });
            db.StockTransactions.Add(new StockTransaction { OrderId = request.OrderId, ShopId = request.ShopId,
                ProductId = requested.ProductId, QuantityChange = 0, Type = StockTransactionType.Reserve,
                Reason = $"Giữ {requested.Quantity} sản phẩm cho đơn hàng." });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return NoContent();
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
            var item = await db.InventoryItems.SingleAsync(x => x.ShopId == reservation.ShopId && x.ProductId == reservation.ProductId);
            item.Quantity -= reservation.Quantity; item.ReservedQuantity -= reservation.Quantity; item.UpdatedAt = DateTime.UtcNow;
            reservation.IsCommitted = true;
            db.StockTransactions.Add(new StockTransaction { OrderId = orderId, ShopId = reservation.ShopId,
                ProductId = reservation.ProductId, QuantityChange = -reservation.Quantity, Type = StockTransactionType.Commit,
                Reason = "Trừ kho khi Shop xác nhận đơn." });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/release")]
    public async Task<IActionResult> Release(Guid orderId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var reservations = await db.StockReservations.Where(x => x.OrderId == orderId && !x.IsReleased && !x.IsCommitted).ToListAsync();
        foreach (var reservation in reservations)
        {
            var item = await db.InventoryItems.SingleAsync(x => x.ShopId == reservation.ShopId && x.ProductId == reservation.ProductId);
            item.ReservedQuantity -= reservation.Quantity; item.UpdatedAt = DateTime.UtcNow;
            reservation.IsReleased = true;
            db.StockTransactions.Add(new StockTransaction { OrderId = orderId, ShopId = reservation.ShopId,
                ProductId = reservation.ProductId, QuantityChange = 0, Type = StockTransactionType.Release,
                Reason = "Hủy giữ hàng." });
        }
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/return")]
    public async Task<IActionResult> Return(Guid orderId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var reservations = await db.StockReservations.Where(x => x.OrderId == orderId && x.IsCommitted && !x.IsReleased).ToListAsync();
        foreach (var reservation in reservations)
        {
            var item = await db.InventoryItems.SingleAsync(x => x.ShopId == reservation.ShopId && x.ProductId == reservation.ProductId);
            item.Quantity += reservation.Quantity; item.UpdatedAt = DateTime.UtcNow; reservation.IsReleased = true;
            db.StockTransactions.Add(new StockTransaction { OrderId = orderId, ShopId = reservation.ShopId,
                ProductId = reservation.ProductId, QuantityChange = reservation.Quantity, Type = StockTransactionType.Return,
                Reason = "Hoàn kho do đơn hàng bị hủy sau khi đã xác nhận." });
        }
        await db.SaveChangesAsync(); return NoContent();
    }
}
