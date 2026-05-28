namespace CredVault.Infrastructure;

/// <summary>Default <see cref="IDateTimeProvider"/> for production — reads <see cref="DateTime.UtcNow"/>.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;
}
