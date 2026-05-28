namespace CredVault.Api.Auth;

/// <summary>String constants for the permission claims used by the API.</summary>
public static class Permissions
{
    /// <summary>Read credential metadata (listings, audit logs, rotation history).</summary>
    public const string ReadMetadata = "credentials:read:metadata";

    /// <summary>Decrypt and read credential values. Required for <c>/value</c> and decrypted notes.</summary>
    public const string ReadValue = "credentials:read:value";

    /// <summary>Create, rotate, revoke, or delete credentials.</summary>
    public const string WriteCredentials = "credentials:write";

    /// <summary>Create, modify, or delete suppliers.</summary>
    public const string WriteSuppliers = "suppliers:write";

    /// <summary>Create, modify, or delete projects and environments.</summary>
    public const string WriteProjects = "projects:write";

    /// <summary>Register new credential schemas at runtime via <c>POST /api/admin/credential-schemas</c>.</summary>
    public const string AdminSchemas = "admin:schemas";
}
