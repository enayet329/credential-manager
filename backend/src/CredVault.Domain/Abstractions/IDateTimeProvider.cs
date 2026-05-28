namespace CredVault.Domain.Abstractions;

/// <summary>
/// Abstraction over the system clock. Domain code must never read <see cref="System.DateTime.UtcNow"/> directly;
/// inject an <see cref="IDateTimeProvider"/> so tests can advance time deterministically.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Returns the current UTC instant.</summary>
    DateTime UtcNow { get; }
}
