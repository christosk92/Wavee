using Eum.UI.Items;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.Sources;

namespace Eum.UI.ViewModels.Search;

public class SearchGroupViewModel : ISearchGroup, IEquatable<SearchGroupViewModel>
{
    public string Title { get; init; }
    public string Id { get; init; }
    public ComposedKey Key => new(Id);
    public ServiceType Source { get; init; }

    public async Task FetchData(int offset, int limit)
    {

    }

    public bool Equals(SearchGroupViewModel? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }
    public bool Equals(ISearchGroup? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SearchGroupViewModel)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(SearchGroupViewModel? left, SearchGroupViewModel? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SearchGroupViewModel? left, SearchGroupViewModel? right)
    {
        return !Equals(left, right);
    }
}