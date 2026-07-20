using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Contracts;
using PetShop.Orders.Api.Application;
using PetShop.Orders.Api.Contracts;
using PetShop.Orders.Api.Domain;
using PetShop.Orders.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Orders.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController(
    OrdersDbContext db,
    InventoryClient inventoryClient,
    ShopClient shopClient,
    NotificationClient notificationClient) : ControllerBase
{
    [HttpPost("checkout")]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<ActionResult<IReadOnlyCollection<OrderResponse>>> Checkout(CheckoutRequest request)
    {
        var customerId = User.GetRequiredUserId();
        var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.CustomerId == customerId);
        if (cart is null || cart.Items.Count == 0) return BadRequest(new { message = "Giỏ hàng đang trống." });

        var orders = new List<Order>();
        var reservedOrderIds = new List<Guid>();
        try
        {
            foreach (var group in cart.Items.GroupBy(x => x.ShopId))
            {
                var order = new Order
                {
                    OrderCode = CreateOrderCode(), CustomerId = customerId, ShopId = group.Key,
                    ReceiverName = request.ReceiverName.Trim(), ReceiverPhone = request.ReceiverPhone.Trim(),
                    ShippingAddress = request.ShippingAddress.Trim(), Note = request.Note?.Trim(),
                    ShippingFee = request.ShippingFeePerShop, PaymentMethod = request.PaymentMethod,
                    PaymentStatus = request.PaymentMethod == PaymentMethod.COD ? PaymentStatus.CodPending : PaymentStatus.Pending,
                    Items = group.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId, VariantId = i.VariantId, ProductName = i.ProductName,
                        VariantName = i.VariantName, Sku = i.Sku, ImageUrl = i.ImageUrl,
                        UnitPrice = i.UnitPrice, Quantity = i.Quantity, LineTotal = i.UnitPrice * i.Quantity
                    }).ToList()
                };
                order.SubTotal = order.Items.Sum(x => x.LineTotal);
                order.TotalAmount = order.SubTotal + order.ShippingFee;
                order.StatusHistories.Add(new OrderStatusHistory { Status = OrderStatus.Pending, Note = "Khách hàng tạo đơn.", ChangedBy = customerId });

                await inventoryClient.ReserveAsync(order.Id, order.ShopId,
                    order.Items.Select(x => (x.ProductId, x.Quantity)));
                reservedOrderIds.Add(order.Id);
                orders.Add(order);
            }

            db.Orders.AddRange(orders);
            db.CartItems.RemoveRange(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        catch
        {
            foreach (var orderId in reservedOrderIds)
            {
                try { await inventoryClient.ReleaseAsync(orderId); } catch { /* best effort rollback */ }
            }
            throw;
        }

        return Ok(orders.Select(Map));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<ActionResult<PagedResult<OrderResponse>>> Mine(OrderStatus? status, int page = 1, int pageSize = 20)
    {
        var customerId = User.GetRequiredUserId(); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Orders.Include(x => x.Items).AsNoTracking().Where(x => x.CustomerId == customerId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<OrderResponse>(items.Select(Map).ToArray(), page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> Detail(Guid id)
    {
        var order = await db.Orders.Include(x => x.Items).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        var userId = User.GetRequiredUserId();
        if (order.CustomerId != userId && !await IsOwnerOfShopAsync(userId, order.ShopId) && !User.IsInRole("Admin") && !User.IsInRole("Staff")) return Forbid();
        return Ok(Map(order));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<IActionResult> CustomerCancel(Guid id, ChangeStatusRequest request)
    {
        var userId = User.GetRequiredUserId();
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == userId);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Pending) return Conflict(new { message = "Khách hàng chỉ được hủy đơn đang chờ xác nhận." });
        await inventoryClient.ReleaseAsync(order.Id);
        await UpdateStatusAsync(order, OrderStatus.Cancelled, request.Note ?? "Khách hàng hủy đơn.", userId);
        return NoContent();
    }

    [HttpGet("owner")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<ActionResult<PagedResult<OrderResponse>>> OwnerOrders(OrderStatus? status, int page = 1, int pageSize = 20)
    {
        var shop = await RequiredShopAsync(); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Orders.Include(x => x.Items).AsNoTracking().Where(x => x.ShopId == shop.Id);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResult<OrderResponse>(items.Select(Map).ToArray(), page, pageSize, total));
    }

    [HttpPost("owner/{id:guid}/confirm")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<IActionResult> Confirm(Guid id, ChangeStatusRequest request)
    {
        var order = await RequiredOwnerOrderAsync(id);
        if (order.Status != OrderStatus.Pending) return Conflict(new { message = "Chỉ đơn Pending mới được xác nhận." });
        await inventoryClient.CommitAsync(order.Id);
        await UpdateStatusAsync(order, OrderStatus.Confirmed, request.Note ?? "Shop xác nhận đơn.", User.GetRequiredUserId());
        await notificationClient.SendAsync(order.CustomerId, "Đơn hàng đã được xác nhận", $"Đơn {order.OrderCode} đã được Shop xác nhận.", "OrderConfirmed");
        return NoContent();
    }

    [HttpPost("owner/{id:guid}/preparing")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<IActionResult> Preparing(Guid id, ChangeStatusRequest request)
        => await OwnerMoveAsync(id, OrderStatus.Confirmed, OrderStatus.Preparing, request.Note ?? "Shop đang chuẩn bị hàng.");

    [HttpPost("owner/{id:guid}/shipping")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<IActionResult> Shipping(Guid id, ChangeStatusRequest request)
        => await OwnerMoveAsync(id, OrderStatus.Preparing, OrderStatus.Shipping, request.Note ?? "Đơn đang được giao.");

    [HttpPost("owner/{id:guid}/complete")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<IActionResult> Complete(Guid id, ChangeStatusRequest request)
        => await OwnerMoveAsync(id, OrderStatus.Shipping, OrderStatus.Completed, request.Note ?? "Đơn đã hoàn thành.");

    [HttpPost("owner/{id:guid}/cancel")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<IActionResult> OwnerCancel(Guid id, ChangeStatusRequest request)
    {
        var order = await RequiredOwnerOrderAsync(id);
        if (order.Status is OrderStatus.Shipping or OrderStatus.Completed or OrderStatus.Cancelled)
            return Conflict(new { message = "Không thể hủy đơn ở trạng thái hiện tại." });
        if (order.Status == OrderStatus.Pending) await inventoryClient.ReleaseAsync(order.Id);
        else await inventoryClient.ReturnAsync(order.Id);
        await UpdateStatusAsync(order, OrderStatus.Cancelled, request.Note ?? "Shop hủy đơn.", User.GetRequiredUserId());
        await notificationClient.SendAsync(order.CustomerId, "Đơn hàng đã bị hủy", $"Đơn {order.OrderCode} đã bị Shop hủy.", "OrderCancelled");
        return NoContent();
    }

    private async Task<IActionResult> OwnerMoveAsync(Guid id, OrderStatus required, OrderStatus next, string note)
    {
        var order = await RequiredOwnerOrderAsync(id);
        if (order.Status != required) return Conflict(new { message = $"Đơn phải ở trạng thái {required}." });
        await UpdateStatusAsync(order, next, note, User.GetRequiredUserId());
        await notificationClient.SendAsync(order.CustomerId, "Cập nhật đơn hàng", $"Đơn {order.OrderCode}: {next}.", $"Order{next}");
        return NoContent();
    }

    private async Task<Order> RequiredOwnerOrderAsync(Guid id)
    {
        var shop = await RequiredShopAsync();
        return await db.Orders.SingleOrDefaultAsync(x => x.Id == id && x.ShopId == shop.Id)
               ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng của Shop.");
    }

    private async Task<OwnedShop> RequiredShopAsync()
    {
        var shop = await shopClient.GetByOwnerAsync(User.GetRequiredUserId());
        if (shop is null) throw new KeyNotFoundException("Không tìm thấy Shop của tài khoản.");
        if (!shop.IsActive) throw new InvalidOperationException("Shop đang bị khóa hoặc ngừng hoạt động.");
        return shop;
    }

    private async Task<bool> IsOwnerOfShopAsync(Guid userId, Guid shopId)
    {
        var shop = await shopClient.GetByOwnerAsync(userId);
        return shop?.Id == shopId;
    }

    private async Task UpdateStatusAsync(Order order, OrderStatus status, string note, Guid changedBy)
    {
        order.Status = status; order.UpdatedAt = DateTime.UtcNow;
        db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = order.Id, Status = status, Note = note, ChangedBy = changedBy });
        await db.SaveChangesAsync();
    }

    private static string CreateOrderCode() => $"PS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    internal static OrderResponse Map(Order x) => new(x.Id, x.OrderCode, x.CustomerId, x.ShopId,
        x.ReceiverName, x.ReceiverPhone, x.ShippingAddress, x.Note, x.SubTotal, x.ShippingFee,
        x.TotalAmount, x.PaymentMethod, x.PaymentStatus, x.Status, x.CreatedAt,
        x.Items.Select(i => new OrderItemResponse(i.Id, i.ProductId, i.VariantId, i.ProductName,
            i.VariantName, i.Sku, i.ImageUrl, i.UnitPrice, i.Quantity, i.LineTotal)).ToArray());
}
