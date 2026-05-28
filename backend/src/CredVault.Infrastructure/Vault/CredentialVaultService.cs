using CredVault.Domain.Credentials;
using CredVault.Domain.Suppliers;
using CredVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainCredentialEnvelope = CredVault.Domain.Credentials.CredentialEnvelope;

namespace CredVault.Infrastructure.Vault;

/// <summary>
/// Dynamic-field credential vault. Validates inbound fields against the supplier's schema, encrypts
/// with envelope encryption (AAD includes the credential id), persists, and writes a per-call access
/// log on retrieve. Decryption is rate-limited per credential.
/// </summary>
public sealed class CredentialVaultService : ICredentialVaultService
{
    private static readonly Action<ILogger, Guid, Exception?> LogRateLimited =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(2001, nameof(CredentialVaultService)),
            "Credential {CredentialId} retrieve was rate-limited");

    private readonly CredVaultDbContext _context;
    private readonly IEnvelopeEncryptionService _encryption;
    private readonly ICredentialSchemaProvider _schemas;
    private readonly ICredentialAccessLogRepository _accessLogs;
    private readonly IDateTimeProvider _clock;
    private readonly IInProcessRateLimiter _rateLimiter;
    private readonly PlaintextCache _plaintextCache;
    private readonly VaultRateLimitOptions _rateLimitOptions;
    private readonly ILogger<CredentialVaultService> _logger;

    /// <summary>Constructs the service.</summary>
    public CredentialVaultService(
        CredVaultDbContext context,
        IEnvelopeEncryptionService encryption,
        ICredentialSchemaProvider schemas,
        ICredentialAccessLogRepository accessLogs,
        IDateTimeProvider clock,
        IInProcessRateLimiter rateLimiter,
        PlaintextCache plaintextCache,
        IOptions<VaultRateLimitOptions> rateLimitOptions,
        ILogger<CredentialVaultService> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encryption);
        ArgumentNullException.ThrowIfNull(schemas);
        ArgumentNullException.ThrowIfNull(accessLogs);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(rateLimiter);
        ArgumentNullException.ThrowIfNull(plaintextCache);
        ArgumentNullException.ThrowIfNull(rateLimitOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _encryption = encryption;
        _schemas = schemas;
        _accessLogs = accessLogs;
        _clock = clock;
        _rateLimiter = rateLimiter;
        _plaintextCache = plaintextCache;
        _rateLimitOptions = rateLimitOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Guid> StoreAsync(
        Guid supplierId,
        Guid environmentId,
        string name,
        Slug slug,
        IReadOnlyDictionary<string, string> fields,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(fields);

        var supplier = await _context.CredentialSuppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException($"Supplier {supplierId} not found.");

        var schema = _schemas.GetSchema(supplier.SupplierType);
        CredentialFieldValidator.Validate(schema, fields);

        var credentialId = Guid.NewGuid();
        var aad = EncryptionContexts.ForCredential(supplier.OrganizationId, environmentId, supplierId, credentialId);

        var plaintext = CanonicalJson.SerializeFields(fields);
        var payload = await _encryption.EncryptAsync(plaintext, aad, cancellationToken).ConfigureAwait(false);

        var preview = MaskedPreview.Compute(schema, fields);
        var envelope = new DomainCredentialEnvelope(
            payload.Ciphertext, payload.WrappedDataKey, payload.Nonce, payload.AuthTag,
            payload.KekVersion, preview);

        var credential = Credential.Create(
            supplierId, environmentId, name, slug, envelope, schema.Version, expiresAtUtc, _clock);

        // Bind the entity's id to the pre-generated id that the AAD already references.
        DomainAccessors.SetCredentialId(credential, credentialId);

        _context.Credentials.Add(credential);
        _context.Entry(credential).Property<Guid>("OrganizationId").CurrentValue = supplier.OrganizationId;

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return credentialId;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, string>> RetrieveAsync(
        Guid credentialId,
        CredentialAccessContext access,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(access);

        var lease = await _rateLimiter.AcquireAsync($"cred:{credentialId}", cancellationToken).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            LogRateLimited(_logger, credentialId, null);
            await WriteAccessLogAsync(credentialId, access, AccessOutcome.Denied, cancellationToken).ConfigureAwait(false);
            throw new DomainException(
                $"Rate limit exceeded for credential {credentialId}. Limit is {_rateLimitOptions.PerCredentialPerMinute} retrievals per minute.");
        }

        var credential = await _context.Credentials
            .FirstOrDefaultAsync(c => c.Id == credentialId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException($"Credential {credentialId} not found.");

        var orgId = _context.Entry(credential).Property<Guid>("OrganizationId").CurrentValue;

        byte[] plaintext;
        if (_plaintextCache.TryGet(credentialId, out var cached))
        {
            plaintext = cached;
        }
        else
        {
            var supplier = await GetSupplierAsync(credential.SupplierId, cancellationToken).ConfigureAwait(false);
            var aad = EncryptionContexts.ForCredential(supplier.OrganizationId, credential.EnvironmentId, credential.SupplierId, credentialId);
            var payload = new EncryptedPayload(
                credential.EncryptedPayload, credential.WrappedDataKey, credential.Nonce, credential.AuthTag, credential.KekVersion);
            plaintext = await _encryption.DecryptAsync(payload, aad, cancellationToken).ConfigureAwait(false);
            _plaintextCache.Set(credentialId, plaintext);
        }

        var fields = CanonicalJson.DeserializeFields(plaintext);

        credential.RecordAccess(access.ActorType, access.ActorId, access.AccessMethod, _clock);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await WriteAccessLogAsync(credentialId, access, AccessOutcome.Success, cancellationToken, orgId).ConfigureAwait(false);

        return fields;
    }

    /// <inheritdoc/>
    public async Task RotateAsync(
        Guid credentialId,
        IReadOnlyDictionary<string, string> newFields,
        DateTime? newExpiresAtUtc,
        string? reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(newFields);

        var credential = await _context.Credentials
            .Include(c => c.Rotations)
            .FirstOrDefaultAsync(c => c.Id == credentialId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException($"Credential {credentialId} not found.");

        var supplier = await GetSupplierAsync(credential.SupplierId, cancellationToken).ConfigureAwait(false);
        var schema = _schemas.GetSchema(supplier.SupplierType);
        CredentialFieldValidator.Validate(schema, newFields);

        var aad = EncryptionContexts.ForCredential(supplier.OrganizationId, credential.EnvironmentId, credential.SupplierId, credentialId);
        var plaintext = CanonicalJson.SerializeFields(newFields);
        var payload = await _encryption.EncryptAsync(plaintext, aad, cancellationToken).ConfigureAwait(false);

        var preview = MaskedPreview.Compute(schema, newFields);
        var envelope = new DomainCredentialEnvelope(
            payload.Ciphertext, payload.WrappedDataKey, payload.Nonce, payload.AuthTag,
            payload.KekVersion, preview);

        // Pick a default actor for system-driven rotations; callers needing attribution should
        // pass it through the higher-level command handler.
        credential.Rotate(envelope, rotatedByUserId: Guid.NewGuid(), reason, newExpiresAtUtc, _clock);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _plaintextCache.Invalidate(credentialId);
    }

    private async Task<CredentialSupplier> GetSupplierAsync(Guid supplierId, CancellationToken cancellationToken) =>
        await _context.CredentialSuppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            .ConfigureAwait(false)
        ?? throw new DomainException($"Supplier {supplierId} not found.");

    private async Task WriteAccessLogAsync(
        Guid credentialId,
        CredentialAccessContext access,
        AccessOutcome outcome,
        CancellationToken cancellationToken,
        Guid? organizationId = null)
    {
        var log = CredentialAccessLog.Record(
            credentialId, access.ActorType, access.ActorId,
            access.IpAddress, access.UserAgent, access.AccessMethod, outcome, _clock);

        if (organizationId is { } orgId)
            _context.Entry(log).Property<Guid>("OrganizationId").CurrentValue = orgId;

        await _accessLogs.AppendAsync(log, cancellationToken).ConfigureAwait(false);
    }

}
