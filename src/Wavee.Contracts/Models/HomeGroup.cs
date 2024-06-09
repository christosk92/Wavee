namespace Wavee.Contracts.Models;

public class HomeGroup(string Id, string Title, bool Pinned, int Order, bool IsLazySection) : IEquatable<HomeGroup>
{
    public string Id { get; init; } = Id;
    public string Title { get; init; } = Title;
    public bool Pinned { get; init; } = Pinned;
    public int Order { get; init; } = Order;
    public bool IsLazySection { get; init; } = IsLazySection;

    public void Deconstruct(out string Id, out string Title, out bool Pinned, out int Order, out bool IsLazySection)
    {
        Id = this.Id;
        Title = this.Title;
        Pinned = this.Pinned;
        Order = this.Order;
        IsLazySection = this.IsLazySection;
    }

    public bool Equals(HomeGroup other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((HomeGroup)obj);
    }

    public override int GetHashCode()
    {
        return (Id != null ? Id.GetHashCode() : 0);
    }
}