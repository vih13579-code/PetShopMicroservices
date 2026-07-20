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
public sealed class CartController(OrdersDbContext db, CatalogClient catalogClient, ShopClient shopClient, InventoryClient inventoryClient) : ControllerBase
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
        if (snapshot is null || !snapshot.IsActive)
            return BadRequest(new { message = "Sản phẩm hoặc phân loại không tồn tại hoặc đã ngừng bán." });

        if (User.IsInRole("ShopOwner"))
        {
            var ownedShop = await shopClient.GetByOwnerAsync(User.GetRequiredUserId());
            if (ownedShop is not null && snapshot.ShopId == ownedShop.Id)
                return BadRequest(new { message = "Chủ cửa hàng không thể tự thêm sản phẩm của Shop mình vào giỏ hàng." });
        }

        var availableStock = await inventoryClient.GetAvailableQuantityAsync(request.ProductId);
        if (availableStock <= 0)
            return BadRequest(new { message = $"Sản phẩm '{snapshot.Name}' hiện đã hết hàng trong kho." });

        var cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: true);
        var existing = await db.CartItems.FirstOrDefaultAsync(x => x.CartId == cart.Id && x.ProductId == request.ProductId && x.VariantId == request.VariantId);
        var existingQuantity = existing?.Quantity ?? 0;
        var totalRequested = existingQuantity + request.Quantity;

        if (totalRequested > availableStock)
            return BadRequest(new { message = $"Sản phẩm '{snapshot.Name}' chỉ còn {availableStock} trong kho (bạn đã có {existingQuantity} trong giỏ)." });

        if (existing is null)
        {
            db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = snapshot.Id,
                VariantId = snapshot.VariantId,
                ShopId = snapshot.ShopId,
                ProductName = snapshot.Name,
                VariantName = snapshot.VariantName,
                Sku = snapshot.Sku,
                ImageUrl = snapshot.ImageUrl,
                UnitPrice = snapshot.UnitPrice,
                Quantity = request.Quantity
            });
        }
        else
        {
            existing.Quantity = Math.Min(999, totalRequested);
            existing.UnitPrice = snapshot.UnitPrice;
        }
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: true);
        return Ok(Map(cart));
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<ActionResult<CartResponse>> Update(Guid itemId, UpdateCartItemRequest request)
    {
        if (request.Quantity <= 0) return BadRequest(new { message = "Số lượng sản phẩm phải lớn hơn 0." });
        var cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: true);
        var item = cart.Items.FirstOrDefault(x => x.Id == itemId);
        if (item is null) return NotFound();

        var availableStock = await inventoryClient.GetAvailableQuantityAsync(item.ProductId);
        if (request.Quantity > availableStock)
            return BadRequest(new { message = $"Sản phẩm '{item.ProductName}' chỉ còn {availableStock} trong kho." });

        item.Quantity = Math.Min(999, request.Quantity);
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(Map(cart));
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> Remove(Guid itemId)
    {
        var cart = await GetOrCreateCartAsync(User.GetRequiredUserId(), saveIfNew: true);
        var item = cart.Items.FirstOrDefault(x => x.Id == itemId);
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
        var cart = await db.Carts.Include(x => x.Items).FirstOrDefaultAsync(x => x.CustomerId == customerId);
        if (cart is not null) return cart;

        try
        {
            cart = new Cart { CustomerId = customerId };
            db.Carts.Add(cart);
            if (saveIfNew) await db.SaveChangesAsync();
            return cart;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return await db.Carts.Include(x => x.Items).FirstAsync(x => x.CustomerId == customerId);
        }
    }

    internal static CartResponse Map(Cart x) => new(x.Id, x.CustomerId,
        x.Items.Select(i => new CartItemResponse(i.Id, i.ProductId, i.VariantId, i.ShopId,
            i.ProductName, i.VariantName, i.Sku, i.ImageUrl, i.UnitPrice, i.Quantity,
            i.UnitPrice * i.Quantity)).ToArray(), x.Items.Sum(i => i.UnitPrice * i.Quantity));
}
