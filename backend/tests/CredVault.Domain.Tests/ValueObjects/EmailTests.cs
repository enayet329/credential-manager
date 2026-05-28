namespace CredVault.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last+tag@sub.example.co.uk")]
    [InlineData("a_b@x.io")]
    public void Create_returns_lowercase_email_for_valid_input(string raw)
    {
        var email = Email.Create(raw);
        email.Value.Should().Be(raw.ToLowerInvariant());
        email.ToString().Should().Be(raw.ToLowerInvariant());
    }

    [Fact]
    public void Create_trims_whitespace_and_lowercases()
    {
        var email = Email.Create("  Mixed@Case.COM  ");
        email.Value.Should().Be("mixed@case.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty(string raw)
    {
        var act = () => Email.Create(raw);
        act.Should().Throw<DomainException>().WithMessage("*empty*");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@host")]
    [InlineData("user with space@example.com")]
    public void Create_rejects_invalid_format(string raw)
    {
        var act = () => Email.Create(raw);
        act.Should().Throw<DomainException>().WithMessage("*not a valid email*");
    }

    [Fact]
    public void Create_rejects_too_long()
    {
        var local = new string('a', 250);
        var act = () => Email.Create($"{local}@x.com");
        act.Should().Throw<DomainException>().WithMessage($"*at most {Email.MaxLength}*");
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Email.Create("a@b.com").Should().Be(Email.Create("A@B.COM"));
        Email.Create("a@b.com").Should().NotBe(Email.Create("c@d.com"));
    }
}
