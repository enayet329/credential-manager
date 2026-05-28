using CredVault.Domain.Credentials;

namespace CredVault.Domain.Tests.Credentials;

public class CredentialEnvelopeTests
{
    private static CredentialEnvelope Valid() =>
        new(Bytes.Repeat(0x01, 32), Bytes.Repeat(0x02, 32),
            Bytes.Repeat(0x03, CredentialEnvelope.NonceLength),
            Bytes.Repeat(0x04, CredentialEnvelope.AuthTagLength),
            KekVersion: 1, MaskedPreview: "sk-***xyz");

    [Fact]
    public void Validate_passes_for_well_formed_envelope() =>
        Valid().Invoking(e => e.Validate()).Should().NotThrow();

    [Fact]
    public void Validate_rejects_empty_payload()
    {
        var env = new CredentialEnvelope([], [0x1], Bytes.Repeat(0, 12), Bytes.Repeat(0, 16), 1, "x");
        env.Invoking(e => e.Validate()).Should().Throw<DomainException>().WithMessage("*EncryptedPayload*");
    }

    [Fact]
    public void Validate_rejects_empty_data_key()
    {
        var env = new CredentialEnvelope([0x1], [], Bytes.Repeat(0, 12), Bytes.Repeat(0, 16), 1, "x");
        env.Invoking(e => e.Validate()).Should().Throw<DomainException>().WithMessage("*WrappedDataKey*");
    }

    [Fact]
    public void Validate_rejects_wrong_nonce_size()
    {
        var env = new CredentialEnvelope([0x1], [0x2], Bytes.Repeat(0, 11), Bytes.Repeat(0, 16), 1, "x");
        env.Invoking(e => e.Validate()).Should().Throw<DomainException>().WithMessage("*Nonce*");
    }

    [Fact]
    public void Validate_rejects_wrong_tag_size()
    {
        var env = new CredentialEnvelope([0x1], [0x2], Bytes.Repeat(0, 12), Bytes.Repeat(0, 15), 1, "x");
        env.Invoking(e => e.Validate()).Should().Throw<DomainException>().WithMessage("*AuthTag*");
    }

    [Fact]
    public void Validate_rejects_zero_kek_version()
    {
        var env = new CredentialEnvelope([0x1], [0x2], Bytes.Repeat(0, 12), Bytes.Repeat(0, 16), 0, "x");
        env.Invoking(e => e.Validate()).Should().Throw<DomainException>().WithMessage("*KekVersion*");
    }

    [Fact]
    public void Validate_rejects_long_preview()
    {
        var env = new CredentialEnvelope([0x1], [0x2], Bytes.Repeat(0, 12), Bytes.Repeat(0, 16), 1, new string('x', 17));
        env.Invoking(e => e.Validate()).Should().Throw<DomainException>().WithMessage("*MaskedPreview*");
    }

    [Fact]
    public void Validate_rejects_null_arrays()
    {
        new CredentialEnvelope(null!, [0x2], Bytes.Repeat(0, 12), Bytes.Repeat(0, 16), 1, "x")
            .Invoking(e => e.Validate()).Should().Throw<ArgumentNullException>();
    }
}
