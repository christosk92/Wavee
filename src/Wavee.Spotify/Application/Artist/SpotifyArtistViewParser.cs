using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using TagLib.Matroska;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Playlists;

namespace Wavee.Spotify.Application.Artist;

internal static class SpotifyArtistViewParser
{
    public static SpotifyArtistView Parse(JsonElement artist)
    {
        SpotifyId uri = SpotifyId.FromUri(artist.GetProperty("uri").GetString()!);
        var discography = ParseDiscography(artist.GetProperty("discography"));
        var profile = ParseProfile(artist.GetProperty("profile"));
        var saved = artist.GetProperty("saved").GetBoolean();
        var relatedContent = ParseRelatedContent(artist.GetProperty("relatedContent"));
        var stats = ParseStats(artist.GetProperty("stats"));
        var visuals = ParseVisuals(artist.GetProperty("visuals"));

        return new SpotifyArtistView
        {
            Discography = discography,
            Profile = profile,
            Related = relatedContent,
            Stats = stats,
            Visuals = visuals,
            Saved = saved,
            Id = uri
        };
    }

    private static SpotifyArtistViewVisuals ParseVisuals(JsonElement getProperty)
    {
        var avatarImage = getProperty.GetProperty("avatarImage").GetProperty("sources");
        var avatarImageParsed = ParseImages(avatarImage);

        var headerImage = getProperty.GetProperty("headerImage");
        IReadOnlyCollection<SpotifyImage> headerImageParsed = ImmutableArray<SpotifyImage>.Empty;
        if (headerImage.ValueKind is not JsonValueKind.Null)
        {
            headerImageParsed = ParseImages(headerImage.GetProperty("sources"));
        }

        return new SpotifyArtistViewVisuals
        {
            AvatarImage = avatarImageParsed,
            HeaderImage = headerImageParsed.Count > 0 ? headerImageParsed.Single() : null
        };
    }

    private static SpotifyArtistViewStats ParseStats(JsonElement getProperty)
    {
        var followers = getProperty.GetProperty("followers").GetUInt64();
        var monthlyListeners = getProperty.GetProperty("monthlyListeners").GetUInt64();

        var worldRank = getProperty.GetProperty("worldRank");
        ushort? worldRankVal = null;
        if (worldRank.ValueKind is not JsonValueKind.Null)
        {
            worldRankVal = worldRank.GetUInt16();
        }

        return new SpotifyArtistViewStats
        {
            Followers = followers,
            MonthlyListeners = monthlyListeners,
            Worldrank = worldRankVal
        };
    }

    private static SpotifyArtistViewRelatedContent ParseRelatedContent(JsonElement getProperty)
    {
        var appearsOn = getProperty.GetProperty("appearsOn");
        var appearsOnParsed = ParseDiscographyGroup(appearsOn, ParseAlbum);

        var discoveredOn = getProperty.GetProperty("discoveredOnV2");
        var discoveredOnParsed = ParseDiscographyGroup(discoveredOn, ParsePlaylist);

        var featuring = getProperty.GetProperty("featuringV2");
        var featuringParsed = ParseDiscographyGroup(featuring, ParsePlaylist);

        var relatedArtists = getProperty.GetProperty("relatedArtists");
        var relatedArtistsParsed = ParseDiscographyGroup(relatedArtists, ParseArtist);

        return new SpotifyArtistViewRelatedContent
        {
            AppearsOn = appearsOnParsed,
            DiscoveredOn = discoveredOnParsed,
            FeaturedIn = featuringParsed,
            RelatedArtists = relatedArtistsParsed
        };
    }

