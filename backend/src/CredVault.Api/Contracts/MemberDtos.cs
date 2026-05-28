using System.ComponentModel.DataAnnotations;
using CredVault.Domain.Enums;

namespace CredVault.Api.Contracts;

/// <summary>Public view of an organisation member row.</summary>
public sealed record MemberDto(
    Guid UserId,
    string Email,
    OrganizationRole Role,
    DateTime JoinedAtUtc,
    bool EmailConfirmed);

/// <summary>Body for <c>POST /orgs/{slug}/members</c>.</summary>
/// <remarks>
/// Creates the user if no account exists for the email yet. Returns the new (or existing) member.
/// The temporary password is returned exactly once when a user is created.
/// </remarks>
public sealed record InviteMemberRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] OrganizationRole Role,
    [property: StringLength(64, MinimumLength = 8)] string? InitialPassword);

/// <summary>Response shape for invite. <c>TemporaryPassword</c> is populated only for new users.</summary>
public sealed record InviteMemberResponse(
    MemberDto Member,
    bool UserCreated,
    string? TemporaryPassword);

/// <summary>Body for <c>PATCH /orgs/{slug}/members/{userId}</c>.</summary>
public sealed record UpdateMemberRequest([property: Required] OrganizationRole Role);
