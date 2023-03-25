using ActiverWebAPI.Interfaces.Service;
using System.Collections;
using System.Security.Cryptography;

namespace ActiverWebAPI.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string HashPassword(string password)
    {
        using var algorithm = new Rfc2898DeriveBytes(
            password,
            SaltSize,
            10000,
            HashAlgorithmName.SHA256
        );

        var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
        var salt = Convert.ToBase64String(algorithm.Salt);

        return $"{10000}.{salt}.{key}";
    }

    public bool VerifyHashedPassword(string hashedPassword, string password)
    {
        var parts = hashedPassword.Split('.');
        var iterations = int.Parse(parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var key = Convert.FromBase64String(parts[2]);

        using var algorithm = new Rfc2898DeriveBytes(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256
        );

        var keyToCheck = algorithm.GetBytes(KeySize);

        return StructuralComparisons.StructuralEqualityComparer.Equals(keyToCheck, key);
    }
}