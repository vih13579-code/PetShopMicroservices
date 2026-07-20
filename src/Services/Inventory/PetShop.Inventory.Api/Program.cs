using Microsoft.EntityFrameworkCore;
using PetShop.Inventory.Api.Application;
using PetShop.Inventory.Api.Infrastructure;
using PetShop.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPetShopApi(builder.Configuration, "PetShop Inventory API");
builder.Services.AddDbContext<InventoryDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("InventoryDb")));
builder.Services.AddHttpClient<ShopClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Shops"]!));
var app = builder.Build(); app.UsePetShopApi();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.EnsureCreatedAsync();
app.Run();
