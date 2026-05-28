using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CredVault.Infrastructure.Persistence.Conversions;

/// <summary>Reusable EF value converters for domain value objects.</summary>
internal static class ValueObjectConverters
{
    /// <summary><see cref="Slug"/> ↔ <see cref="string"/>.</summary>
    public static readonly ValueConverter<Slug, string> Slug = new(
        slug => slug.Value,
        value => CredVault.Domain.ValueObjects.Slug.Create(value));

    /// <summary><see cref="Email"/> ↔ <see cref="string"/>.</summary>
    public static readonly ValueConverter<Email, string> Email = new(
        email => email.Value,
        value => CredVault.Domain.ValueObjects.Email.Create(value));
}
