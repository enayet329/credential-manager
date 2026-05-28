namespace CredVault.Domain.Credentials;

/// <summary>
/// Built-in catalogue of credential schemas indexed by <see cref="SupplierType"/>. Bump a schema's
/// <see cref="CredentialSchema.Version"/> when its field set changes — old credentials remain
/// decryptable because <see cref="Credential.CredentialSchemaVersion"/> records the version they
/// were written under.
/// </summary>
public static class CredentialSchemaRegistry
{
    private static readonly Dictionary<SupplierType, CredentialSchema> Schemas = Build();

    /// <summary>All registered schemas.</summary>
    public static IReadOnlyCollection<CredentialSchema> All => Schemas.Values;

    /// <summary>Returns the current schema for a supplier. Throws <see cref="DomainException"/> if none is registered.</summary>
    public static CredentialSchema Get(SupplierType supplierType) =>
        Schemas.TryGetValue(supplierType, out var schema)
            ? schema
            : throw new DomainException($"No schema registered for supplier '{supplierType}'.");

    private static Dictionary<SupplierType, CredentialSchema> Build()
    {
        var schemas = new[]
        {
            new CredentialSchema(SupplierType.OpenAI, 1,
            [
                new CredentialField("api_key", "API key", FieldType.Password, IsRequired: true, IsSecret: true,
                    Placeholder: "sk-..."),
                new CredentialField("organization_id", "Organization ID", FieldType.Text, IsRequired: false, IsSecret: false,
                    Placeholder: "org-..."),
            ]),
            new CredentialSchema(SupplierType.Anthropic, 1,
            [
                new CredentialField("api_key", "API key", FieldType.Password, IsRequired: true, IsSecret: true,
                    Placeholder: "sk-ant-..."),
            ]),
            new CredentialSchema(SupplierType.AzureOpenAI, 1,
            [
                new CredentialField("api_key", "API key", FieldType.Password, IsRequired: true, IsSecret: true),
                new CredentialField("endpoint", "Endpoint", FieldType.Url, IsRequired: true, IsSecret: false,
                    Placeholder: "https://my-resource.openai.azure.com"),
                new CredentialField("deployment_name", "Deployment name", FieldType.Text, IsRequired: true, IsSecret: false),
                new CredentialField("api_version", "API version", FieldType.Text, IsRequired: true, IsSecret: false,
                    Placeholder: "2024-10-21"),
            ]),
            new CredentialSchema(SupplierType.AwsCredentials, 1,
            [
                new CredentialField("access_key_id", "Access key ID", FieldType.Text, IsRequired: true, IsSecret: false,
                    Placeholder: "AKIA..."),
                new CredentialField("secret_access_key", "Secret access key", FieldType.Password, IsRequired: true, IsSecret: true),
                new CredentialField("region", "Region", FieldType.Text, IsRequired: true, IsSecret: false,
                    Placeholder: "us-east-1"),
                new CredentialField("session_token", "Session token", FieldType.Password, IsRequired: false, IsSecret: true,
                    HelpText: "Required only when using temporary STS credentials."),
            ]),
            new CredentialSchema(SupplierType.Stripe, 1,
            [
                new CredentialField("publishable_key", "Publishable key", FieldType.Text, IsRequired: true, IsSecret: false,
                    Placeholder: "pk_live_..."),
                new CredentialField("secret_key", "Secret key", FieldType.Password, IsRequired: true, IsSecret: true,
                    Placeholder: "sk_live_..."),
                new CredentialField("webhook_secret", "Webhook signing secret", FieldType.Password, IsRequired: false, IsSecret: true,
                    Placeholder: "whsec_..."),
            ]),
            new CredentialSchema(SupplierType.GitHub, 1,
            [
                new CredentialField("personal_access_token", "Personal access token", FieldType.Password, IsRequired: true, IsSecret: true,
                    Placeholder: "ghp_..."),
                new CredentialField("username", "Username", FieldType.Text, IsRequired: false, IsSecret: false),
            ]),
            new CredentialSchema(SupplierType.Postgres, 1,
            [
                new CredentialField("host", "Host", FieldType.Text, IsRequired: true, IsSecret: false,
                    Placeholder: "db.example.com"),
                new CredentialField("port", "Port", FieldType.Text, IsRequired: true, IsSecret: false,
                    Placeholder: "5432", ValidationRegex: @"^\d{1,5}$"),
                new CredentialField("username", "Username", FieldType.Text, IsRequired: true, IsSecret: false),
                new CredentialField("password", "Password", FieldType.Password, IsRequired: true, IsSecret: true),
                new CredentialField("database", "Database name", FieldType.Text, IsRequired: true, IsSecret: false),
                new CredentialField("ssl_mode", "SSL mode", FieldType.Text, IsRequired: false, IsSecret: false,
                    Placeholder: "require"),
            ]),
            new CredentialSchema(SupplierType.GenericApiKey, 1,
            [
                new CredentialField("api_key", "API key", FieldType.Password, IsRequired: true, IsSecret: true),
                new CredentialField("base_url", "Base URL", FieldType.Url, IsRequired: false, IsSecret: false),
            ]),
            new CredentialSchema(SupplierType.Custom, 1,
            [
                new CredentialField("payload", "Free-form payload", FieldType.MultiLine, IsRequired: true, IsSecret: true,
                    HelpText: "Free-form text — organisation defines its own structure."),
            ]),
        };

        return schemas.ToDictionary(s => s.SupplierType);
    }
}
