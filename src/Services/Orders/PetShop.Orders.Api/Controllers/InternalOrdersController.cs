using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Orders.Api.Contracts;
using PetShop.Orders.Api.Domain;
using PetShop.Orders.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Orders.Api.Controllers;

[ApiController]
[Route("internal/orders")]
public sealed class InternalOrdersController(OrdersDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InternalOrderSummary>> Get(Guid id)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return order is null ? NotFound() : Ok(new InternalOrderSummary(order.Id, order.OrderCode,
            order.CustomerId, order.ShopId, order.TotalAmount, order.Status.ToString(),
            order.PaymentStatus.ToString(), order.PaymentMethod.ToString()));
    }

    [HttpGet("customers/{customerId:guid}/purchased/{productId:guid}")]
    public async Task<IActionResult> HasPurchased(Guid customerId, Guid productId)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        var purchased = await db.Orders.AsNoTracking()
            .AnyAsync(x => x.CustomerId == customerId && x.Status == OrderStatus.Completed
                && x.Items.Any(i => i.ProductId == productId));
        return Ok(new { purchased });
    }

    [HttpPatch("{id:guid}/payment-status/{status}")]
    public async Task<IActionResult> UpdatePaymentStatus(Guid id, string status)
    {
        if (!Request.HasValidInternalKey(configuration)) return Unauthorized();
        if (!Enum.TryParse<PaymentStatus>(status, true, out var parsed)) return BadRequest(new { message = "PaymentStatus không hợp lệ." });
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        order.PaymentStatus = parsed; order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return NoContent();
    }
}
