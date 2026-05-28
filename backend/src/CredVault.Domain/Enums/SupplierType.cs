namespace CredVault.Domain.Enums;

/// <summary>
/// Catalogue of credential issuers CredVault knows about. Each value is paired with a
/// <see cref="CredVault.Domain.Credentials.CredentialSchema"/> describing the fields the
/// supplier expects.
/// </summary>
public enum SupplierType
{
    /// <summary>OpenAI API (chat / completions / embeddings).</summary>
    OpenAI = 0,

    /// <summary>Anthropic Claude API.</summary>
    Anthropic = 1,

    /// <summary>Azure-hosted OpenAI deployment.</summary>
    AzureOpenAI = 2,

    /// <summary>AWS programmatic credentials (access key id + secret access key).</summary>
    AwsCredentials = 3,

    /// <summary>GCP service-account credentials.</summary>
    GcpCredentials = 4,

    /// <summary>Azure AD / service-principal credentials.</summary>
    AzureCredentials = 5,

    /// <summary>Stripe API keys (publishable, secret, webhook signing).</summary>
    Stripe = 6,

    /// <summary>GitHub personal access token / fine-grained token.</summary>
    GitHub = 7,

    /// <summary>GitLab personal access token / project token.</summary>
    GitLab = 8,

    /// <summary>PostgreSQL connection credentials.</summary>
    Postgres = 9,

    /// <summary>MySQL / MariaDB connection credentials.</summary>
    MySql = 10,

    /// <summary>MongoDB connection string / username + password.</summary>
    MongoDb = 11,

    /// <summary>Redis connection string + password.</summary>
    Redis = 12,

    /// <summary>A simple API-key + optional base-URL pair for unknown vendors.</summary>
    GenericApiKey = 13,

    /// <summary>Free-form schema defined by the organization.</summary>
    Custom = 14,
}
