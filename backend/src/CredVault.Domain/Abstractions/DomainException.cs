namespace CredVault.Domain.Abstractions;

/// <summary>
/// Raised when a domain invariant is violated. Callers should treat this as a 4xx (bad request)
/// at the API boundary, never as a 5xx.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>Creates a new <see cref="DomainException"/> with the given message.</summary>
    public DomainException(string message) : base(message) { }

    /// <summary>Creates a new <see cref="DomainException"/> with a message and inner exception.</summary>
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
