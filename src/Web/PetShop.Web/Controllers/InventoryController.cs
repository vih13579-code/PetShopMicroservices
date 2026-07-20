using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class InventoryController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var result=await api.GetAsync<IReadOnlyCollection<InventoryVm>>("api/inventory/owner"); ViewBag.Error=result.Error; return View(result.Data??[]);
    }
    [HttpPost]
    public async Task<IActionResult> Set(Guid productId,int quantity,string? reason)
    {
        var r=await api.PutAsync<InventoryVm>("api/inventory/owner/set",new{productId,quantity,reason}); TempData[r.Success?"Success":"Error"]=r.Success?"Đã cập nhật kho.":r.Error; return RedirectToAction("Index");
    }
}
