using System.Data;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
using SistemasPrecios.Api.Dtos;

namespace SistemasPrecios.Api.Services;

public sealed class TeamService(ApplicationDbContext db, IPasswordService passwords)
{
    public async Task<TeamUserResponse> CreateAsync(CreateTeamUserRequest request,CancellationToken ct)
    {
        var email=request.Email?.Trim().ToLowerInvariant();
        if(string.IsNullOrWhiteSpace(request.FullName)||request.FullName.Trim().Length>150 || string.IsNullOrWhiteSpace(email)||email.Length>250 || !MailAddress.TryCreate(email,out var address)||address.Address!=email)
            throw new ArgumentException("Completa un nombre de hasta 150 caracteres y un correo válido.");
        ValidatePassword(request.Password);
        var role=ParseRole(request.Role);
        if(role==UserRole.Disabled) throw new ArgumentException("Crea la cuenta con un rol habilitado.");
        if(await db.Users.AnyAsync(u=>u.Email==email,ct)) throw new ArgumentException("Ya existe una cuenta con ese correo.");
        var user=new User { FullName=request.FullName.Trim(),Email=email,PasswordHash=passwords.HashPassword(request.Password),Role=role };
        db.Users.Add(user); await db.SaveChangesAsync(ct); return Response(user);
    }

    public async Task<TeamUserResponse> ChangeRoleAsync(Guid actorId,Guid targetId,string roleName,CancellationToken ct)
    {
        if(actorId==targetId) throw new ArgumentException("No puedes cambiar el acceso de tu propia cuenta desde Equipo.");
        var role=ParseRole(roleName);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var actor=await db.Users.SingleOrDefaultAsync(u=>u.Id==actorId,ct);
        if(actor?.Role!=UserRole.Admin) throw new UnauthorizedAccessException();
        var user=await db.Users.SingleOrDefaultAsync(u=>u.Id==targetId,ct)??throw new ArgumentException("No se encontró esa cuenta.");
        user.Role=role; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Response(user);
    }

    public async Task ChangePasswordAsync(Guid userId,ChangePasswordRequest request,CancellationToken ct)
    {
        ValidatePassword(request.NewPassword);
        var user=await db.Users.SingleOrDefaultAsync(u=>u.Id==userId,ct);
        if(user is null || !UserSessions.Enabled(user) || string.IsNullOrWhiteSpace(request.CurrentPassword)||request.CurrentPassword.Length>500||!passwords.VerifyPassword(request.CurrentPassword,user.PasswordHash))
            throw new ArgumentException("La contraseña actual no coincide.");
        if(passwords.VerifyPassword(request.NewPassword,user.PasswordHash)) throw new ArgumentException("Elige una contraseña diferente de la actual.");
        user.PasswordHash=passwords.HashPassword(request.NewPassword); await db.SaveChangesAsync(ct);
    }

    public static void ValidatePassword(string? password)
    {
        if(string.IsNullOrWhiteSpace(password)||password.Length<12||password.Length>200) throw new ArgumentException("La contraseña debe tener entre 12 y 200 caracteres.");
    }
    private static UserRole ParseRole(string? name)
    {
        if(!Enum.TryParse<UserRole>(name,out var role)||!Enum.IsDefined(role)||name!=role.ToString()) throw new ArgumentException("Elige un rol válido.");
        return role;
    }
    public static TeamUserResponse Response(User user) => new(user.Id,user.FullName,user.Email,user.Role.ToString());
}
