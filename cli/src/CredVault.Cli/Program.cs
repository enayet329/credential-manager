using System.CommandLine;
using System.Reflection;

namespace CredVault.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        var root = new RootCommand("CredVault CLI — manage team secrets from your terminal.");
        var parseResult = root.Parse(args);
        return parseResult.Invoke();
    }

    internal static string Version =>
        typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? "0.0.0";
}
