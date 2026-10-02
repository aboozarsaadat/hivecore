using System.Security.Cryptography;

namespace HiveCore.Services.Helpers;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
    bool NeedsRehash(string hashedPassword);
}

/// <summary>
/// Password hasher using PBKDF2 (HMAC-SHA256) with a per-password salt.
/// Note: For Identity-based flows we rely on ASP.NET Core Identity's own hasher.
/// This helper is for non-Identity passwords (e.g., API keys, share secrets).
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;      // 128-bit salt
    private const int KeySize = 32;       // 256-bit subkey
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var key = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: Algorithm,
            outputLength: KeySize);

        // Format: iterations.salt.key (Base64)
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
            return false;

        var parts = hashedPassword.Split('.', 3);
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var iterations) || iterations <= 0)
            return false;

        byte[] salt, key;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            key = Convert.FromBase64String(parts[2]);
        }
        catch
        {
            return false;
        }

        var candidate = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: Algorithm,
            outputLength: key.Length);

        return CryptographicOperations.FixedTimeEquals(candidate, key);
    }

    public bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword)) return true;

        var parts = hashedPassword.Split('.', 3);
        if (parts.Length != 3) return true;

        if (!int.TryParse(parts[0], out var iterations)) return true;

        return iterations < Iterations;
    }
}