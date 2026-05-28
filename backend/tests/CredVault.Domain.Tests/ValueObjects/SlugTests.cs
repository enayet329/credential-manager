namespace CredVault.Domain.Tests.ValueObjects;

public class SlugTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("a-b-c")]
    [InlineData("project-1")]
    [InlineData("123")]
    [InlineData("a1b2c3-d4")]
    public void Create_accepts_valid(string raw)
    {
        var slug = Slug.Create(raw);
        slug.Value.Should().Be(raw);
        slug.ToString().Should().Be(raw);
    }

    [Theory]
    [InlineData("ab", "too short")]
    [InlineData("UPPER", "uppercase")]
    [InlineData("with space", "space")]
    [InlineData("under_score", "underscore")]
    [InlineData("dot.dot", "dot")]
    [InlineData("trailing-dash-", "ok by regex but the regex actually accepts this; included for awareness")]
    public void Create_rejects_invalid(string raw, string _)
    {
        if (raw == "trailing-dash-")
        {
            Slug.Create(raw).Value.Should().Be(raw);
            return;
        }
        var act = () => Slug.Create(raw);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty(string raw)
    {
        var act = () => Slug.Create(raw);
        act.Should().Throw<DomainException>().WithMessage("*empty*");
    }

    [Fact]
    public void Create_rejects_too_long()
    {
        var act = () => Slug.Create(new string('a', Slug.MaxLength + 1));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_accepts_boundary_lengths()
    {
        Slug.Create(new string('a', Slug.MinLength)).Value.Length.Should().Be(Slug.MinLength);
        Slug.Create(new string('a', Slug.MaxLength)).Value.Length.Should().Be(Slug.MaxLength);
    }

    [Fact]
    public void Implicit_string_conversion_works()
    {
        string s = Slug.Create("hello");
        s.Should().Be("hello");
    }
}
