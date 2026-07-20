using Microsoft.EntityFrameworkCore;
using PetShop.ServiceDefaults;
using PetShop.Shops.Api.Application;
using PetShop.Shops.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPetShopApi(builder.Configuration, "PetShop Shops API");
builder.Services.AddDbContext<ShopsDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("ShopsDb")));
builder.Services.AddHttpClient<IdentityClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Identity"]!));
builder.Services.AddHttpClient<NotificationClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Notifications"]!));
var app = builder.Build();
app.UsePetShopApi();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<ShopsDbContext>().Database.EnsureCreatedAsync();
app.Run();
