using CredVault.Domain.Organizations;

namespace CredVault.Domain.Tests.Organizations;

public class OrganizationTests
{
    private readonly FakeClock _clock = new();

    private Organization NewOrg() =>
        Organization.Create("Acme", Slug.Create("acme"), _clock);

    [Fact]
    public void Create_initializes_active_with_timestamps()
    {
        var org = NewOrg();
        org.Id.Should().NotBe(Guid.Empty);
        org.Name.Should().Be("Acme");
        org.Slug.Value.Should().Be("acme");
        org.CreatedAtUtc.Should().Be(_clock.UtcNow);
        org.IsActive.Should().BeTrue();
        org.Memberships.Should().BeEmpty();
    }

    [Fact]
    public void Create_trims_name() =>
        Organization.Create("  Spaced  ", Slug.Create("spaced"), _clock).Name.Should().Be("Spaced");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_name(string name)
    {
        var act = () => Organization.Create(name, Slug.Create("acme"), _clock);
        act.Should().Throw<DomainException>().WithMessage("*empty*");
    }

    [Fact]
    public void Create_rejects_long_name()
    {
        var act = () => Organization.Create(new string('a', 101), Slug.Create("acme"), _clock);
        act.Should().Throw<DomainException>().WithMessage("*100 characters*");
    }

    [Fact]
    public void Create_rejects_null_args()
    {
        ((Action)(() => Organization.Create("x", null!, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => Organization.Create("x", Slug.Create("acme"), null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Rename_updates_name()
    {
        var org = NewOrg();
        org.Rename(" New Name ");
        org.Name.Should().Be("New Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_rejects_empty(string n)
    {
        var org = NewOrg();
        ((Action)(() => org.Rename(n))).Should().Throw<DomainException>();
    }

    [Fact]
    public void Rename_rejects_long()
    {
        var org = NewOrg();
        ((Action)(() => org.Rename(new string('a', 101)))).Should().Throw<DomainException>();
    }

    [Fact]
    public void AddMember_appends_with_role()
    {
        var org = NewOrg();
        var userId = Guid.NewGuid();
        var membership = org.AddMember(userId, OrganizationRole.Admin, _clock);
        membership.UserId.Should().Be(userId);
        membership.Role.Should().Be(OrganizationRole.Admin);
        membership.OrganizationId.Should().Be(org.Id);
        membership.JoinedAtUtc.Should().Be(_clock.UtcNow);
        org.Memberships.Should().ContainSingle();
    }

    [Fact]
    public void AddMember_rejects_duplicate_user()
    {
        var org = NewOrg();
        var userId = Guid.NewGuid();
        org.AddMember(userId, OrganizationRole.Developer, _clock);

        var act = () => org.AddMember(userId, OrganizationRole.Admin, _clock);
        act.Should().Throw<DomainException>().WithMessage("*already a member*");
    }

    [Fact]
    public void AddMember_rejects_empty_user_id()
    {
        var act = () => NewOrg().AddMember(Guid.Empty, OrganizationRole.Viewer, _clock);
        act.Should().Throw<DomainException>().WithMessage("*UserId*");
    }

    [Fact]
    public void AddMember_rejects_null_clock()
    {
        var act = () => NewOrg().AddMember(Guid.NewGuid(), OrganizationRole.Viewer, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveMember_removes_existing()
    {
        var org = NewOrg();
        var userId = Guid.NewGuid();
        org.AddMember(userId, OrganizationRole.Developer, _clock);
        org.RemoveMember(userId);
        org.Memberships.Should().BeEmpty();
    }

    [Fact]
    public void RemoveMember_throws_when_not_a_member()
    {
        var org = NewOrg();
        var act = () => org.RemoveMember(Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*not a member*");
    }

    [Fact]
    public void ChangeMemberRole_updates_membership()
    {
        var org = NewOrg();
        var userId = Guid.NewGuid();
        org.AddMember(userId, OrganizationRole.Viewer, _clock);
        org.ChangeMemberRole(userId, OrganizationRole.Owner);
        org.Memberships.Single().Role.Should().Be(OrganizationRole.Owner);
    }

    [Fact]
    public void ChangeMemberRole_throws_when_not_a_member()
    {
        var act = () => NewOrg().ChangeMemberRole(Guid.NewGuid(), OrganizationRole.Owner);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_Deactivate_toggle_flag()
    {
        var org = NewOrg();
        org.Deactivate();
        org.IsActive.Should().BeFalse();
        org.Activate();
        org.IsActive.Should().BeTrue();
    }
}
