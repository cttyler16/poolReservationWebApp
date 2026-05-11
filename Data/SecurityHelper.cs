using System.Security.Cryptography;
using System.Text;

namespace PoolReservationWeb.Data;

public static class SecurityHelper
{
    public static string GenerateSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public static string GeneratePin()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(6);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    public static string GenerateConfirmationCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(4);
        return Convert.ToHexString(bytes).ToUpper();
    }

    public static string HashPin(string pin, string salt)
    {
        var combined = Encoding.UTF8.GetBytes(pin + salt);
        var hash = SHA256.HashData(combined);
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPin(string pin, string salt, string storedHash)
    {
        var computedHash = HashPin(pin, salt);
        var computedBytes = Convert.FromBase64String(computedHash);
        var storedBytes = Convert.FromBase64String(storedHash);
        return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
    }
}
