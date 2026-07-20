using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Payments.Api.Application;
using PetShop.Payments.Api.Contracts;
using PetShop.Payments.Api.Domain;
using PetShop.Payments.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Payments.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController(
    PaymentsDbContext db,
    OrdersClient ordersClient,
    NotificationClient notificationClient,
    ShopClient shopClient) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<ActionResult<PaymentResponse>> Create(CreatePaymentRequest request)
    {
        var currentUserId = User.GetRequiredUserId();
        var order = await ordersClient.GetAsync(request.OrderId);
        if (order is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        if (order.CustomerId != currentUserId) return Forbid();
        if (order.Status is "Cancelled" or "Completed") return Conflict(new { message = "Không thể tạo thanh toán cho đơn ở trạng thái hiện tại." });

        var existing = await db.Payments.SingleOrDefaultAsync(x => x.OrderId == request.OrderId);
        if (existing is not null) return Ok(Map(existing));

        var payment = new Payment
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Amount = order.TotalAmount,
            Method = request.Method,
            Status = request.Method == PaymentMethod.COD ? PaymentStatus.CodPending : PaymentStatus.Pending,
            TransactionCode = request.Method == PaymentMethod.BankTransferMock
                ? $"MOCK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}"
                : null
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        await ordersClient.SetPaymentStatusAsync(order.Id, payment.Status.ToString());
        return CreatedAtAction(nameof(GetByOrder), new { orderId = order.Id }, Map(payment));
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetByOrder(Guid orderId)
    {
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == orderId);
        if (payment is null) return NotFound();
        var userId = User.GetRequiredUserId();
        if (payment.CustomerId != userId && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            var order = await ordersClient.GetAsync(orderId);
            var ownedShop = await shopClient.GetByOwnerAsync(userId);
            if (order is null || ownedShop?.Id != order.ShopId) return Forbid();
        }
        return Ok(Map(payment));
    }

    [HttpPost("{id:guid}/confirm-mock")]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<ActionResult<PaymentResponse>> ConfirmMock(Guid id)
    {
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.Id == id);
        if (payment is null) return NotFound();
        if (payment.CustomerId != User.GetRequiredUserId()) return Forbid();
        if (payment.Method != PaymentMethod.BankTransferMock) return BadRequest(new { message = "Chỉ áp dụng cho thanh toán chuyển khoản mô phỏng." });
        if (payment.Status == PaymentStatus.Paid) return Ok(Map(payment));
        if (payment.Status != PaymentStatus.Pending) return Conflict(new { message = "Thanh toán không còn ở trạng thái Pending." });

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await ordersClient.SetPaymentStatusAsync(payment.OrderId, PaymentStatus.Paid.ToString());
        await notificationClient.SendAsync(payment.CustomerId, "Thanh toán thành công",
            $"Đơn hàng {payment.OrderId} đã được thanh toán.", "PaymentSucceeded");
        return Ok(Map(payment));
    }

    [HttpPost("{id:guid}/fail")]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<ActionResult<PaymentResponse>> Fail(Guid id)
    {
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.Id == id);
        if (payment is null) return NotFound();
        if (payment.CustomerId != User.GetRequiredUserId()) return Forbid();
        if (payment.Status == PaymentStatus.Paid || payment.Status == PaymentStatus.Refunded)
            return Conflict(new { message = "Không thể đánh dấu thất bại sau khi đã thanh toán/hoàn tiền." });
        payment.Status = PaymentStatus.Failed;
        await db.SaveChangesAsync();
        await ordersClient.SetPaymentStatusAsync(payment.OrderId, PaymentStatus.Failed.ToString());
        return Ok(Map(payment));
    }

    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin,ShopOwner")]
    public async Task<ActionResult<PaymentResponse>> Refund(Guid id)
    {
        var payment = await db.Payments.SingleOrDefaultAsync(x => x.Id == id);
        if (payment is null) return NotFound();
        if (!User.IsInRole("Admin"))
        {
            var order = await ordersClient.GetAsync(payment.OrderId);
            var ownedShop = await shopClient.GetByOwnerAsync(User.GetRequiredUserId());
            if (order is null || ownedShop?.Id != order.ShopId) return Forbid();
        }
        if (payment.Status != PaymentStatus.Paid) return Conflict(new { message = "Chỉ thanh toán Paid mới được hoàn tiền." });
        payment.Status = PaymentStatus.Refunded;
        payment.RefundedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await ordersClient.SetPaymentStatusAsync(payment.OrderId, PaymentStatus.Refunded.ToString());
        await notificationClient.SendAsync(payment.CustomerId, "Đã hoàn tiền",
            $"Thanh toán của đơn {payment.OrderId} đã được hoàn tiền.", "PaymentRefunded");
        return Ok(Map(payment));
    }

    private static PaymentResponse Map(Payment x) => new(x.Id, x.OrderId, x.CustomerId,
        x.Amount, x.Method, x.Status, x.TransactionCode, x.CreatedAt, x.PaidAt, x.RefundedAt);
}
