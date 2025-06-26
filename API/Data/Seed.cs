namespace API.Data;

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using API.DataEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

[ExcludeFromCodeCoverage]
public class Seed
{
    public static async Task SeedUsersAsync(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");
        var users = JsonSerializer.Deserialize<List<AppUser>>(userData, ReadOptions);

        if (users == null)
        {
            return;
        }

        var roles = new List<AppRole>
        {
            new() { Name = "Admin" },
            new() { Name = "Member" },
            new() { Name = "Moderator" }
        };

        foreach (var role in roles)
        {
            await roleManager.CreateAsync(role);
        }

        var admin = new AppUser
        {
            UserName = "admin",
            KnownAs = "Admin",
            Gender = string.Empty,
            City = string.Empty,
            Country = string.Empty
        };

        await userManager.CreateAsync(admin, "Pa$$w0rd");
        await userManager.AddToRolesAsync(admin, ["Admin", "Moderator"]);


        foreach (var user in users)
        {
            user.UserName = user.UserName!.ToLowerInvariant();
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "Member");
        }
    }

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        AllowTrailingCommas = true
    };
}
