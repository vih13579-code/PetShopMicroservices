using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetShop.Identity.Api.Domain;

namespace PetShop.Identity.Api.Infrastructure;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IdentityDbContext db, IConfiguration configuration)
    {
        await db.Database.EnsureCreatedAsync();

        foreach (var roleName in AppRoles.All)
        {
            if (!await db.Roles.AnyAsync(x => x.Name == roleName))
            {
                db.Roles.Add(new Role { Name = roleName });
            }
        }
        await db.SaveChangesAsync();

        await EnsureUserAsync(db, configuration["SeedAdmin:Email"] ?? "admin@petshop.local",
            configuration["SeedAdmin:Password"] ?? "Admin@123",
            configuration["SeedAdmin:FullName"] ?? "PetShop Administrator", AppRoles.Admin);

        await EnsureUserAsync(db, configuration["SeedStaff:Email"] ?? "staff@petshop.local",
            configuration["SeedStaff:Password"] ?? "Staff@123",
            configuration["SeedStaff:FullName"] ?? "PetShop Staff", AppRoles.Staff);

        await EnsureUserAsync(db, configuration["SeedCustomer:Email"] ?? "customer@petshop.local",
            configuration["SeedCustomer:Password"] ?? "Customer@123",
            configuration["SeedCustomer:FullName"] ?? "PetShop Customer", AppRoles.Customer);
    }

    private static async Task EnsureUserAsync(IdentityDbContext db, string email, string password, string fullName, string roleName)
    {
        email = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email)) return;
        var user = new User { FullName = fullName, Email = email, IsActive = true };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        var role = await db.Roles.SingleAsync(x => x.Name == roleName);
        user.UserRoles.Add(new UserRole { User = user, Role = role });
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
