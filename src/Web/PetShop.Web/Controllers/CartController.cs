using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class CartController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var result = await api.GetAsync<CartVm>("api/cart"); ViewBag.Error = result.Error;
        return View(result.Data ?? new CartVm(Guid.Empty, Guid.Empty, [], 0));
    }

    [HttpPost]
    public async Task<IActionResult> Add(Guid productId, Guid? variantId, int quantity = 1)
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        var result = await api.PostAsync<CartVm>("api/cart/items", new { productId, variantId, quantity });
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Đã thêm vào giỏ hàng." : result.Error;
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(Guid itemId)
    {
        await api.DeleteAsync<object>($"api/cart/items/{itemId}"); return RedirectToAction("Index");
    }

    [HttpGet] public IActionResult Checkout() => View(new CheckoutVm());
    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<IReadOnlyCollection<OrderVm>>("api/orders/checkout", model);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error ?? "Không thể đặt hàng."); return View(model); }
        TempData["Success"] = "Đặt hàng thành công."; return RedirectToAction("Index", "Orders");
    }
}
