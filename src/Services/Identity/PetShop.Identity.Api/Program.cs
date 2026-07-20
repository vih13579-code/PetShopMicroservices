using Microsoft.EntityFrameworkCore;
using PetShop.Identity.Api.Application;
using PetShop.Identity.Api.Infrastructure;
using PetShop.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPetShopApi(builder.Configuration, "PetShop Identity API");
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDb")));
builder.Services.AddScoped<TokenService>();

var app = builder.Build();
app.UsePetShopApi();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await IdentitySeeder.SeedAsync(db, app.Configuration);
}

app.Run();
