namespace CredVault.Application.Abstractions.Cryptography;

/// <summary>
/// Canonical builders for AAD strings passed to <see cref="IEnvelopeEncryptionService"/>. Centralising
/// the format here ensures encrypt and decrypt always use identical bytes — a typo anywhere breaks
/// decryption deterministically.
/// </summary>
public static class EncryptionContexts
{
    /// <summary>AAD for a credential's secret payload.</summary>
    public static string ForCredential(Guid orgId, Guid envId, Guid supplierId, Guid credentialId) =>
        $"org:{orgId}|env:{envId}|supplier:{supplierId}|cred:{credentialId}";

    /// <summary>AAD for a credential note.</summary>
    public static string ForCredentialNote(Guid orgId, Guid credentialId, Guid noteId) =>
        $"org:{orgId}|cred:{credentialId}|note:{noteId}";

    /// <summary>AAD for a user's MFA seed.</summary>
    public static string ForMfaSecret(Guid userId) => $"user:{userId}|mfa";

    /// <summary>AAD for a webhook signing secret.</summary>
    public static string ForWebhookSecret(Guid orgId, Guid webhookId) =>
        $"org:{orgId}|webhook:{webhookId}";
}
