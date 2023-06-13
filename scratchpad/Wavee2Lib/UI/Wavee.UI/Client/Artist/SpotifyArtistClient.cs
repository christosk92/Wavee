using LanguageExt;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Eum.Spotify.context;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.UI.ViewModels.Playback;
using static LanguageExt.Prelude;

namespace Wavee.UI.Client.Artist;
internal static class SpotifyArtistClient
{
    public static async Task<SpotifyArtistView> FetchArtist(this SpotifyClient client, AudioId artistId)
    {
        const string fetch_uri = "hm://artist/v1/{0}/desktop?format=json&catalogue=premium&locale={1}&cat=1";
        var url = string.Format(fetch_uri, artistId.ToBase62(), "en");
        var aff =
            from mercuryClient in SuccessEff(client.Mercury)
            from response in mercuryClient.Get(url, CancellationToken.None).ToAff()
            select response;
        var result = await Task.Run(async () => await aff.Run());
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
            ? mnl.TryGetProperty("listener_count", out var lc) ? lc.GetUInt64() : 0
            : 0;

        var topTracks = new List<ArtistTopTrackView>();
        if (jsonDoc.RootElement.GetProperty("top_tracks")
            .TryGetProperty("tracks", out var toptr))
        {
            using var topTracksArr = toptr.EnumerateArray();
            int index = 0;
            var playcommandFortoptracks = ReactiveCommand.Create<AudioId, Unit>(x =>
            {
                var ctx = new PlayContextStruct(
                    ContextId: artistId.ToString(),
                    Index: topTracks.FindIndex(c => c.Id == x),
                    ContextUrl: $"context://{artistId.ToString()}",
                    TrackId: x,
                    NextPages: Option<IEnumerable<ContextPage>>.None,
                    PageIndex: Option<int>.None,
                    Metadata: LanguageExt.HashMap<string, string>.Empty
                );
                PlaybackViewModel.Instance.PlayCommand.Execute(ctx);
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
                        PlaybackViewModel.Instance.PlayCommand.Execute(new PlayContextStruct(
                            ContextId: artistid.ToString(),
                            Index: index,
                            TrackId: x,
                            ContextUrl: Option<string>.None,
                            NextPages: Option<IEnumerable<ContextPage>>.Some(nextPages),
                            PageIndex: 0,
                            Metadata: LanguageExt.HashMap<string, string>.Empty));
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