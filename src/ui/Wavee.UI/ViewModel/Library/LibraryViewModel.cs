using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Eum.Spotify;
using LanguageExt;
using ReactiveUI;
using Wavee.Id;
using Wavee.UI.Client.Library;
using Wavee.UI.User;

namespace Wavee.UI.ViewModel.Library;

public sealed class LibraryViewModel : ObservableObject
{
    private readonly SourceCache<WaveeUILibraryItem, string> _items = new(x => x.Id);
    private readonly UserViewModel _user;
    private readonly Action<AudioItemType, int> _added;
    private readonly Action<AudioItemType, int> _removed;

    private IDisposable? _subscription;
    public LibraryViewModel(UserViewModel user, Action<AudioItemType, int> added, Action<AudioItemType, int> removed)
    {
        _user = user;
        _added = added;
        _removed = removed;
    }

    public async Task Initialize()
    {
        _subscription?.Dispose();

        _items.Clear();
        var result = await _user.Client.Library.InitializeLibraryAsync(CancellationToken.None);
        foreach (var item in result.Ids)
        {
            _items.AddOrUpdate(item);
        }
        var groups = _items.Items.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Count());
        foreach (var (key, value) in groups)
        {
            _added(key, value);
        }
        _subscription = _user.Client.Library
            .CreateListener()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(notification =>
            {
                _items.Edit((x) =>
                {
                    if (notification.Added)
                    {
                        foreach (var item in notification.Ids)
                        {
                            x.AddOrUpdate(item);
                        }

                        var groups = notification.Ids.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Count());
                        foreach (var (key, value) in groups)
                        {
                            _added(key, value);
                        }
                    }
                    else
                    {
                        foreach (var item in notification.Ids)
                        {
                            x.Remove(item);
                        }

                        var groups = notification.Ids.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.Count());
                        foreach (var (key, value) in groups)
                        {
                            _removed(key, value);
                        }
                    }
                });
            });
    }

    public bool InLibrary(string id)
    {
        return _items.Lookup(id).HasValue;
    }

    public IObservable<bool> CreateListener(string id)
    {
        //create listener for added/removed
        return _items
            .Connect()
            .Filter(x => string.Equals(x.Id, id, StringComparison.InvariantCultureIgnoreCase))
            .Select(changes =>
            {
                foreach (var change in changes)
                {
                    switch (change.Reason)
                    {
                        case ChangeReason.Add:
                            return true;
                        case ChangeReason.Remove:
                            return false;
                            break;
                    }
                }
                return false;
            });
    }
}