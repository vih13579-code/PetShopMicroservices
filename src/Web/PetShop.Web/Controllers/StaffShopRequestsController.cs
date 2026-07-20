using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class StaffShopRequestsController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<PagedVm<ShopRequestVm>>("api/shop-requests?page=1&pageSize=100");
        ViewBag.Error = result.Error; return View(result.Data?.Items ?? []);
    }
    [HttpPost] public async Task<IActionResult> Approve(Guid id) { var r=await api.PostAsync<ShopVm>($"api/shop-requests/{id}/approve"); TempData[r.Success?"Success":"Error"]=r.Success?"Đã duyệt Shop.":r.Error; return RedirectToAction("Index"); }
    [HttpPost] public async Task<IActionResult> Reject(Guid id, string reason) { var r=await api.PostAsync<object>($"api/shop-requests/{id}/reject",new{reason}); TempData[r.Success?"Success":"Error"]=r.Success?"Đã từ chối.":r.Error; return RedirectToAction("Index"); }
}
