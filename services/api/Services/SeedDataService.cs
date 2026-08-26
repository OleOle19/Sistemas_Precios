using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;

namespace SistemasPrecios.Api.Services;

public static class SeedDataService
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User
                {
                    FullName = "Administrador Demo",
                    Email = "admin@precios.local",
                    PasswordHash = passwordService.HashPassword("Admin123!"),
                    Role = UserRole.Admin
                },
                new User
                {
                    FullName = "Analista Demo",
                    Email = "analyst@precios.local",
                    PasswordHash = passwordService.HashPassword("Analyst123!"),
                    Role = UserRole.Analyst
                });
        }

        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.AddRange(
                new Supplier { Name = "Distribuidora Andina", ContactEmail = "andina@proveedores.local" },
                new Supplier { Name = "Mercado Norte", ContactEmail = "norte@proveedores.local" },
                new Supplier { Name = "Proveedor Central", ContactEmail = "central@proveedores.local" });
        }

        if (!await db.CanonicalProducts.AnyAsync())
        {
            var aceite = new CanonicalProduct
            {
                Name = "Aceite vegetal 1L",
                BaseUnit = "L",
                Aliases =
                [
                    new ProductAlias { Alias = "aceite vegetal 1l" },
                    new ProductAlias { Alias = "aceite 1 litro" }
                ]
            };

            var arroz = new CanonicalProduct
            {
                Name = "Arroz extra 5kg",
                BaseUnit = "KG",
                Aliases =
                [
                    new ProductAlias { Alias = "arroz extra 5kg" },
                    new ProductAlias { Alias = "arroz x 5kg" }
                ]
            };

            var azucar = new CanonicalProduct
            {
                Name = "Azucar rubia 1kg",
                BaseUnit = "KG",
                Aliases =
                [
                    new ProductAlias { Alias = "azucar rubia 1kg" }
                ]
            };

            db.CanonicalProducts.AddRange(aceite, arroz, azucar);
        }

        await db.SaveChangesAsync();
    }
}
