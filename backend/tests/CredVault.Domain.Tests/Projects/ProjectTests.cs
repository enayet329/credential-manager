using CredVault.Domain.Projects;

namespace CredVault.Domain.Tests.Projects;

public class ProjectTests
{
    private readonly FakeClock _clock = new();

    private Project NewProject() =>
        Project.Create(Guid.NewGuid(), "My App", Slug.Create("my-app"), "desc", _clock);

    [Fact]
    public void Create_sets_all_fields()
    {
        var orgId = Guid.NewGuid();
        var p = Project.Create(orgId, "App", Slug.Create("app"), "  trimmed  ", _clock);
        p.OrganizationId.Should().Be(orgId);
        p.Name.Should().Be("App");
        p.Slug.Value.Should().Be("app");
        p.Description.Should().Be("trimmed");
        p.CreatedAtUtc.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void Create_allows_null_description()
    {
        var p = Project.Create(Guid.NewGuid(), "App", Slug.Create("app"), null, _clock);
        p.Description.Should().BeNull();
    }

    [Fact]
    public void Create_rejects_empty_org_id() =>
        ((Action)(() => Project.Create(Guid.Empty, "x", Slug.Create("x-x"), null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*OrganizationId*");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_name(string n) =>
        ((Action)(() => Project.Create(Guid.NewGuid(), n, Slug.Create("abc"), null, _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_long_name() =>
        ((Action)(() => Project.Create(Guid.NewGuid(), new string('a', 101), Slug.Create("abc"), null, _clock)))
            .Should().Throw<DomainException>().WithMessage("*100*");

    [Fact]
    public void Create_rejects_long_description() =>
        ((Action)(() => Project.Create(Guid.NewGuid(), "x", Slug.Create("abc"), new string('d', 501), _clock)))
            .Should().Throw<DomainException>().WithMessage("*500*");

    [Fact]
    public void Create_rejects_nulls()
    {
        ((Action)(() => Project.Create(Guid.NewGuid(), "x", null!, null, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => Project.Create(Guid.NewGuid(), "x", Slug.Create("abc"), null, null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Rename_updates_name()
    {
        var p = NewProject();
        p.Rename(" Other ");
        p.Name.Should().Be("Other");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_rejects_empty(string n) =>
        ((Action)(() => NewProject().Rename(n))).Should().Throw<DomainException>();

    [Fact]
    public void Rename_rejects_long() =>
        ((Action)(() => NewProject().Rename(new string('a', 101)))).Should().Throw<DomainException>();

    [Fact]
    public void UpdateDescription_allows_null()
    {
        var p = NewProject();
        p.UpdateDescription(null);
        p.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateDescription_trims()
    {
        var p = NewProject();
        p.UpdateDescription("  yes  ");
        p.Description.Should().Be("yes");
    }

    [Fact]
    public void UpdateDescription_rejects_long() =>
        ((Action)(() => NewProject().UpdateDescription(new string('d', 501)))).Should().Throw<DomainException>();
}
