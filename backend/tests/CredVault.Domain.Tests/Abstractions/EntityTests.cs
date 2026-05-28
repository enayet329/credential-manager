namespace CredVault.Domain.Tests.Abstractions;

public class EntityTests
{
    private sealed record DummyEvent(Guid Id, DateTime OccurredAtUtc) : DomainEvent(OccurredAtUtc);

    private sealed class DummyEntity : Entity
    {
        public DummyEntity(Guid id) { Id = id; }
        public void RaisePublic(DomainEvent e) => Raise(e);
    }

    [Fact]
    public void Equality_is_by_type_and_id()
    {
        var id = Guid.NewGuid();
        var a = new DummyEntity(id);
        var b = new DummyEntity(id);
        var c = new DummyEntity(Guid.NewGuid());

        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Equals("not-an-entity").Should().BeFalse();
    }

    [Fact]
    public void Raise_and_ClearDomainEvents_work()
    {
        var e = new DummyEntity(Guid.NewGuid());
        e.DomainEvents.Should().BeEmpty();
        e.RaisePublic(new DummyEvent(Guid.NewGuid(), DateTime.UtcNow));
        e.DomainEvents.Should().HaveCount(1);
        e.ClearDomainEvents();
        e.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainException_with_inner_exposes_inner()
    {
        var inner = new InvalidOperationException("boom");
        var ex = new DomainException("wrapped", inner);
        ex.InnerException.Should().BeSameAs(inner);
        ex.Message.Should().Be("wrapped");
    }
}
