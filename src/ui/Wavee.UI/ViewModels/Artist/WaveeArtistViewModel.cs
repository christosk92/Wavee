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

public sealed class WaveeArtistViewModel
{
    private static Dictionary<string, WeakReference<WaveeArtistViewModel>> _holders = new();

    public WaveeArtistViewModel(string id, string name, IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel> discography)
    {
        Id = id;
        Name = name;
        Discography = discography;
    }
    public string Id { get; }
    public string Name { get; }
    public IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel> Discography { get; }
    public double ScrollPosition { get; set; }
    public static WaveeArtistViewModel? GetOrCreate(string id, object item, IWaveeUIAuthenticatedProfile profile,
        IDispatcher dispatcher,
        IAsyncRelayCommand<WaveeAlbumTrackViewModel> playCommand)
    {
        if (_holders.TryGetValue(id, out var x) && x.TryGetTarget(out var y)) return y;

        switch (item)
        {
            case SpotifySimpleArtist spotifySimpleArtist:
                {
                    y = new WaveeArtistViewModel(
                        id: spotifySimpleArtist.Id,
                        name: spotifySimpleArtist.Name,
                        discography: ConstructFrom(spotifySimpleArtist.Discography, profile, dispatcher, playCommand));
                    _holders[id] = new WeakReference<WaveeArtistViewModel>(y);
                    return y;
                    break;
                }
        }

        return null;
    }

    private static IReadOnlyCollection<WaveeArtistDiscographyGroupViewModel> ConstructFrom(
        IEnumerable<IGrouping<SpotifyDiscographyType, SpotifyId>>? discography,
        IWaveeUIAuthenticatedProfile profile, IDispatcher dispatcher,
        IAsyncRelayCommand<WaveeAlbumTrackViewModel> playCommand)
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

            output.Add(new WaveeArtistDiscographyGroupViewModel(name, ids, profile, averageTracks, dispatcher, playCommand));
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
        IAsyncRelayCommand<WaveeAlbumTrackViewModel> playCommand)
    {
        Name = name;
        _profile = profile;
        GroupAverageTracks = groupAverageTracks;
        _ids = ids;
        TotalCount = ids.Length;

        Items = _ids.Select(x => new LazyWaveeAlbumViewModel(x, groupAverageTracks,
            b => Task.Run(async () => await Realize(b, dispatcher)), playCommand, profile)).ToImmutableArray();
        double estimatedHeight = 0;
        foreach (var item in Items)
        {
            estimatedHeight += groupAverageTracks * 40;
            _realizationParametersMap[item.IdNonBlocking] = new RealizationParameters(false, false);
        }

        EstimatedHeight = estimatedHeight;
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

        var album = await Task.Run(async () => await _profile.GetAlbum(obj.IdNonBlocking, playCommand: obj.PlayTrackCommand));
        dispatcher.Dispatch(() =>
        {
            obj.Value = album;
        }, true);
    }

    public string Name { get; }
    public int TotalCount { get; }
    public int GroupAverageTracks { get; }
    public double EstimatedHeight { get; }
    public IReadOnlyCollection<LazyWaveeAlbumViewModel> Items { get; }
}

public readonly record struct RealizationParameters(bool First, bool Realized);

public sealed class LazyWaveeAlbumViewModel : ObservableObject
{
    private readonly object _lock = new object();
    private bool _realized = false;
    private bool _realizedFirstTime = false;
    private WaveeAlbumViewModel? _value;
    private string _id;
    private Action<LazyWaveeAlbumViewModel> _action;
    private bool _imageLoaded;

    public LazyWaveeAlbumViewModel(string id, int lazyCount,
        Action<LazyWaveeAlbumViewModel> action,
        IAsyncRelayCommand<WaveeAlbumTrackViewModel> playTrackCommand,
        IWaveeUIAuthenticatedProfile profile)
    {
        _id = id;
        _action = action;
        PlayTrackCommand = playTrackCommand;
        Profile = profile;
        Value = new WaveeAlbumViewModel(id, lazyCount);
    }

    public string IdNonBlocking => _id;

    public string Id
    {
        get
        {
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

    public IAsyncRelayCommand<WaveeAlbumTrackViewModel> PlayTrackCommand { get; }
    public IWaveeUIAuthenticatedProfile Profile { get; }
}
public sealed class WaveeAlbumViewModel
{
    public WaveeAlbumViewModel(string id,
        string name,
        uint year,
        Seq<IWaveeTrackAlbum> tracks,
        string? mediumImageUrl,
        ICommand playCommand)
    {
        Id = id;
        Name = name;
        Year = year;
        Tracks = tracks.Select(x => new WaveeAlbumTrackViewModel(x, playCommand)).ToImmutableArray();
        MediumImageUrl = mediumImageUrl;
        Loaded = true;
    }

    public WaveeAlbumViewModel(string id, int lazyCount)
    {
        Id = id;
        Tracks = Enumerable.Range(0, lazyCount).Select(_ => new WaveeAlbumTrackViewModel(_)).ToImmutableArray();
        MediumImageUrl = "ms-appx:///Assets/AlbumPlaceholder.png";
        Loaded = false;
    }

    public string Id { get; }
    public string Name { get; }
    public uint Year { get; }
    public IReadOnlyCollection<WaveeAlbumTrackViewModel> Tracks { get; }
    public string? MediumImageUrl { get; }
    public bool Loaded { get; }
}

public sealed class WaveeAlbumTrackViewModel : WaveeTrackViewModel
{

    public WaveeAlbumTrackViewModel(IWaveeTrackAlbum item, ICommand playCommand) : base(new ComposedKey(item.Id), playCommand)
    {
        Item = item;
        Number = item.Number;
        Loaded = true;
        This = this;
    }

    public WaveeAlbumTrackViewModel(int number) : base(new ComposedKey(number), null)
    {
        Number = number;
        Loaded = false;
        This = this;
    }
    public int Number { get; }
    public IWaveeTrackAlbum Item { get; }
    public bool Loaded { get; }
    public WaveeAlbumTrackViewModel This { get; }

    public override string Name => Item.Name;

    public override bool Is(IWaveePlayableItem x, Option<string> uid)
    {
        if (x is null) return false;
        if (uid.IsSome)
        {
            var isEqual = uid.ValueUnsafe() == Item?.Uid;
            if (isEqual) return true;
        }

        var trackId = x.Id;
        var trackComposedKey = new ComposedKey(trackId);
        var y = base.Id.Equals(trackComposedKey);
        return y;
    }
}