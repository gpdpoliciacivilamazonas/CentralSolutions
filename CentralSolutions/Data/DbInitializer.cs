using CentralSolutions.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CentralSolutions.Data;

public static class DbInitializer
{
  public static async Task InitializeAsync(
    ApplicationDbContext context,
    IPasswordHasher<User> passwordHasher)
  {
    await context.Database.MigrateAsync();

    if (await context.Users.AnyAsync())
    {
      return;
    }

    var username = Environment.GetEnvironmentVariable("APP_USERNAME");
    var password = Environment.GetEnvironmentVariable("APP_PASSWORD");

    var user = new User
    {
      Email = username,
      IsActive = true
    };

    user.PasswordHash = passwordHasher.HashPassword(
      user,
      password);

    context.Users.Add(user);

    await context.SaveChangesAsync();
  }
}
