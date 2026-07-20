using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Orders.Api.Application;
using PetShop.Orders.Api.Domain;
using PetShop.Orders.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Orders.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/reports")]
public sealed class ReportsController(OrdersDbContext db, ShopClient shopClient) : ControllerBase
{
    [HttpGet("owner-revenue")]
    [Authorize(Roles = "ShopOwner")]
    public async Task<IActionResult> OwnerRevenue(DateTime? from, DateTime? to)
    {
        var shop = await shopClient.GetByOwnerAsync(User.GetRequiredUserId())
                   ?? throw new KeyNotFoundException("Không tìm thấy Shop.");
        return Ok(await BuildAsync(db.Orders.Where(x => x.ShopId == shop.Id), from, to));
    }

    [HttpGet("system-revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SystemRevenue(DateTime? from, DateTime? to)
        => Ok(await BuildAsync(db.Orders.AsQueryable(), from, to));

    private static async Task<object> BuildAsync(IQueryable<Order> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt < to.Value.AddDays(1));
        var totalOrders = await query.CountAsync();
        var completed = await query.CountAsync(x => x.Status == OrderStatus.Completed);
        var cancelled = await query.CountAsync(x => x.Status == OrderStatus.Cancelled);
        var revenue = await query.Where(x => x.Status == OrderStatus.Completed).SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
        var byDay = await query.Where(x => x.Status == OrderStatus.Completed)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new { date = g.Key, orders = g.Count(), revenue = g.Sum(x => x.TotalAmount) })
            .OrderBy(x => x.date).ToListAsync();
        return new { totalOrders, completed, cancelled, revenue, byDay };
    }
}
