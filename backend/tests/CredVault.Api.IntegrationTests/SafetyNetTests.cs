using CredVault.Api.Filters;

namespace CredVault.Api.IntegrationTests;

public class SafetyNetTests
{
    [Fact]
    public void Scan_trips_when_secret_value_appears_with_sensitive_key()
    {
        const string json = "{\"apiKey\":\"sk-Live0123456789ABCDEF\"}";
        ResponseSafetyNetFilter.Scan(json).Should().BeTrue();
    }

    [Theory]
    [InlineData("{\"name\":\"alice\"}")]
    [InlineData("{\"apiKey\":\"short\"}")] // sensitive key but value not secret-shaped
    [InlineData("{\"id\":\"sk-Live0123456789ABCDEF\"}")] // secret-shaped value but innocuous key
    public void Scan_does_not_trip_on_safe_documents(string json) =>
        ResponseSafetyNetFilter.Scan(json).Should().BeFalse();
}
