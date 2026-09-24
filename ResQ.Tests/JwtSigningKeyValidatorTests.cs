using Microsoft.Extensions.Configuration;
using ResQ.API.Security;

namespace ResQ.Tests;

public sealed class JwtSigningKeyValidatorTests
{
    [Fact]
    public void GetRequiredKeyBytes_accepts_at_least_32_bytes()
    {
        var configuration = CreateConfiguration(new string('k', 32));

        var keyBytes = JwtSigningKeyValidator.GetRequiredKeyBytes(configuration);

        Assert.Equal(32, keyBytes.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short-key")]
    public void GetRequiredKeyBytes_rejects_missing_or_weak_key_without_exposing_value(string? secret)
    {
        var configuration = CreateConfiguration(secret);

        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtSigningKeyValidator.GetRequiredKeyBytes(configuration));

        Assert.Contains("JwtSettings:SecretKey", exception.Message);
        if (!string.IsNullOrEmpty(secret))
            Assert.DoesNotContain(secret, exception.Message);
    }

    private static IConfiguration CreateConfiguration(string? secret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = secret
            })
            .Build();
}
