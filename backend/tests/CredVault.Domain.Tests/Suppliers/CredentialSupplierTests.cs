using CredVault.Domain.Suppliers;

namespace CredVault.Domain.Tests.Suppliers;

public class CredentialSupplierTests
{
    private readonly FakeClock _clock = new();

    private CredentialSupplier NewSupplier() =>
        CredentialSupplier.Create(Guid.NewGuid(), SupplierType.OpenAI, "OpenAI prod", _clock);

    [Fact]
    public void Create_sets_fields_active()
    {
        var orgId = Guid.NewGuid();
        var s = CredentialSupplier.Create(orgId, SupplierType.Anthropic, "  Anthropic  ", _clock);
        s.OrganizationId.Should().Be(orgId);
        s.SupplierType.Should().Be(SupplierType.Anthropic);
        s.DisplayName.Should().Be("Anthropic");
        s.IsActive.Should().BeTrue();
        s.CreatedAtUtc.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void Create_rejects_empty_org() =>
        ((Action)(() => CredentialSupplier.Create(Guid.Empty, SupplierType.OpenAI, "X", _clock)))
            .Should().Throw<DomainException>();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_display(string n) =>
        ((Action)(() => CredentialSupplier.Create(Guid.NewGuid(), SupplierType.OpenAI, n, _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_long_display() =>
        ((Action)(() => CredentialSupplier.Create(Guid.NewGuid(), SupplierType.OpenAI, new string('a', 101), _clock)))
            .Should().Throw<DomainException>();

    [Fact]
    public void Create_rejects_null_clock() =>
        ((Action)(() => CredentialSupplier.Create(Guid.NewGuid(), SupplierType.OpenAI, "X", null!)))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void Rename_updates()
    {
        var s = NewSupplier();
        s.Rename(" New ");
        s.DisplayName.Should().Be("New");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_rejects_empty(string n) =>
        ((Action)(() => NewSupplier().Rename(n))).Should().Throw<DomainException>();

    [Fact]
    public void Rename_rejects_long() =>
        ((Action)(() => NewSupplier().Rename(new string('a', 101)))).Should().Throw<DomainException>();

    [Fact]
    public void Activate_Deactivate_toggle()
    {
        var s = NewSupplier();
        s.Deactivate();
        s.IsActive.Should().BeFalse();
        s.Activate();
        s.IsActive.Should().BeTrue();
    }
}
