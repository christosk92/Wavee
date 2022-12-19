using DynamicData;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.SearchItems;

namespace Eum.UI.ViewModels.Search.Sources;

public interface ISearchSource
{
	IObservable<IChangeSet<ISearchItem, ComposedKey>> Changes { get; }

    IObservable<IChangeSet<SearchGroup>> GroupChanges { get; }
}

public class SearchGroup : IEquatable<SearchGroup>
{
    public string Title { get; init; }
    public string Id { get; init; }
    public ComposedKey Key => new(Id);

    public bool Equals(SearchGroup? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SearchGroup) obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(SearchGroup? left, SearchGroup? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SearchGroup? left, SearchGroup? right)
    {
        return !Equals(left, right);
    }
}
