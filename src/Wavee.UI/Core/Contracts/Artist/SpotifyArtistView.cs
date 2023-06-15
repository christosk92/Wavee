using System.Text.Json;
using LanguageExt;
using System.Windows.Input;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using ReactiveUI;

namespace Wavee.UI.Core.Contracts.Artist;
public record SpotifyArtistView(
    AudioId Id,
    string Name, string ProfilePicture, string HeaderImage,
    ulong MonthlyListeners,
    IList<ArtistTopTrackView> TopTracks,
    IList<ArtistDiscographyGroupView> Discography)
{
    public static SpotifyArtistView From(JsonDocument jsonDoc, AudioId artistId)
    {
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
            ? mnl.TryGetProperty("listener_count", out var lc) ? lc.GetUInt64() : 0
            : 0;

        var topTracks = new List<ArtistTopTrackView>();
        if (jsonDoc.RootElement.GetProperty("top_tracks")
            .TryGetProperty("tracks", out var toptr))
        {
            using var topTracksArr = toptr.EnumerateArray();
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
        }

        var releases = jsonDoc.RootElement.GetProperty("releases");

        static void GetView(JsonElement releases,
            string key,
            bool canSwitchViews,
            bool alwaysHorizontal,
            List<ArtistDiscographyGroupView> output,
            AudioId artistid)
        {
            var albums = releases.GetProperty(key);
            var totalAlbums = albums.GetProperty("total_count").GetInt32();
            if (totalAlbums > 0)
            {
                var rl = albums.GetProperty("releases");
                using var albumReleases = rl.EnumerateArray();
                var albumsView = new List<ArtistDiscographyItem>(rl.GetArrayLength());

                foreach (var release in albumReleases)
                {
                    var releaseUri = release.GetProperty("uri").GetString();
                    var releaseName = release.GetProperty("name").GetString();
                    var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
                    var year = release.GetProperty("year").GetUInt16();

                    var tracks = new List<ArtistDiscographyTrack>();
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
                                Playcount = Option<ulong>.None,
                                Title = null,
                                Number = (ushort)(c + 1),
                                Id = default,
                                Duration = default,
                                IsExplicit = false
                            }));
                    }

                    var pluralModifier = tracks.Count > 1 ? "tracks" : "track";
                    albumsView.Add(new ArtistDiscographyItem
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
                static string FirstCharToUpper(string key)
                {
                    ReadOnlySpan<char> sliced = key;
                    return $"{char.ToUpper(sliced[0])}{sliced[1..]}";
                }
                var newGroup = new ArtistDiscographyGroupView
                {
                    GroupName = FirstCharToUpper(key),
                    Views = albumsView,
                    CanSwitchTemplates = canSwitchViews,
                    AlwaysHorizontal = alwaysHorizontal
                };

                output.Add(newGroup);
            }
        }


        var res = new List<ArtistDiscographyGroupView>(3);
        GetView(releases, "albums", true, false, res, artistId);
        GetView(releases, "singles", true, false, res, artistId);
        GetView(releases, "compilations", false, false, res, artistId);
        GetView(releases, "appears_on", false, true, res, artistId);

        return new SpotifyArtistView(
            Id: artistId,
            Name: name,
            ProfilePicture: profilePic,
            HeaderImage: headerImage,
            MonthlyListeners: monthlyListeners,
            TopTracks: topTracks,
            Discography: res
        );
    }
}

public class ArtistTopTrackView
{
    public string Uri { get; set; }
    public Option<ulong> Playcount { get; set; }
    public string ReleaseImage { get; set; }
    public string ReleaseName { get; set; }
    public string ReleaseUri { get; set; }
    public string Title { get; set; }
    public AudioId Id { get; set; }
    public int Index { get; set; }

    public string FormatPlaycount(Option<ulong> playcount)
    {
        return playcount.IsSome
            ? playcount.ValueUnsafe().ToString("N0")
            : "< 1,000";
    }
}
public class ArtistDiscographyGroupView
{
    public string GroupName { get; set; }
    public List<ArtistDiscographyItem> Views { get; set; }
    public bool CanSwitchTemplates { get; set; }
    public bool AlwaysHorizontal { get; set; }
}
public class ArtistDiscographyItem
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
public class SpotifyAlbumArtistView
{
    public string Name { get; set; }
    public AudioId Id { get; set; }
    public string Image { get; set; }
}