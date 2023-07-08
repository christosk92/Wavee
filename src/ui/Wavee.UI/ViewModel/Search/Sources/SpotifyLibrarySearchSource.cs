using DynamicData;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Search.Patterns;

namespace Wavee.UI.ViewModel.Search.Sources;

internal class SpotifyLibrarySearchSource : ReactiveObject, ISearchSource, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public SpotifyLibrarySearchSource(UserViewModel user, Subject<string> filterChanged)
    {
        //nothing for now
        Changes = new SourceCache<ISearchItem, ComposedKey>(x => x.Key)
            .DisposeWith(_disposables)
            .Connect();
    }

    public IObservable<IChangeSet<ISearchItem, ComposedKey>> Changes { get; }
    public void Dispose()
    {
        _disposables.Dispose();
    }
}