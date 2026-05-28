using CredVault.Domain.Credentials;

namespace CredVault.Domain.Tests.Credentials;

public class CredentialNoteTests
{
    private readonly FakeClock _clock = new();

    private static CredentialEnvelope Envelope() =>
        new(Bytes.Repeat(0x1, 16), Bytes.Repeat(0x2, 16),
            Bytes.Repeat(0x3, CredentialEnvelope.NonceLength),
            Bytes.Repeat(0x4, CredentialEnvelope.AuthTagLength),
            KekVersion: 1, MaskedPreview: "note");

    [Fact]
    public void Create_sets_fields_from_envelope()
    {
        var credId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var note = CredentialNote.Create(credId, userId, Envelope(), _clock);
        note.CredentialId.Should().Be(credId);
        note.CreatedByUserId.Should().Be(userId);
        note.CreatedAtUtc.Should().Be(_clock.UtcNow);
        note.EncryptedContent.Should().NotBeEmpty();
        note.KekVersion.Should().Be(1);
    }

    [Fact]
    public void Create_rejects_empty_credential_id() =>
        ((Action)(() => CredentialNote.Create(Guid.Empty, Guid.NewGuid(), Envelope(), _clock)))
            .Should().Throw<DomainException>().WithMessage("*CredentialId*");

    [Fact]
    public void Create_rejects_empty_author() =>
        ((Action)(() => CredentialNote.Create(Guid.NewGuid(), Guid.Empty, Envelope(), _clock)))
            .Should().Throw<DomainException>().WithMessage("*CreatedByUserId*");

    [Fact]
    public void Create_validates_envelope_shape()
    {
        var bad = new CredentialEnvelope([0x1], [0x2], Bytes.Repeat(0, 10), Bytes.Repeat(0, 16), 1, "x");
        ((Action)(() => CredentialNote.Create(Guid.NewGuid(), Guid.NewGuid(), bad, _clock)))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_nulls()
    {
        ((Action)(() => CredentialNote.Create(Guid.NewGuid(), Guid.NewGuid(), null!, _clock))).Should().Throw<ArgumentNullException>();
        ((Action)(() => CredentialNote.Create(Guid.NewGuid(), Guid.NewGuid(), Envelope(), null!))).Should().Throw<ArgumentNullException>();
    }
}
