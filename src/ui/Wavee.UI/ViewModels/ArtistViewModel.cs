using System.Collections.ObjectModel;
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

namespace Wavee.UI.ViewModels;

public sealed class ArtistViewModel<R> : INavigableViewModel
    where R : struct, HasSpotify<R>, HasFile<R>, HasDirectory<R>, HasLocalPath<R>
{
    private R _runtime;
    public TaskCompletionSource ArtistFetched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);


    static ArtistViewModel()
    {
        PlayCommand = ReactiveCommand.CreateFromTask<PlayContextStruct, Unit>(async str =>
        {
            await ShellViewModel<R>.Instance.Playback.PlayContextAsync(str);
            return default(Unit);
        });
    }
    public ArtistViewModel(R runtime)
    {
        _runtime = runtime;
    }

    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is not AudioId artistId)
            return;

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

        using var profilePics = info.GetProperty("portraits")
            .EnumerateArray();
        var profilePic = profilePics.First().GetProperty("uri").GetString();

        var monthlyListeners = jsonDoc.RootElement.GetProperty("monthly_listeners")
            .GetProperty("listener_count")
            .GetUInt64();

        var toptr = jsonDoc.RootElement.GetProperty("top_tracks")
            .GetProperty("tracks");
        using var topTracksArr = toptr.EnumerateArray();
        var topTracks = new List<ArtistTopTrackView>(toptr.GetArrayLength());
        int index = 0;
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
            };
            topTracks.Add(track);
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
                        var currentId = AudioId.FromUri(releaseUri).ToBase62();
                        var pageUrl = $"hm://artistplaycontext/v1/page/spotify/album/{currentId}/km";
                        //next pages:
                        var nextPages = new RepeatedField<ContextPage>
                        {
                            new ContextPage
                            {
                                PageUrl = pageUrl
                            }
                        };
                        nextPages.AddRange(
                            albumsView.Select(albumView =>
                                $"hm://artistplaycontext/v1/page/spotify/album/{albumView.Id.ToBase62()}/km")
                                .Select(nextPageUrl => new ContextPage { PageUrl = nextPageUrl }));
                        var index = tracks.FindIndex(c => c.Id == x);
                        PlayCommand.Execute(new PlayContextStruct(artistid,
                            index,
                            nextPages,
                            0));
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
        _runtime = default;
        Artist = default;
        ArtistFetched = null;
    }
}

public class ArtistView
{
    public string Name { get; }
    public string HeaderImage { get; }
    public ulong MonthlyListeners { get; }
    public List<ArtistTopTrackView> TopTracks { get; set; }
    public List<ArtistDiscographyGroupView> Discography { get; set; }
    public string ProfilePicture { get; }
    public AudioId Id { get; }

    public ArtistView(string name, string headerImage, ulong monthlyListeners, List<ArtistTopTrackView>
        topTracks, List<ArtistDiscographyGroupView> discography, string profilePicture, AudioId id)
    {
        Name = name;
        HeaderImage = headerImage;
        MonthlyListeners = monthlyListeners;
        TopTracks = topTracks;
        Discography = discography;
        ProfilePicture = profilePicture;
        Id = id;
    }

    public void Clear()
    {
        TopTracks.Clear();
        foreach (var artistDiscographyGroupView in Discography)
        {
            foreach (var artistDiscographyView in artistDiscographyGroupView.Views)
                artistDiscographyView.Tracks.Tracks.Clear();

            artistDiscographyGroupView.Views.Clear();
        }

        Discography.Clear();
        Discography = null;
        TopTracks = null;
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
    public ICommand PlayCommand { get; set; }

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

    public string FormatPlaycount(Option<ulong> playcount)
    {
        return playcount.IsSome
            ? playcount.ValueUnsafe().ToString("N0")
            : "< 1,000";
    }
}