using System.Reactive.Linq;
using DynamicData;
using Wavee.UI.ViewModel.Search.Patterns;

namespace Wavee.UI.ViewModel.Search.Sources;

public class CompositeSearchSource : ISearchSource
{
    private readonly ISearchSource[] _sources;

    public CompositeSearchSource(params ISearchSource[] sources)
    {
        _sources = sources;
    }

    public IObservable<IChangeSet<ISearchItem, ComposedKey>> Changes => _sources.Select(r => r.Changes).Merge();
}