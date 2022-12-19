using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.ViewModels.Search.SearchItems;
using Eum.UI.ViewModels.Search.Sources;

namespace Eum.UI.ViewModels.Search;

public class SearchOverviewViewModel : ISearchGroup
{
    public string Id { get; init; }
    public string Title { get; init; }

    protected bool Equals(SearchOverviewViewModel other)
    {
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
        return Equals((SearchOverviewViewModel)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(SearchOverviewViewModel? left, SearchOverviewViewModel? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SearchOverviewViewModel? left, SearchOverviewViewModel? right)
    {
        return !Equals(left, right);
    }
}