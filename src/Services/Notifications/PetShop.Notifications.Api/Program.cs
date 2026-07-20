using Microsoft.EntityFrameworkCore;
using PetShop.Notifications.Api.Infrastructure;
using PetShop.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPetShopApi(builder.Configuration, "PetShop Notifications API");
builder.Services.AddDbContext<NotificationsDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("NotificationsDb")));
var app = builder.Build(); app.UsePetShopApi();
using (var scope = app.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.EnsureCreatedAsync();
app.Run();
