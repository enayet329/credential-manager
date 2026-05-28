namespace CredVault.Cli.Tests;

public class SmokeTests
{
    [Fact]
    public void VersionIsNonEmpty() => Assert.False(string.IsNullOrWhiteSpace(Program.Version));
}
