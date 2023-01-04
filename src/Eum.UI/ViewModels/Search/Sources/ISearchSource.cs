using DynamicData;
using Eum.UI.ViewModels.Search.Patterns;
using Eum.UI.ViewModels.Search.SearchItems;

namespace Eum.UI.ViewModels.Search.Sources;

public interface ISearchSource
{
	IObservable<IChangeSet<ISearchItem, ComposedKey>> Changes { get; }

    IObservable<IChangeSet<ISearchGroup>> GroupChanges { get; }
    IObservable<bool> IsBusy { get; }
}

public interface ISearchGroup : IEquatable<ISearchGroup>
{
    string Id { get; }
    string Title { get; }
}