using System.Text.Json;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.ViewModels;

public sealed class ArtistViewModel<R> : ReactiveObject, INavigableViewModel
    where R : struct, HasSpotify<R>
{
    public ArtistView _artist;

    private readonly R _runtime;
    public TaskCompletionSource ArtistFetched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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

        using var topTracksArr = jsonDoc.RootElement.GetProperty("top_tracks")
            .GetProperty("tracks").EnumerateArray();
        var topTracks = LanguageExt.Seq<ArtistTopTrackView>.Empty;
        foreach (var topTrack in topTracksArr)
        {
            var release = topTrack.GetProperty("release");
            var releaseName = release.GetProperty("name").GetString();
            var releaseUri = release.GetProperty("uri").GetString();
            var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
            var track = new ArtistTopTrackView
            {
                Uri = topTrack.GetProperty("uri").GetString(),
                Playcount = topTrack.GetProperty("playcount")
                    is { ValueKind: JsonValueKind.Number } e
                ? e.GetUInt64() : Option<ulong>.None,
                ReleaseName = releaseName,
                ReleaseUri = releaseUri,
                ReleaseImage = releaseImage,
                Title = topTrack.GetProperty("name").GetString(),
            };
            topTracks = topTracks.Add(track);
        }

        var releases = jsonDoc.RootElement.GetProperty("releases");

        static void GetView(JsonElement releases,
            string key,
            bool canSwitchViews,
            List<ArtistDiscographyGroupView> output)
        {
            var albums = releases.GetProperty(key);
            var totalAlbums = albums.GetProperty("total_count").GetInt32();
            if (totalAlbums > 0)
            {
                using var albumReleases = albums.GetProperty("releases").EnumerateArray();
                var albumsView = LanguageExt.Seq<ArtistDiscographyView>.Empty;

                foreach (var release in albumReleases)
                {
                    var releaseUri = release.GetProperty("uri").GetString();
                    var releaseName = release.GetProperty("name").GetString();
                    var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
                    var year = release.GetProperty("year").GetUInt16();

                    Seq<ArtistDiscographyTrack> tracks = LanguageExt.Seq<ArtistDiscographyTrack>.Empty;
                    if (release.TryGetProperty("discs", out var discs))
                    {
                        using var discsArr = discs.EnumerateArray();
                        foreach (var disc in discsArr)
                        {
                            using var tracksInDisc = disc.GetProperty("tracks").EnumerateArray();
                            foreach (var track in tracksInDisc)
                            {
                                tracks = tracks.Add(new ArtistDiscographyTrack
                                {
                                    Playcount = track.GetProperty("playcount")
                                        is { ValueKind: JsonValueKind.Number } e
                                        ? e.GetUInt64()
                                        : Option<ulong>.None,
                                    Title = track.GetProperty("name").GetString(),
                                    Number = track.GetProperty("number")
                                        .GetUInt16()
                                });
                            }
                        }
                    }
                    else
                    {
                        var tracksCount = release.GetProperty("track_count").GetUInt16();
                        tracks = Enumerable.Range(0, tracksCount)
                            .Select(c => new ArtistDiscographyTrack
                            {
                                Playcount = Option<ulong>.None,
                                Title = null,
                                Number = (ushort)(c + 1)
                            }).ToSeq();
                    }

                    var pluralModifier = tracks.Length > 1 ? "tracks" : "track";
                    albumsView = albumsView.Add(new ArtistDiscographyView
                    {
                        Id = releaseUri,
                        Title = releaseName,
                        Image = releaseImage,
                        Tracks = tracks,
                        ReleaseDateAsStr = $"{year.ToString()} - {tracks.Length} {pluralModifier}"
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
        GetView(releases, "albums", true, res);
        GetView(releases, "singles", true, res);
        GetView(releases, "compilations", false, res);


        _artist = new ArtistView(
            name: name,
            headerImage: headerImage,
            monthlyListeners: monthlyListeners,
            topTracks: topTracks,
            res.ToSeq(),
            profilePic,
            id: artistId.ToBase62()
        );

        ArtistFetched.SetResult();
    }
    public ArtistView Artist => _artist;
    public void OnNavigatedFrom()
    {

    }
    private static string FirstCharToUpper(string key)
    {
        ReadOnlySpan<char> sliced = key;
        return $"{char.ToUpper(sliced[0])}{sliced[1..]}";
    }
}

public readonly struct ArtistView
{
    public string Name { get; }
    public string HeaderImage { get; }
    public ulong MonthlyListeners { get; }
    public Seq<ArtistTopTrackView> TopTracks { get; }
    public Seq<ArtistDiscographyGroupView> Discography { get; }
    public string ProfilePicture { get; }
    public string Id { get; }

    public ArtistView(string name, string headerImage, ulong monthlyListeners, Seq<ArtistTopTrackView> topTracks, Seq<ArtistDiscographyGroupView> discography, string profilePicture, string id)
    {
        Name = name;
        HeaderImage = headerImage;
        MonthlyListeners = monthlyListeners;
        TopTracks = topTracks;
        Discography = discography;
        ProfilePicture = profilePicture;
        Id = id;
    }
}
public readonly struct ArtistDiscographyGroupView
{
    public required string GroupName { get; init; }
    public required Seq<ArtistDiscographyView> Views { get; init; }
    public required bool CanSwitchTemplates { get; init; }
}
public readonly struct ArtistDiscographyView
{
    public required string Title { get; init; }
    public required string Image { get; init; }
    public required string Id { get; init; }
    public Seq<ArtistDiscographyTrack> Tracks { get; init; }
    public required string ReleaseDateAsStr { get; init; }
}
public readonly struct ArtistDiscographyTrack
{
    public required Option<ulong> Playcount { get; init; }
    public required string Title { get; init; }
    public required ushort Number { get; init; }
    public bool IsLoaded => !string.IsNullOrEmpty(Title);
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
}

public readonly struct ArtistTopTrackView
{
    public required string Uri { get; init; }
    public required Option<ulong> Playcount { get; init; }
    public required string ReleaseImage { get; init; }
    public required string ReleaseName { get; init; }
    public required string ReleaseUri { get; init; }
    public required string Title { get; init; }

    public string FormatPlaycount(Option<ulong> playcount)
    {
        return playcount.IsSome
            ? playcount.ValueUnsafe().ToString("N0")
            : "< 1,000";
    }
}