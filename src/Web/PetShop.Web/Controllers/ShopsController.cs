using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class ShopsController(GatewayApiClient api) : Controller
{
    public async Task<IActionResult> Manage()
    {
        var r=await api.GetAsync<PagedVm<ShopVm>>("api/shops?page=1&pageSize=100");ViewBag.Error=r.Error;return View(r.Data?.Items??[]);
    }
    [HttpPost] public async Task<IActionResult> Lock(Guid id){await api.PatchAsync<object>($"api/shops/{id}/lock");return RedirectToAction("Manage");}
    [HttpPost] public async Task<IActionResult> Unlock(Guid id){await api.PatchAsync<object>($"api/shops/{id}/unlock");return RedirectToAction("Manage");}
    public async Task<IActionResult> Mine()
    {
        var r = await api.GetAsync<ShopVm>("api/shops/mine");
        ViewBag.Error = r.Error;
        if (r.Data is null) return View((ShopEditVm?)null);
        return View(new ShopEditVm { Id = r.Data.Id, OwnerUserId = r.Data.OwnerUserId, Name = r.Data.Name, Description = r.Data.Description, Phone = r.Data.Phone, Email = r.Data.Email, Address = r.Data.Address, TaxCode = r.Data.TaxCode });
    }
    [HttpPost]
    public async Task<IActionResult> Mine(ShopEditVm model)
    {
        var r=await api.PutAsync<ShopVm>("api/shops/mine",new{name=model.Name,model.Description,model.Phone,model.Email,model.Address,model.TaxCode});
        TempData[r.Success?"Success":"Error"]=r.Success?"Đã cập nhật Shop.":r.Error;return RedirectToAction("Mine");
    }
}
