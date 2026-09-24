using System.Text;

namespace ResQ.API.Security;

public static class JwtSigningKeyValidator
{
    private const string ConfigurationKey = "JwtSettings:SecretKey";
    private const int MinimumKeyLengthBytes = 32;

    public static byte[] GetRequiredKeyBytes(IConfiguration configuration)
    {
        var secretKey = configuration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException($"Configuration value '{ConfigurationKey}' is required.");

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length < MinimumKeyLengthBytes)
            throw new InvalidOperationException($"Configuration value '{ConfigurationKey}' must contain at least {MinimumKeyLengthBytes} bytes of key material.");

        return keyBytes;
    }
}
