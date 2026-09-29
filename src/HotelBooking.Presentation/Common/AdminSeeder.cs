using HotelBooking.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Presentation.Common;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var email = Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL") ?? "admin@hotelbooking.com";
        var userName = Environment.GetEnvironmentVariable("SEED_ADMIN_USERNAME") ?? "admin";
        var password = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD") ?? "Admin1234";

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new ApplicationRole("Admin"));

        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "Admin");
    }
}
