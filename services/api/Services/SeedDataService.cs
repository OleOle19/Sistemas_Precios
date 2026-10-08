using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
namespace SistemasPrecios.Api.Services;
public static class SeedDataService
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await db.Users.AnyAsync()) return;
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var password = config["Bootstrap:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("Configura Bootstrap:Password con al menos 12 caracteres para crear tu cuenta local. Consulta docs/local-run.md.");
        db.Users.Add(new User
        {
            FullName = config["Bootstrap:Name"] ?? "Administrador",
            Email = (config["Bootstrap:Email"] ?? "admin@precios.local").Trim().ToLowerInvariant(),
            PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordService>().HashPassword(password),
            Role = UserRole.Admin
        });
        await db.SaveChangesAsync();
    }
}
