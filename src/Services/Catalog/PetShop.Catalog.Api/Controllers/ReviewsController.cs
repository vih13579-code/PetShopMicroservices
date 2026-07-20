using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetShop.Catalog.Api.Application;
using PetShop.Catalog.Api.Contracts;
using PetShop.Catalog.Api.Domain;
using PetShop.Catalog.Api.Infrastructure;
using PetShop.ServiceDefaults;

namespace PetShop.Catalog.Api.Controllers;

[ApiController]
[Route("api/catalog/products/{productId:guid}/reviews")]
public sealed class ReviewsController(CatalogDbContext db, OrdersClient ordersClient) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ReviewResponse>>> GetAll(Guid productId)
    {
        var items = await db.ProductReviews.AsNoTracking()
            .Where(x => x.ProductId == productId && x.IsVisible)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        return Ok(items.Select(Map));
    }

    [HttpPost]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<ActionResult<ReviewResponse>> Create(Guid productId, ReviewRequest request)
    {
        var userId = User.GetRequiredUserId();
        if (!await db.Products.AnyAsync(x => x.Id == productId && x.IsActive)) return NotFound();
        if (!await ordersClient.HasPurchasedAsync(userId, productId))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Chỉ khách đã mua và hoàn thành đơn mới được đánh giá." });
        if (await db.ProductReviews.AnyAsync(x => x.ProductId == productId && x.UserId == userId))
            return Conflict(new { message = "Bạn đã đánh giá sản phẩm này." });
        var entity = new ProductReview { ProductId = productId, UserId = userId, Rating = request.Rating, Comment = request.Comment?.Trim() };
        db.ProductReviews.Add(entity); await db.SaveChangesAsync();
        return Ok(Map(entity));
    }

    [HttpPut("mine")]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<ActionResult<ReviewResponse>> UpdateMine(Guid productId, ReviewRequest request)
    {
        var userId = User.GetRequiredUserId();
        var entity = await db.ProductReviews.SingleOrDefaultAsync(x => x.ProductId == productId && x.UserId == userId);
        if (entity is null) return NotFound();
        entity.Rating = request.Rating; entity.Comment = request.Comment?.Trim(); entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return Ok(Map(entity));
    }

    [HttpDelete("mine")]
    [Authorize(Roles = "Customer,ShopOwner")]
    public async Task<IActionResult> DeleteMine(Guid productId)
    {
        var userId = User.GetRequiredUserId();
        var entity = await db.ProductReviews.SingleOrDefaultAsync(x => x.ProductId == productId && x.UserId == userId);
        if (entity is null) return NotFound();
        db.ProductReviews.Remove(entity); await db.SaveChangesAsync(); return NoContent();
    }

    private static ReviewResponse Map(ProductReview x) => new(x.Id, x.ProductId, x.UserId, x.Rating, x.Comment, x.CreatedAt, x.UpdatedAt);
}
