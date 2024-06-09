using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Common;
using Wavee.Contracts.Enums;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.WinUI;

namespace Wavee.UI.ViewModels;

public sealed partial class AccountViewModel : ReactiveObject
{
    private readonly BehaviorSubject<bool> _isSignedInSubj = new(false);
    private readonly BehaviorSubject<Guid?> _userIdSubj = new(null);
    private readonly BehaviorSubject<IAccountClient> _clientSubj = new(null);


    [AutoNotify] private bool _isSignedIn;
    [AutoNotify] private Guid? _userId;
    [AutoNotify] private string? _userName;
    [AutoNotify] private string? _userProfilePicture;
    [AutoNotify] private string? _externalId;

    private readonly IAccountClientFactory _clientFactory;
    public AccountViewModel(IAccountClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
        Library = new AccountLibrary();

        IsSignedInObservable = _isSignedInSubj.AsObservable();
        _isSignedInSubj.Subscribe(x => IsSignedIn = x);
        _userIdSubj.Subscribe(x =>
        {
            UserId = x;
            Library.UserId = x;
        });

        SignInSpotifyCommand = ReactiveCommand.CreateFromTask(SigninSpotifyWithBrowser);
    }

    private Task<Unit> SigninSpotifyWithBrowser(CancellationToken arg)
    {
        //TODO:
        _userIdSubj.OnNext(Guid.NewGuid());
        UserName = "Test User";
        ExternalId = "testuser";
        _isSignedInSubj.OnNext(true);
        _clientSubj.OnNext(_clientFactory.Create());

        Library.AddPinnableItem(new TestPin(), UserId);
        return Task.FromResult(Unit.Default);
    }

    public IObservable<IAccountClient?> Client => _clientSubj;
    public IObservable<bool> IsSignedInObservable { get; }
    public ReactiveCommand<Unit, Unit> SignInSpotifyCommand { get; }
    public AccountLibrary Library { get; }

    private sealed class TestPin : IPinnableItem, IComparable<TestPin>
    {
        public TestPin()
        {
            Id = Guid.NewGuid().ToString();
            Name = "Test Pin";
            Type = ItemType.Album;
        }

        public string Id { get; }
        public string Name { get; }
        public UrlImage[] Images { get; }
        public string Color { get; set; }

        public int CompareTo(IPinnableItem other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(TestPin other)
        {
            throw new NotImplementedException();
        }

        public ItemType Type { get; }
    }
}

public sealed partial class AccountLibrary : ReactiveObject
{
    [AutoNotify] private Guid? _userId;
    private readonly SourceCache<Owned<IPinnableItem>, string> _pinsSourceCache;
    private readonly SourceCache<Owned<IPlaylist>, string> _playlistsSourceCache;
    private readonly SourceCache<Owned<ILikedSong>, string> _likedSongsSourceCache;
    private readonly SourceCache<Owned<ILikedAlbum>, string> _albumsSourceCache;
    private readonly SourceCache<Owned<ILikedArtist>, string> _artistsSourceCache;
    private readonly SourceCache<Owned<IFolder>, string> _foldersSourceCache;

    public AccountLibrary()
    {
        _pinsSourceCache = new SourceCache<Owned<IPinnableItem>, string>(x => x.Id);
        _playlistsSourceCache = new SourceCache<Owned<IPlaylist>, string>(x => x.Id);
        _likedSongsSourceCache = new SourceCache<Owned<ILikedSong>, string>(x => x.Id);
        _albumsSourceCache = new SourceCache<Owned<ILikedAlbum>, string>(x => x.Id);
        _artistsSourceCache = new SourceCache<Owned<ILikedArtist>, string>(x => x.Id);
        _foldersSourceCache = new SourceCache<Owned<IFolder>, string>(x => x.Id);

        Pins = CreateUserIdObservable(_pinsSourceCache);
        Playlists = CreateUserIdObservable(_playlistsSourceCache);
        LikedSongs = CreateUserIdObservable(_likedSongsSourceCache);
        Albums = CreateUserIdObservable(_albumsSourceCache);
        Artists = CreateUserIdObservable(_artistsSourceCache);
        Folders = CreateUserIdObservable(_foldersSourceCache);
    }

    public bool AddPinnableItem(IPinnableItem item, Guid? userId = null)
    {
        if (UserId is null && userId is null)
            return false;

        if (userId is null)
            userId = UserId;

        _pinsSourceCache.AddOrUpdate(new Owned<IPinnableItem>(item.Id, item, userId.Value));
        return true;
    }

    public IObservable<IChangeSet<IPinnableItem, string>> Pins { get; }
    public IObservable<IChangeSet<IPlaylist, string>> Playlists { get; }
    public IObservable<IChangeSet<ILikedSong, string>> LikedSongs { get; }
    public IObservable<IChangeSet<ILikedAlbum, string>> Albums { get; }
    public IObservable<IChangeSet<ILikedArtist, string>> Artists { get; }
    public IObservable<IChangeSet<IFolder, string>> Folders { get; }

    private Func<Owned<T>, bool> MakeFilter<T>(Guid ownerId) where T : IItem
    {
        return x => x.OwnedBy == ownerId;
    }

    private IObservable<IChangeSet<T, string>> CreateUserIdObservable<T>(
        SourceCache<Owned<T>, string> cache) where T : IItem
    {
        var filter = this.WhenAnyValue(x => x.UserId)
            .Where(x => x.HasValue)
            .Select(x => MakeFilter<T>(x.Value));

        var items = cache.Connect()
            .Filter(filter)
            .Transform(x => x.Item);

        return items;
    }
}

public sealed class Owned<T> where T : IItem
{
    public Owned(string id, T item, Guid ownedBy)
    {
        Id = id;
        Item = item;
        OwnedBy = ownedBy;
    }

    public string Id { get; }
    public T Item { get; }
    public Guid OwnedBy { get; }
}