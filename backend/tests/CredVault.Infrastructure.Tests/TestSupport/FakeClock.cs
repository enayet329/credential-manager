namespace CredVault.Infrastructure.Tests.TestSupport;

internal sealed class FakeClock(DateTime? start = null) : IDateTimeProvider
{
    public DateTime UtcNow { get; private set; } = start ?? new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => UtcNow = UtcNow + by;
}
