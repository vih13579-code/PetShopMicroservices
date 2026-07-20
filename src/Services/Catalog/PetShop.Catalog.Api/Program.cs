using Microsoft.EntityFrameworkCore;
using PetShop.Catalog.Api.Application;
using PetShop.Catalog.Api.Infrastructure;
using PetShop.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPetShopApi(builder.Configuration, "PetShop Catalog API");
builder.Services.AddDbContext<CatalogDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("CatalogDb")));
builder.Services.AddHttpClient<ShopClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Shops"]!));
builder.Services.AddHttpClient<OrdersClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Orders"]!));
var app = builder.Build(); app.UsePetShopApi();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.EnsureCreatedAsync();
app.Run();
