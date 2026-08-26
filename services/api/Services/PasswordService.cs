using System.Security.Cryptography;
using System.Text;

namespace SistemasPrecios.Api.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

public sealed class PasswordService : IPasswordService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 120_000;

    public string HashPassword(string password)
    {
        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            salt.ToArray(),
            Iterations,
            HashAlgorithmName.SHA256);

        var hash = deriveBytes.GetBytes(HashSize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);

        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256);

        var actualHash = deriveBytes.GetBytes(HashSize);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
