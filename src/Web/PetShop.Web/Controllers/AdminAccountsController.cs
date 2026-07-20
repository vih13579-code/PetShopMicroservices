using Microsoft.AspNetCore.Mvc;
using PetShop.Web.Models;
using PetShop.Web.Services;

namespace PetShop.Web.Controllers;

public sealed class AdminAccountsController(GatewayApiClient api) : Controller
{
    private IActionResult? CheckAdminRole()
    {
        if (!api.IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!api.IsInRole("Admin"))
        {
            TempData["Error"] = "Bạn không có quyền truy cập vào khu vực Admin.";
            return RedirectToAction("Index", "Dashboard");
        }
        return null;
    }

    public async Task<IActionResult> Index(string? keyword, string? role, bool? isActive)
    {
        var guard = CheckAdminRole(); if (guard != null) return guard;
        var url = "api/admin/accounts?page=1&pageSize=100";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (!string.IsNullOrWhiteSpace(role)) url += $"&role={Uri.EscapeDataString(role)}";
        if (isActive.HasValue) url += $"&isActive={isActive.Value}";

        var result = await api.GetAsync<PagedVm<UserVm>>(url);
        ViewBag.Error = result.Error;
        ViewBag.Keyword = keyword;
        ViewBag.Role = role;
        ViewBag.IsActive = isActive;
        return View(result.Data?.Items ?? []);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var guard = CheckAdminRole(); if (guard != null) return guard;
        var result = await api.GetAsync<UserVm>($"api/admin/accounts/{id}");
        return !result.Success || result.Data is null ? NotFound() : View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var guard = CheckAdminRole(); if (guard != null) return guard;
        var result = await api.GetAsync<UserVm>($"api/admin/accounts/{id}");
        if (!result.Success || result.Data is null) return NotFound();

        var model = new ProfileFormVm
        {
            Id = result.Data.Id,
            Email = result.Data.Email,
            FullName = result.Data.FullName,
            Phone = result.Data.Phone,
            Address = result.Data.Address,
            Roles = result.Data.Roles,
            CreatedAt = result.Data.CreatedAt
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, ProfileFormVm model)
    {
        var guard = CheckAdminRole(); if (guard != null) return guard;
        if (!ModelState.IsValid) return View(model);

        var result = await api.PutAsync<UserVm>($"api/admin/accounts/{id}", new
        {
            fullName = model.FullName.Trim(),
            phone = model.Phone?.Trim(),
            address = model.Address?.Trim()
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể cập nhật tài khoản.");
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật thông tin tài khoản.";
        return RedirectToAction("Details", new { id });
    }

    [HttpGet]
    public IActionResult CreateStaff()
    {
        var guard = CheckAdminRole(); if (guard != null) return guard;
        return View(new StaffFormVm());
    }

    [HttpPost]
    public async Task<IActionResult> CreateStaff(StaffFormVm model)
    {
        var guard = CheckAdminRole(); if (guard != null) return guard;
        if (!ModelState.IsValid) return View(model);
        var result = await api.PostAsync<UserVm>("api/admin/accounts/staff", model);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Error ?? "Không thể tạo Staff."); return View(model); }
        TempData["Success"] = "Đã tạo tài khoản Staff.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Lock(Guid id)
    {
        var guard = CheckAdminRole(); if (guard != null) return guard;
        await api.PatchAsync<object>($"api/admin/accounts/{id}/lock");
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Unlock(Guid id)
    {
        var guard = CheckAdminRole(); if (guard != null) return guard;
        await api.PatchAsync<object>($"api/admin/accounts/{id}/unlock");
        return RedirectToAction("Index");
    }
}
