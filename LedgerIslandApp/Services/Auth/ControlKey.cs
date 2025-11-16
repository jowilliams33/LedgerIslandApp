using System.Security.Cryptography;
using System.Text;

public static class ControlKey
{
    public static string Make(string secretOrBase64, Guid sessionId)
    {
        byte[] key;
        try
        {
            // Try Base64 first
            key = Convert.FromBase64String(secretOrBase64);
        }
        catch (FormatException)
        {
            // Fallback to UTF-8 bytes of raw string
            key = Encoding.UTF8.GetBytes(secretOrBase64);
        }

        using var hmac = new HMACSHA256(key);
        var bytes = hmac.ComputeHash(sessionId.ToByteArray());
        return Convert.ToBase64String(bytes);
    }



    public static bool Verify(string secretBase64, Guid sessionId, string providedBase64)
    {
        var expected = Make(secretBase64, sessionId);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(expected),
            Convert.FromBase64String(providedBase64));
    }
}
