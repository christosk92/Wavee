using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using NeoSmart.AsyncLock;
using TagLib.Flac;
using Wavee.Spfy;
using Wavee.Spfy.Items;
using Wavee.UI.Providers;
using Wavee.UI.Services;

namespace Wavee.UI.ViewModels.Artist;

public sealed class WaveeArtistViewModel : WaveePlayableItemViewModel
{
    private record CachedEntry(WaveeArtistViewModel Item, DateTimeOffset CachedAt);
    private static readonly Dictionary<string, CachedEntry> _holders = new();

    static WaveeArtistViewModel()
    {
        TimeSpan MAX_LIFE = TimeSpan.FromMinutes(10);

        new Thread(async () =>
            {
                while (true)
                {
                    foreach (var holder in _holders.ToList())
                    {
                        var val = holder.Value;
                        var age = DateTimeOffset.UtcNow - val.CachedAt;
                        if (age >= MAX_LIFE)
                        {
                            foreach (var waveeArtistDiscographyGroupViewModel in holder.Value.Item.Discography)
                            {
                                waveeArtistDiscographyGroupViewModel.Clear();
                            }

                            holder.Value.Item.Discography = null;
                            _holders.Remove(holder.Key);
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }).Start();
    }

    public WaveeArtistViewModel(string id, string name, IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel> discography)
        : base(id, null)
    {
        Name = name;
        Discography = discography;
    }
    public override string Name { get; }
    public override bool Is(IWaveePlayableItem x, Option<string> uid, string eContextId)
    {
        return eContextId == Id || x.Descriptions.HeadOrNone().Match(
            None: () => false,
            Some: y => y.Id == Id
        );
    }
    public IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel> Discography { get; set; }
    public double ScrollPosition { get; set; }

    public static WaveeArtistViewModel? GetOrCreate(string id, object item, IWaveeUIAuthenticatedProfile profile,
        IDispatcher dispatcher,
        IAsyncRelayCommand<WaveeAlbumPlayableItemViewModel> playCommand)
    {
        if (_holders.TryGetValue(id, out var x))
        {
            return x.Item;
        }

        switch (item)
        {
            case SpotifySimpleArtist spotifySimpleArtist:
                {
                    var y = new WaveeArtistViewModel(
                        id: spotifySimpleArtist.Id,
                        name: spotifySimpleArtist.Name,
                        discography: ConstructFrom(spotifySimpleArtist.Id, spotifySimpleArtist.Discography, profile, dispatcher, playCommand));
                    _holders[id] = new CachedEntry(y, DateTimeOffset.UtcNow);
                    return y;
                }
        }

        return null;
    }

    private static IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel> ConstructFrom(
        string artistId,
        IEnumerable<IGrouping<SpotifyDiscographyType, SpotifyId>>? discography,
        IWaveeUIAuthenticatedProfile profile, IDispatcher dispatcher,
        IAsyncRelayCommand<WaveeAlbumPlayableItemViewModel> playCommand)
    {
        if (discography is null) return ImmutableArray<WaveeArtistDiscographyGroupViewModel>.Empty;
        var output = new List<WaveeArtistDiscographyGroupViewModel>(3);
        foreach (var groupInput in discography)
        {
            var name = groupInput.Key switch
            {
                SpotifyDiscographyType.Album => "ALBUMS",
                SpotifyDiscographyType.Single => "SINGLES",
                SpotifyDiscographyType.Compilation => "COMPILATIONS"
            };
            var averageTracks = groupInput.Key switch
            {
                SpotifyDiscographyType.Album => 10,
                SpotifyDiscographyType.Single => 2,
                SpotifyDiscographyType.Compilation => 10
            };
            var ids = groupInput.Select(x => x.ToString()).ToImmutableArray();

            output.Add(new WaveeArtistDiscographyGroupViewModel(name, ids, profile, averageTracks, dispatcher, playCommand, artistId));
        }

        return output;
    }
}

public class WaveeArtistDiscographyGroupViewModel
{
    private readonly IReadOnlyCollection<string> _ids;
    private readonly IWaveeUIAuthenticatedProfile _profile;

    private readonly AsyncLock _lock = new AsyncLock();
    private readonly Dictionary<string, RealizationParameters> _realizationParametersMap = new();

    public WaveeArtistDiscographyGroupViewModel(string name, ImmutableArray<string> ids,
        IWaveeUIAuthenticatedProfile profile,
        int groupAverageTracks,
        IDispatcher dispatcher,
        IAsyncRelayCommand<WaveeAlbumPlayableItemViewModel> playCommand, string artistId)
    {
        Name = name;
        _profile = profile;
        GroupAverageTracks = groupAverageTracks;
        ArtistId = artistId;
        _ids = ids;
        TotalCount = ids.Length;

        Items = _ids.Select(x => new LazyWaveeAlbumViewModel(x, groupAverageTracks,
            b => Task.Run(async () => await Realize(b, dispatcher)), playCommand, profile, this)).ToImmutableArray();
        double estimatedHeight = 0;
        foreach (var item in Items)
        {
            estimatedHeight += groupAverageTracks * 40;
            _realizationParametersMap[item.IdNonBlocking] = new RealizationParameters(false, false);
        }

        EstimatedHeight = estimatedHeight;
    }

    public void Clear()
    {
        foreach (var item in Items)
        {
            if (item.Value is not null)
            {
                item.Value.Tracks = null;
            }

            item.Profile = null;
            item.ClearAction();
        }

        Items = null;
    }
    private async Task Realize(LazyWaveeAlbumViewModel obj, IDispatcher dispatcher)
    {
        using (await _lock.LockAsync())
        {
            var parameters = _realizationParametersMap[obj.IdNonBlocking];
            if (!parameters.First)
            {
                _realizationParametersMap[obj.IdNonBlocking] = parameters with
                {
                    First = true
                };
                return;
            }

            if (!parameters.Realized)
            {
                Debug.WriteLine($"Requesting {obj.IdNonBlocking}");
                _realizationParametersMap[obj.IdNonBlocking] = parameters with
                {
                    Realized = true
                };
            }
            else
            {
                return;
            }
        }

        var album = await Task.Run(async () => await _profile.GetAlbum(obj.IdNonBlocking, playCommand: obj.PlayTrackCommand, group: obj.Parent));
        obj.ClearAction();
        dispatcher.Dispatch(() =>
        {
            obj.Value = album;
        }, true);
    }

    public string Name { get; }
    public int TotalCount { get; }
    public int GroupAverageTracks { get; }
    public double EstimatedHeight { get; }
    public IReadOnlyCollection<LazyWaveeAlbumViewModel> Items { get; set; }
    public string ArtistId { get; }
}

public readonly record struct RealizationParameters(bool First, bool Realized);

public sealed class LazyWaveeAlbumViewModel : ObservableObject
{
    private WaveeAlbumViewModel? _value;
    private string _id;
    private Action<LazyWaveeAlbumViewModel>? _action;
    private bool _imageLoaded;

    public LazyWaveeAlbumViewModel(string id, int lazyCount,
        Action<LazyWaveeAlbumViewModel> action,
        IAsyncRelayCommand<WaveeAlbumPlayableItemViewModel> playTrackCommand,
        IWaveeUIAuthenticatedProfile profile,
        WaveeArtistDiscographyGroupViewModel parent)
    {
        _id = id;
        _action = action;
        PlayTrackCommand = playTrackCommand;
        Profile = profile;
        Parent = parent;
        Value = new WaveeAlbumViewModel(id, lazyCount, parent);
    }

    public void ClearAction() => _action = null;
    public string IdNonBlocking => _id;

    public string Id
    {
        get
        {
            if (_action is not null)
                _action(this);
            return _id;
        }
    }
    public WaveeAlbumViewModel? Value
    {
        get => _value;
        internal set => SetProperty(ref _value, value);
    }
    public bool ImageLoaded
    {
        get => _imageLoaded; set => SetProperty(ref _imageLoaded, value);
    }

    public IAsyncRelayCommand<WaveeAlbumPlayableItemViewModel> PlayTrackCommand { get; }
    public IWaveeUIAuthenticatedProfile Profile { get; set; }
    public WaveeArtistDiscographyGroupViewModel Parent { get; set; }
}
public sealed class WaveeAlbumViewModel
{
    public WaveeAlbumViewModel(string id,
        string name,
        uint year,
        Seq<IWaveeAlbumTrack> tracks,
        string? mediumImageUrl,
        ICommand playCommand, WaveeArtistDiscographyGroupViewModel parent)
    {
        Id = id;
        Name = name;
        Year = year;
        Tracks = tracks.Select(x => new WaveeAlbumPlayableItemViewModel(this, x, playCommand)).ToImmutableList();
        MediumImageUrl = mediumImageUrl;
        Parent = parent;
        Loaded = true;
    }

    public WaveeAlbumViewModel(string id, int lazyCount, WaveeArtistDiscographyGroupViewModel parent)
    {
        Id = id;
        Parent = parent;
        Tracks = Enumerable.Range(0, lazyCount).Select(_ => new WaveeAlbumPlayableItemViewModel(this, _)).ToImmutableList();
        MediumImageUrl = "ms-appx:///Assets/AlbumPlaceholder.png";
        Loaded = false;
    }
    public WaveeArtistDiscographyGroupViewModel Parent { get; }
    public string Id { get; }
    public string Name { get; }
    public uint Year { get; }
    public IReadOnlyCollection<WaveeAlbumPlayableItemViewModel> Tracks { get; set; }
    public string? MediumImageUrl { get; }
    public bool Loaded { get; }
}

public sealed class WaveeAlbumPlayableItemViewModel : WaveePlayableItemViewModel
{

    public WaveeAlbumPlayableItemViewModel(WaveeAlbumViewModel parent, IWaveeAlbumTrack item, ICommand playCommand) : base(item.Id, playCommand)
    {
        Parent = parent;
        Item = item;
        Number = item.Number;
        Loaded = true;
    }

    public WaveeAlbumPlayableItemViewModel(WaveeAlbumViewModel parent, int number) : base(number.ToString(), null)
    {
        Parent = parent;
        Number = number;
        Loaded = false;
    }

    public WaveeAlbumViewModel Parent { get; set; }
    public int Number { get; }
    public IWaveeAlbumTrack Item { get; }
    public bool Loaded { get; }
    public override string Name => Item.Name;

    public override bool Is(IWaveePlayableItem x, Option<string> uid, string eContextId)
    {
        if (x is null) return false;
        if (uid.IsSome)
        {
            var isEqual = uid.ValueUnsafe() == Item?.Uid;
            if (isEqual) return true;
        }

        var trackId = x.Id;
        var y = base.Id.Equals(trackId);
        return y;
    }
}