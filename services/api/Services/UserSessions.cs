using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;

namespace SistemasPrecios.Api.Services;

public static class UserSessions
{
    public static bool Enabled(User user) => user.Role is UserRole.Admin or UserRole.Analyst or UserRole.Viewer;
    private static string CredentialVersion(User user) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.PasswordHash)));

    public static ClaimsPrincipal Principal(User user) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("credential_version", CredentialVersion(user))
    }, CookieAuthenticationDefaults.AuthenticationScheme));

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var db=context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        User? user=null;
        if(Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),out var id))
            user=await db.Users.AsNoTracking().SingleOrDefaultAsync(u=>u.Id==id,context.HttpContext.RequestAborted);
        if(user is null || !Enabled(user) || context.Principal?.FindFirstValue("credential_version")!=CredentialVersion(user))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }
        // Permissions are refreshed from the database, including previously issued cookies.
        if(context.Principal!.FindFirstValue(ClaimTypes.Role)!=user.Role.ToString() || context.Principal.FindFirstValue(ClaimTypes.Name)!=user.FullName)
        {
            context.ReplacePrincipal(Principal(user));
            context.ShouldRenew=true;
        }
    }
}
