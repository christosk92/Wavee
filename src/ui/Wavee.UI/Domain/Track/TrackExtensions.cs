using System.Collections.Immutable;
using Spotify.Metadata;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Domain.Podcast;

namespace Wavee.UI.Domain.Track;

public static class TrackExtensions
{
    public static SimpleAlbumEntity ToSimpleAlbum(this SpotifySimpleAlbum x)
    {
        return new SimpleAlbumEntity
        {
            Id = x.Uri.ToString(),
            Name = x.Name,
            Images = x.Images,
            Year = (ushort?)x.ReleaseDate.Year,
            Type = x.Type
        };
    }
    public static SimpleArtistEntity ToSimpleArtist(this SpotifySimpleArtist x)
    {
        return new SimpleArtistEntity
        {
            Id = x.Uri.ToString(),
            Name = x.Name,
            BiggestImageUrl = x.Images.OrderByDescending(x => x.Width ?? 0)
                .FirstOrDefault()
                .Url,
            SmallestImageUrl = x.Images.OrderBy(x => x.Width ?? 0)
                .FirstOrDefault()
                .Url,
        };
    }

    public static WaveeTrackOrEpisodeOrArtist MapToSimpleEntity(
        this SpotifyTrackOrEpisode item)
    {
        return new WaveeTrackOrEpisodeOrArtist(
            Track: item.Track?.ToSimpleTrack(),
            Episode: item.Episode?.ToSimplePodcastEpisode(),
            Artist: null,
            Id: item.Id.ToString()
        );
    }

    public static SimplePodcastEpisode ToSimplePodcastEpisode(this Episode x)
    {
        return new SimplePodcastEpisode();
    }

    public static SimpleArtistEntity ToSimpleArtist(this global::Spotify.Metadata.Artist x)
    {
        const string url = "https://i.scdn.co/image/";
        var images = x.PortraitGroup.Image.Select(c =>
        {
            var id = SpotifyId.FromRaw(c.FileId.Span, SpotifyItemType.Unknown);
            var hex = id.ToBase16();

            return ($"{url}{hex}", c.Width);
        }).ToList();

        return new SimpleArtistEntity
        {
            Id = SpotifyId.FromRaw(x.Gid.Span, SpotifyItemType.Artist).ToString(),
            Name = x.Name,
            BiggestImageUrl = images.OrderByDescending(x => x.Width)
                .FirstOrDefault()
                .Item1,
            SmallestImageUrl = images.OrderBy(x => x.Width)
                .FirstOrDefault()
                .Item1,
        };
    }
    public static SimpleTrackEntity ToSimpleTrack(this global::Spotify.Metadata.Track track)
    {
        const string url = "https://i.scdn.co/image/";
        var images = track.Album.CoverGroup.Image.Select(c =>
        {
            var id = SpotifyId.FromRaw(c.FileId.Span, SpotifyItemType.Unknown);
            var hex = id.ToBase16();

            return ($"{url}{hex}", c.Width);
        }).ToList();
        return new SimpleTrackEntity
        {
            Name = track.Name,
            SmallestImageUrl = images.OrderBy(x => x.Width)
                .FirstOrDefault()
                .Item1,
            Artists = track.Artist.Select(x => (SpotifyId.FromRaw(x.Gid.Span,
                        SpotifyItemType.Artist)
                    .ToString(), x.Name))
                .ToImmutableArray(),
            Album = (SpotifyId.FromRaw(track.Album.Gid.Span,
                        SpotifyItemType.Album)
                    .ToString(),
                track.Album.Name),
            Duration = TimeSpan.FromMilliseconds(track.Duration),
            BiggestImageUrl = images.OrderByDescending(x => x.Width)
                .FirstOrDefault()
                .Item1,
            Id = SpotifyId.FromRaw(track.Gid.Span, SpotifyItemType.Track).ToString()
        };
    }
}