using CredVault.Infrastructure.Logging;
using Serilog;
using Serilog.Events;

namespace CredVault.Infrastructure.Tests.Logging;

public class SensitivePropertyMaskerTests
{
    [Theory]
    [InlineData("apiKey")]
    [InlineData("api_key")]
    [InlineData("secret_key")]
    [InlineData("master_key")]
    [InlineData("kek")]
    [InlineData("webhook_secret")]
    [InlineData("Authorization")]
    [InlineData("PrivateKey")]
    [InlineData("dek")]
    [InlineData("token")]
    public void IsSensitive_matches_known_property_names(string name) =>
        SensitivePropertyMasker.IsSensitive(name).Should().BeTrue();

    [Theory]
    [InlineData("name")]
    [InlineData("count")]
    [InlineData("email")]
    public void IsSensitive_returns_false_for_innocuous_names(string name) =>
        SensitivePropertyMasker.IsSensitive(name).Should().BeFalse();

    [Fact]
    public void Enrich_masks_sensitive_top_level_properties()
    {
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .Enrich.With(new SensitivePropertyMasker())
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("Request received {apiKey} {secret_key} {master_key} {kek} {webhook_secret} {name}",
            "sk-real-value", "sk-real-secret", "kek-bytes", "kek-bytes", "whsec-real", "alice");

        sink.Events.Should().HaveCount(1);
        var evt = sink.Events[0];
        ((ScalarValue)evt.Properties["apiKey"]).Value.Should().Be(SensitivePropertyMasker.MaskedValue);
        ((ScalarValue)evt.Properties["secret_key"]).Value.Should().Be(SensitivePropertyMasker.MaskedValue);
        ((ScalarValue)evt.Properties["master_key"]).Value.Should().Be(SensitivePropertyMasker.MaskedValue);
        ((ScalarValue)evt.Properties["kek"]).Value.Should().Be(SensitivePropertyMasker.MaskedValue);
        ((ScalarValue)evt.Properties["webhook_secret"]).Value.Should().Be(SensitivePropertyMasker.MaskedValue);
        ((ScalarValue)evt.Properties["name"]).Value.Should().Be("alice");
    }

    [Fact]
    public void Enrich_masks_sensitive_keys_inside_a_structured_object()
    {
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .Enrich.With(new SensitivePropertyMasker())
            .WriteTo.Sink(sink)
            .CreateLogger();

        logger.Information("payload {@request}", new
        {
            User = "alice",
            ApiKey = "sk-actual",
            Nested = new { Secret = "leaked" },
        });

        sink.Events.Should().HaveCount(1);
        var evt = sink.Events[0];
        var rendered = evt.Properties["request"].ToString();
        rendered.Should().NotContain("sk-actual");
        rendered.Should().NotContain("leaked");
        rendered.Should().Contain(SensitivePropertyMasker.MaskedValue);
    }

    private sealed class CapturingSink : Serilog.Core.ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
