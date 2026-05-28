using CredVault.Domain.Organizations;

namespace CredVault.Domain.Tests.Organizations;

public class OrganizationMembershipTests
{
    private readonly FakeClock _clock = new();

    [Fact]
    public void ChangeRole_updates_the_role()
    {
        var org = Organization.Create("Acme", Slug.Create("acme"), _clock);
        var userId = Guid.NewGuid();
        var m = org.AddMember(userId, OrganizationRole.Viewer, _clock);
        m.ChangeRole(OrganizationRole.Owner);
        m.Role.Should().Be(OrganizationRole.Owner);
    }
}
