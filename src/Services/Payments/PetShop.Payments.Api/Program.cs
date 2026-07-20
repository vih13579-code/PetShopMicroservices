using Microsoft.EntityFrameworkCore;
using PetShop.Payments.Api.Application;
using PetShop.Payments.Api.Infrastructure;
using PetShop.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPetShopApi(builder.Configuration, "PetShop Payments API");
builder.Services.AddDbContext<PaymentsDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsDb")));
builder.Services.AddHttpClient<OrdersClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Orders"]!));
builder.Services.AddHttpClient<NotificationClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Notifications"]!));
builder.Services.AddHttpClient<ShopClient>(c => c.BaseAddress = new Uri(builder.Configuration["Services:Shops"]!));
var app = builder.Build(); app.UsePetShopApi();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.EnsureCreatedAsync();
app.Run();
