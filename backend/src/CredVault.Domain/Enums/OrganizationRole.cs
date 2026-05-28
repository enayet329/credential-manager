namespace CredVault.Domain.Enums;

/// <summary>Membership role of a user within an organization. Order is significant: higher value = more privilege.</summary>
public enum OrganizationRole
{
    /// <summary>Read-only access to projects, environments, and credential metadata. Cannot decrypt secret values.</summary>
    Viewer = 0,

    /// <summary>Can read and write credentials in projects they belong to.</summary>
    Developer = 1,

    /// <summary>Can manage members, suppliers, projects, and service tokens within the organization.</summary>
    Admin = 2,

    /// <summary>Full control — including transferring or deleting the organization itself.</summary>
    Owner = 3,
}
