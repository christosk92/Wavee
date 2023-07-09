using DynamicData;
using Wavee.UI.ViewModel.Search.Patterns;

namespace Wavee.UI.ViewModel.Search.Sources;

public interface ISearchSource
{
    IObservable<IChangeSet<ISearchItem, ComposedKey>> Changes { get; }
    IObservable<IChangeSet<FilterItem, string>> Filters { get; }
}