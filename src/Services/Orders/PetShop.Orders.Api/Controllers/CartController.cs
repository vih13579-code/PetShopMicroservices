using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Orders.Api.Application;
using PetShop.Orders.Api.Contracts;
using PetShop.Orders.Api.Domain;
using PetShop.Orders.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Orders.Api.Controllers;

[ApiController]
[Authorize(Roles = "Customer,ShopOwner")]
[Route("api/cart")]
public sealed class CartController(OrdersDbContext db, CatalogClient catalogClient) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartResponse>> Get()
    {
        var cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: true);
        return Ok(Map(cart));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartResponse>> Add(AddCartItemRequest request)
    {
        var snapshot = await catalogClient.GetProductAsync(request.ProductId, request.VariantId);
        if (snapshot is null || !snapshot.IsActive) return BadRequest(new { message = "Sản phẩm hoặc phân loại không tồn tại/đã ngừng bán." });
        var cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: false);
        var existing = cart.Items.SingleOrDefault(x => x.ProductId == request.ProductId && x.VariantId == request.VariantId);
        if (existing is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = snapshot.Id, VariantId = snapshot.VariantId, ShopId = snapshot.ShopId,
                ProductName = snapshot.Name, VariantName = snapshot.VariantName, Sku = snapshot.Sku,
                ImageUrl = snapshot.ImageUrl, UnitPrice = snapshot.UnitPrice, Quantity = request.Quantity
            });
        }
        else
        {
            existing.Quantity = Math.Min(999, existing.Quantity + request.Quantity);
            existing.UnitPrice = snapshot.UnitPrice;
        }
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return Ok(Map(cart));
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<ActionResult<CartResponse>> Update(Guid itemId, UpdateCartItemRequest request)
    {
        var cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: true);
        var item = cart.Items.SingleOrDefault(x => x.Id == itemId);
        if (item is null) return NotFound();
        item.Quantity = request.Quantity; cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return Ok(Map(cart));
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> Remove(Guid itemId)
    {
        var cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: true);
        var item = cart.Items.SingleOrDefault(x => x.Id == itemId);
        if (item is null) return NotFound();
        db.CartItems.Remove(item); cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        var cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: true);
        db.CartItems.RemoveRange(cart.Items); cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return NoContent();
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid customerId, bool saveIfNew)
    {
        var cart = await db.Carts.Include(x => x.Items).SingleOrDefaultAsync(x => x.CustomerId == customerId);
        if (cart is not null) return cart;
        cart = new Cart { CustomerId = customerId };
        db.Carts.Add(cart);
        if (saveIfNew) await db.SaveChangesAsync();
        return cart;
    }

    internal static CartResponse Map(Cart x) => new(x.Id, x.CustomerId,
        x.Items.Select(i => new CartItemResponse(i.Id, i.ProductId, i.VariantId, i.ShopId,
            i.ProductName, i.VariantName, i.Sku, i.ImageUrl, i.UnitPrice, i.Quantity,
            i.UnitPrice * i.Quantity)).ToArray(), x.Items.Sum(i => i.UnitPrice * i.Quantity));
}