    private static SpotifyArtistViewProfile ParseProfile(JsonElement getProperty)
    {
        var name = getProperty.GetProperty("name").GetString()!;
        var pinnedItem = getProperty.GetProperty("pinnedItem");
        var playlists = getProperty.GetProperty("playlistsV2");
        var playlistsParsed = ParseDiscographyGroup(playlists, ParsePlaylist);
        var verified = getProperty.GetProperty("verified").GetBoolean();
        var externalLinks = getProperty.GetProperty("externalLinks").GetProperty("items");
        var output = new Dictionary<string, string>();
        using var enumerator = externalLinks.EnumerateArray();
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            var linkName = curr.GetProperty("name").GetString()!;
            var linkUrl = curr.GetProperty("url").GetString()!;
            output.Add(linkName, linkUrl);
        }
        return new SpotifyArtistViewProfile
        {
            Name = name,
            PinnedItem = null,
            Playlists = playlistsParsed.Items,
            SocialLinks = output,
            Verified = verified
        };
    }



    private static SpotifyArtistViewDiscography ParseDiscography(JsonElement property)
    {
        var albums = property.GetProperty("albums");
        var albumsParsed = ParseDiscographyGroup(albums, ParseAlbum);

        var singles = property.GetProperty("singles");
        var singlesParsed = ParseDiscographyGroup(singles, ParseAlbum);

        var compilations = property.GetProperty("compilations");
        var compilationsParsed = ParseDiscographyGroup(compilations, ParseAlbum);

        var latest = property.GetProperty("latest");
        SpotifySimpleAlbum? latestParsed = null;
        if (latest.ValueKind is not JsonValueKind.Null)
            latestParsed = ParseAlbum(latest);

        var popularReleases = property.GetProperty("popularReleasesAlbums");
        var popularReleasesParsed = ParseDiscographyGroup(popularReleases, ParseAlbum);

        var topTracks = property.GetProperty("topTracks");
        var topTracksParsed = ParseTopTracks(topTracks);

        return new SpotifyArtistViewDiscography
        {
            Albums = albumsParsed,
            Singles = singlesParsed,
            Compilations = compilationsParsed,
            LatestRelease = latestParsed,
            PopularReleases = popularReleasesParsed,
            TopTracks = topTracksParsed
        };
    }

    private static IReadOnlyCollection<SpotifyArtistTopTrack> ParseTopTracks(JsonElement topTracks)
    {
        var items = topTracks.GetProperty("items");
        Span<SpotifyArtistTopTrack> itemsParsed = new SpotifyArtistTopTrack[items.GetArrayLength()];
        int items_idx = 0;
        using var itemsEnumerator = items.EnumerateArray();
        while (itemsEnumerator.MoveNext())
        {
            var item = itemsEnumerator.Current;
            var uid = item.GetProperty("uid").GetString()!;
            var track = item.GetProperty("track");
            var uri = SpotifyId.FromUri(track.GetProperty("uri").GetString()!);
            var name = track.GetProperty("name").GetString()!;
            var playcount = track.GetProperty("playcount").GetString();
            var album = track.GetProperty("albumOfTrack");
            var coverArt = album.GetProperty("coverArt").GetProperty("sources");
            var coverArtParsed = ParseImages(coverArt);

            var artists = track.GetProperty("artists").GetProperty("items");
            var artistsParsed = new SpotifySimpleArtist[artists.GetArrayLength()];
            int artists_idx = 0;
            using var artistsEnumerator = artists.EnumerateArray();
            while (artistsEnumerator.MoveNext())
            {
                var artist = artistsEnumerator.Current;
                var artistUri = SpotifyId.FromUri(artist.GetProperty("uri").GetString()!);
                var artistName = artist.GetProperty("profile").GetProperty("name").GetString()!;

                artistsParsed[artists_idx++] = new SpotifySimpleArtist
                {
                    Uri = artistUri,
                    Name = artistName,
                    Images = ImmutableArray<SpotifyImage>.Empty
                };
            }
            itemsParsed[items_idx++] = new SpotifyArtistTopTrack
            {
                Name = name,
                Playcount = ulong.Parse(playcount),
                Uid = uid,
                Id = uri,
                Images = coverArtParsed,
                Duration = TimeSpan.FromSeconds(track.GetProperty("duration").GetProperty("totalMilliseconds").GetDouble()),
                Artists = artistsParsed
            };
        }

        return ImmutableArray.Create(itemsParsed);
    }

    private static SpotifyArtistDiscographyGroup<T?> ParseDiscographyGroup<T>(JsonElement albums, Func<JsonElement, T> parser)
    {
        var items = albums.GetProperty("items");
        Span<T?> itemsParsed = new T?[items.GetArrayLength()];
        var items_idx = 0;
        using var itemsEnumerator = items.EnumerateArray();
        while (itemsEnumerator.MoveNext())
        {
            var item = itemsEnumerator.Current;
#if DEBUG
            try
            {
#endif
                if (item.TryGetProperty("releases", out var releasesArr))
                {
                    using var releases = releasesArr.GetProperty("items").EnumerateArray();
                    releases.MoveNext();
                    item = releases.Current;
                }

                itemsParsed[items_idx++] = parser(item);
#if DEBUG 
            }
            catch (KeyNotFoundException x)
            {
                if (item.TryGetProperty("data", out var dt) 
                    && dt.TryGetProperty("__typename", out var typeNameProp))
                {
                    if (typeNameProp.GetString() is "NotFound")
                    {
                        //itemsParsed[items_idx++] = default(T);
                        continue;
                    }
                }

                Debugger.Break();
            }
#endif
        }

        return new SpotifyArtistDiscographyGroup<T?>
        {
            Items = ImmutableArray.Create(itemsParsed),
            Total = albums.GetProperty("totalCount").GetUInt32()
        };
    }

    private static SpotifySimplePlaylist ParsePlaylist(JsonElement playlist)
    {
        var data = playlist.GetProperty("data");
        var uri = SpotifyId.FromUri(data.GetProperty("uri").GetString()!);
        var name = data.GetProperty("name").GetString()!;
        using var images = data.GetProperty("images").GetProperty("items").EnumerateArray();
        images.MoveNext();
        var image = images.Current.GetProperty("sources");
        var imageParsed = ParseImages(image);

        return new SpotifySimplePlaylist
        {
            Uri = uri
        };
    }

    private static SpotifySimpleArtist ParseArtist(JsonElement artist)
    {
        var uri = SpotifyId.FromUri(artist.GetProperty("uri").GetString()!);
        var name = artist.GetProperty("profile").GetProperty("name").GetString()!;
        var visuals = artist.GetProperty("visuals").GetProperty("avatarImage");
        IReadOnlyCollection<SpotifyImage> visualsParsed = ImmutableArray<SpotifyImage>.Empty;
        if (visuals.ValueKind is not JsonValueKind.Null)
        {
            visualsParsed = ParseImages(visuals.GetProperty("sources"));
        }

        return new SpotifySimpleArtist
        {
            Uri = uri,
            Name = name,
            Images = visualsParsed
        };
    }
    private static SpotifySimpleAlbum ParseAlbum(JsonElement release)
    {
        var uri = SpotifyId.FromUri(release.GetProperty("uri").GetString()!);
        var name = release.GetProperty("name").GetString()!;
        var releaseDate = release.GetProperty("date");
        int year = 1;
        if (releaseDate.ValueKind is not JsonValueKind.Null)
        {
            year = releaseDate.GetProperty("year").GetInt32();
        }
        else
        {

        }

        var coverArt = release.GetProperty("coverArt").GetProperty("sources");
        var coverArtParsed = ParseImages(coverArt);
        int? tracksCount = null;
        if (release.TryGetProperty("tracks", out var tracks))
        {
            tracksCount = tracks.GetProperty("totalCount").GetInt32();
        }
        var type = release.GetProperty("type").GetString()!;
        return new SpotifySimpleAlbum
        {
            Uri = uri,
            Name = name,
            Images = coverArtParsed,
            ReleaseDate = new DateOnly(year, 1, 1),
            Type = type,
            TotalTracks = tracksCount
        };
    }

    private static IReadOnlyCollection<SpotifyImage> ParseImages(JsonElement coverArt)
    {
        Span<SpotifyImage> images = new SpotifyImage[coverArt.GetArrayLength()];
        int i = 0;
        using var imagesEnumerator = coverArt.EnumerateArray();
        while (imagesEnumerator.MoveNext())
        {
            var image = imagesEnumerator.Current;
            var url = image.GetProperty("url").GetString()!;
            ushort? width = null;
            ushort? height = null;
            if (image.TryGetProperty("width", out var w))
            {
                if (w.ValueKind is not JsonValueKind.Null)
                {
                    width = w.GetUInt16();
                    height = image.GetProperty("height").GetUInt16();
                }
            }

            // var width = image.GetProperty("width").GetUInt16();
            // var height = image.GetProperty("height").GetUInt16();
            images[i++] = new SpotifyImage
            {
                Url = url,
                Width = width,
                Height = height
            };
        }

        return images.ToArray();
    }
}