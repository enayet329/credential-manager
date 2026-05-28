namespace CredVault.Domain.Tests.TestSupport;

internal static class Bytes
{
    public static byte[] Repeat(byte value, int length)
    {
        var bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }
}
