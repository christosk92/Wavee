using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using DynamicData;
using Eum.Spotify.context;
using Google.Protobuf.Collections;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.ViewModels.Artist;

public sealed class ArtistViewModel<R> : INotifyPropertyChanged, INavigableViewModel
    where R : struct, HasSpotify<R>, HasFile<R>, HasDirectory<R>, HasLocalPath<R>
{
    private R _runtime;
    public TaskCompletionSource ArtistFetched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);


    static ArtistViewModel()
    {
        PlayCommand = ReactiveCommand.CreateFromTask<PlayContextStruct, Unit>(async str =>
        {
            await ShellViewModel<R>.Instance.Playback.PlayContextAsync(str);
            return default;
        });
    }

    private readonly IDisposable _listener;
    private bool _following;

    public ArtistViewModel(R runtime)
    {
        _runtime = runtime;
        FollowCommand = ShellViewModel<R>.Instance.Library.SaveCommand;

        _listener = Spotify<R>.ObserveLibrary()
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe()
            .Where(c => c is
            {
                Initial: false, Item.Type: AudioItemType.Artist
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(c =>
            {
                if (c.Item == Artist?.Id)
                {
                    IsFollowing = !c.Removed;
                }
            });
    }

    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is not AudioId artistId)
            return;
        IsFollowing = ShellViewModel<R>.Instance.Library.InLibrary(artistId);
        var id = artistId.ToBase62();
        const string fetch_uri = "hm://artist/v1/{0}/desktop?format=json&catalogue=premium&locale={1}&cat=1";
        // const string fetch_uri = "hm://creatorabout/v0/artist-insights/{0}?format=json&locale={1}";
        var url = string.Format(fetch_uri, id, "en");
        var aff =
            from mercuryClient in Spotify<R>.Mercury().Map(x => x)
            from response in mercuryClient.Get(url, CancellationToken.None).ToAff()
            select response;
        var result = await aff.Run(runtime: _runtime);
        var r = result.ThrowIfFail();
        using var jsonDoc = JsonDocument.Parse(r.Payload);


        var info = jsonDoc.RootElement.GetProperty("info");
        var name = info.GetProperty("name").GetString();
        var headerImage = jsonDoc.RootElement.TryGetProperty("header_image", out var hd)
            ? hd.GetProperty("image")
                .GetString()
            : null;

        string profilePic = null;
        if (info.TryGetProperty("portraits", out var profil))
        {
            using var profilePics = profil.EnumerateArray();
            profilePic = profilePics.First().GetProperty("uri").GetString();
        }

        var monthlyListeners = jsonDoc.RootElement.TryGetProperty("monthly_listeners", out var mnl)
            ?
            (mnl.TryGetProperty("listener_count", out var lc) ? lc.GetUInt64() : 0) : 0;

        var topTracks = new List<ArtistTopTrackView>();
        if (jsonDoc.RootElement.GetProperty("top_tracks")
            .TryGetProperty("tracks", out var toptr))
        {
            using var topTracksArr = toptr.EnumerateArray();
            int index = 0;
            var playcommandFortoptracks = ReactiveCommand.Create<AudioId, Unit>(x =>
            {
                var ctx = new PlayContextStruct(
                    ContextId: Artist.Id,
                    Index: topTracks.FindIndex(c => c.Id == x),
                    ContextUrl: $"context://{Artist.Id.ToString()}",
                    TrackId: x,
                    NextPages: Option<IEnumerable<ContextPage>>.None,
                    PageIndex: Option<int>.None
                );
                PlayCommand.Execute(ctx);
                return default;
            });
            foreach (var topTrack in topTracksArr)
            {
                var release = topTrack.GetProperty("release");
                var releaseName = release.GetProperty("name").GetString();
                var releaseUri = release.GetProperty("uri").GetString();
                var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
                var track = new ArtistTopTrackView
                {
                    Uri = topTrack.GetProperty("uri")
                        .GetString(),
                    Playcount = topTrack.GetProperty("playcount")
                        is
                        {
                            ValueKind: JsonValueKind.Number
                        } e
                        ? e.GetUInt64()
                        : Option<ulong>.None,
                    ReleaseName = releaseName,
                    ReleaseUri = releaseUri,
                    ReleaseImage = releaseImage,
                    Title = topTrack.GetProperty("name")
                        .GetString(),
                    Id = AudioId.FromUri(topTrack.GetProperty("uri")
                        .GetString()),
                    Index = index++,
                    PlayCommand = playcommandFortoptracks,
                };
                topTracks.Add(track);
            }
        }

        var releases = jsonDoc.RootElement.GetProperty("releases");

        static void GetView(JsonElement releases,
            string key,
            bool canSwitchViews,
            List<ArtistDiscographyGroupView> output,
            AudioId artistid)
        {
            var albums = releases.GetProperty(key);
            var totalAlbums = albums.GetProperty("total_count").GetInt32();
            if (totalAlbums > 0)
            {
                var rl = albums.GetProperty("releases");
                using var albumReleases = rl.EnumerateArray();
                var albumsView = new List<ArtistDiscographyView>(rl.GetArrayLength());

                foreach (var release in albumReleases)
                {
                    var releaseUri = release.GetProperty("uri").GetString();
                    var releaseName = release.GetProperty("name").GetString();
                    var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
                    var year = release.GetProperty("year").GetUInt16();

                    var tracks = new List<ArtistDiscographyTrack>();
                    var playCommandForContext = ReactiveCommand.Create<AudioId, Unit>(x =>
                    {
                        //pages are for artists are like:
                        //hm://artistplaycontext/v1/page/spotify/album/{albumId}/km
                        var currentId = AudioId.FromUri(releaseUri);
                        // var pageUrl = $"hm://artistplaycontext/v1/page/spotify/album/{currentId}/km";
                        // //next pages:
                        // var nextPages = new RepeatedField<ContextPage>
                        // {
                        //     new ContextPage
                        //     {
                        //         PageUrl = pageUrl
                        //     }
                        // };
                        var nextPages =
                            output.SelectMany(y => y.Views)
                                .SkipWhile(z => z.Id != currentId).Select(albumView =>
                                    $"hm://artistplaycontext/v1/page/spotify/album/{albumView.Id.ToBase62()}/km")
                                .Select(nextPageUrl => new ContextPage { PageUrl = nextPageUrl });

                        var index = tracks.FindIndex(c => c.Id == x);
                        PlayCommand.Execute(new PlayContextStruct(
                            ContextId: artistid,
                            Index: index,
                            TrackId: x,
                            ContextUrl: None,
                            NextPages: Some(nextPages),
                            PageIndex: 0));
                        return default;
                    });

                    if (release.TryGetProperty("discs", out var discs))
                    {
                        using var discsArr = discs.EnumerateArray();
                        foreach (var disc in discsArr)
                        {
                            using var tracksInDisc = disc.GetProperty("tracks").EnumerateArray();
                            foreach (var track in tracksInDisc)
                            {
                                tracks.Add(new ArtistDiscographyTrack
                                {
                                    PlayCommand = playCommandForContext,
                                    Playcount = track.GetProperty("playcount")
                                        is
                                    {
                                        ValueKind: JsonValueKind.Number
                                    } e
                                        ? e.GetUInt64()
                                        : Option<ulong>.None,
                                    Title = track.GetProperty("name")
                                        .GetString(),
                                    Number = track.GetProperty("number")
                                        .GetUInt16(),
                                    Id = AudioId.FromUri(track.GetProperty("uri").GetString()),
                                    Duration = TimeSpan.FromMilliseconds(track.GetProperty("duration").GetUInt32()),
                                    IsExplicit = track.GetProperty("explicit").GetBoolean()
                                });
                            }
                        }
                    }
                    else
                    {
                        var tracksCount = release.GetProperty("track_count").GetUInt16();
                        tracks.AddRange(Enumerable.Range(0, tracksCount)
                            .Select(c => new ArtistDiscographyTrack
                            {
                                PlayCommand = playCommandForContext,
                                Playcount = Option<ulong>.None,
                                Title = null,
                                Number = (ushort)(c + 1),
                                Id = default,
                                Duration = default,
                                IsExplicit = false
                            }));
                    }

                    var pluralModifier = tracks.Count > 1 ? "tracks" : "track";
                    albumsView.Add(new ArtistDiscographyView
                    {
                        Id = AudioId.FromUri(releaseUri),
                        Title = releaseName,
                        Image = releaseImage,
                        Tracks = new ArtistDiscographyTracksHolder
                        {
                            Tracks = tracks,
                            AlbumId = AudioId.FromUri(releaseUri)
                        },
                        ReleaseDateAsStr = $"{year.ToString()} - {tracks.Count} {pluralModifier}"
                    });
                }

                var newGroup = new ArtistDiscographyGroupView
                {
                    GroupName = FirstCharToUpper(key),
                    Views = albumsView,
                    CanSwitchTemplates = canSwitchViews
                };

                output.Add(newGroup);
            }
        }


        var res = new List<ArtistDiscographyGroupView>(3);
        GetView(releases, "albums", true, res, artistId);
        GetView(releases, "singles", true, res, artistId);
        GetView(releases, "compilations", false, res, artistId);


        Artist = new ArtistView(
            name: name,
            headerImage: headerImage,
            monthlyListeners: monthlyListeners,
            topTracks: topTracks,
            res,
            profilePic,
            id: artistId
        );

        ArtistFetched.SetResult();
    }

    public static ReactiveCommand<PlayContextStruct, Unit> PlayCommand { get; set; }

    public ArtistView Artist { get; set; }

    public bool IsFollowing
    {
        get => _following;
        set => SetField(ref _following, value);
    }

    public ICommand FollowCommand { get; }

    public void OnNavigatedFrom()
    {

    }
    private static string FirstCharToUpper(string key)
    {
        ReadOnlySpan<char> sliced = key;
        return $"{char.ToUpper(sliced[0])}{sliced[1..]}";
    }

    public void Clear()
    {
        Artist.Clear();
        _listener.Dispose();
        _runtime = default;
        Artist = default;
        ArtistFetched = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class ArtistDiscographyGroupView
{
    public required string GroupName { get; set; }
    public required List<ArtistDiscographyView> Views { get; set; }
    public required bool CanSwitchTemplates { get; set; }
}
public class ArtistDiscographyView
{
    public string Title { get; set; }
    public string Image { get; set; }
    public AudioId Id { get; set; }
    public ArtistDiscographyTracksHolder Tracks { get; set; }
    public string ReleaseDateAsStr { get; set; }
}

public class ArtistDiscographyTracksHolder
{
    public List<ArtistDiscographyTrack> Tracks { get; set; }
    public AudioId AlbumId { get; set; }
}
public class ArtistDiscographyTrack
{
    public Option<ulong> Playcount { get; set; }
    public string Title { get; set; }
    public ushort Number { get; set; }
    public List<SpotifyAlbumArtistView> Artists { get; set; }
    public bool IsLoaded => !string.IsNullOrEmpty(Title);
    public AudioId Id { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsExplicit { get; set; }
    public required ICommand PlayCommand { get; set; }

    public ushort MinusOne(ushort v)
    {
        return (ushort)(v - 1);
    }

    public bool Negate(bool b)
    {
        return !b;
    }

    public string FormatPlaycount(Option<ulong> playcount)
    {
        return playcount.IsSome
            ? playcount.ValueUnsafe().ToString("N0")
            : "< 1,000";
    }

    public string FormatTimestamp(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"mm\:ss");
    }
}

public class ArtistTopTrackView
{
    public required string Uri { get; set; }
    public required Option<ulong> Playcount { get; set; }
    public required string ReleaseImage { get; set; }
    public required string ReleaseName { get; set; }
    public required string ReleaseUri { get; set; }
    public required string Title { get; set; }
    public required AudioId Id { get; set; }
    public required int Index { get; set; }
    public required ICommand PlayCommand { get; set; }

    public string FormatPlaycount(Option<ulong> playcount)
    {
        return playcount.IsSome
            ? playcount.ValueUnsafe().ToString("N0")
            : "< 1,000";
    }
}