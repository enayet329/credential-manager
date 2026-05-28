namespace CredVault.Domain.Enums;

/// <summary>The transport that produced a credential access — used to slice audit dashboards.</summary>
public enum AccessMethod
{
    /// <summary>Browser session against the web console.</summary>
    UI = 0,

    /// <summary>Local <c>credvault</c> CLI invocation.</summary>
    Cli = 1,

    /// <summary>Direct API call using a <c>ServiceToken</c> bearer.</summary>
    ServiceTokenApi = 2,
}
