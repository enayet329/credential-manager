using ProjectEnv = CredVault.Domain.Projects.Environment;

namespace CredVault.Domain.Tests.Projects;

public class EnvironmentTests
{
    private readonly FakeClock _clock = new();

    private ProjectEnv NewEnv() =>
        ProjectEnv.Create(Guid.NewGuid(), "Staging", Slug.Create("staging"), EnvironmentType.Staging, _clock);

    [Fact]
    public void Create_sets_fields()
    {
        var projectId = Guid.NewGuid();
        var env = ProjectEnv.Create(projectId, "Prod", Slug.Create("prod"), EnvironmentType.Production, _clock);
        env.ProjectId.Should().Be(projectId);
        env.Name.Should().Be("Prod");
        env.Slug.Value.Should().Be("prod");
        env.Type.Should().Be(EnvironmentType.Production);
        env.CreatedAtUtc.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void Create_rejects_empty_project_id() =>
        ((Action)(() => ProjectEnv.Create(Guid.Empty, "x", Slug.Create("x-x"), EnvironmentType.Custom, _clock)))
            .Should().Throw<DomainException>().WithMessage("*ProjectId*");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_name(string n) =>
        ((Action)(() => ProjectEnv.Create(Guid.NewGuid(), n, Slug.Create("abc"), EnvironmentType.Development, _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_long_name() =>
        ((Action)(() => ProjectEnv.Create(Guid.NewGuid(), new string('a', 101), Slug.Create("abc"), EnvironmentType.Development, _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_nulls()
    {
        ((Action)(() => ProjectEnv.Create(Guid.NewGuid(), "x", null!, EnvironmentType.Development, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => ProjectEnv.Create(Guid.NewGuid(), "x", Slug.Create("abc"), EnvironmentType.Development, null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Rename_updates_name()
    {
        var env = NewEnv();
        env.Rename(" Renamed ");
        env.Name.Should().Be("Renamed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_rejects_empty(string n) =>
        ((Action)(() => NewEnv().Rename(n))).Should().Throw<DomainException>();

    [Fact]
    public void Rename_rejects_long() =>
        ((Action)(() => NewEnv().Rename(new string('a', 101)))).Should().Throw<DomainException>();
}
