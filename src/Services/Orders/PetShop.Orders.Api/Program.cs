using Microsoft.EntityFrameworkCore;
using PetShop.Orders.Api.Application;
using PetShop.Orders.Api.Infrastructure;
using PetShop.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPetShopApi(builder.Configuration, "PetShop Orders API");
builder.Services.AddDbContext<OrdersDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("OrdersDb")));
builder.Services.AddHttpClient<CatalogClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Catalog"]!));
builder.Services.AddHttpClient<InventoryClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Inventory"]!));
builder.Services.AddHttpClient<ShopClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Shops"]!));
builder.Services.AddHttpClient<NotificationClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Notifications"]!));
var app = builder.Build(); app.UsePetShopApi();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.EnsureCreatedAsync();
app.Run();
