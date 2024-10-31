using System.Diagnostics;
using System.Net;
using Wavee.Enums;
using Wavee.Models.Common;
using Wavee.Models.Metadata;
using Wavee.ViewModels.Models;
using Wavee.ViewModels.Models.Items;

namespace Wavee.ViewModels.Extensions;

internal static class SpotifyItemExtensions
{
    public static WaveeItem ToWaveeItem(this SpotifyItem sp)
    {
        var sw = Stopwatch.StartNew();
        var mapping = Mappings[sp.Id.ItemType];
        var item = mapping(sp);
        sw.Stop();
        return item;
    }

    public static WaveeItem HandleUnknownItem(SpotifyId id)
    {
        return id.ItemType switch
        {
            SpotifyItemType.Local => ToWaveeLocalItem(new Local(id)),
            _ => new WaveeUnknownItem(id)
        };
    }

    private static readonly Dictionary<SpotifyItemType, Func<SpotifyItem, WaveeItem>> Mappings = new()
    {
        { SpotifyItemType.Album, ToWaveeItemUnknown },
        { SpotifyItemType.Artist, ToWaveeItemUnknown },
        { SpotifyItemType.Episode, ToWaveeItemUnknown },
        { SpotifyItemType.Playlist, ToWaveeItemUnknown },
        { SpotifyItemType.Show, ToWaveeItemUnknown },
        { SpotifyItemType.Track, ToWaveeSongItem },
        { SpotifyItemType.Local, ToWaveeLocalItem },
        { SpotifyItemType.Unknown, ToWaveeItemUnknown },
        { SpotifyItemType.FolderStart, ToWaveeItemUnknown },
        { SpotifyItemType.FolderEnd, ToWaveeItemUnknown },
        { SpotifyItemType.FolderUnknown, ToWaveeItemUnknown },
        { SpotifyItemType.MetaPage, ToWaveeItemUnknown }
    };

    private static WaveeSongItem ToWaveeSongItem(SpotifyItem sp)
    {
        var spotifyTrack = (SpotifyTrack)sp;
        var album = new WaveeSongAlbum(spotifyTrack.Album.Id, spotifyTrack.Album.Name);
        var artists = spotifyTrack.Artists.Select(a => new WaveeSongArtist(a.Id, a.Name));
        var images = spotifyTrack.Album.Images;
        return new WaveeSongItem(
            Id: sp.Id,
            Title: sp.Name,
            Duration: spotifyTrack.Duration,
            Album: album,
            Artists: artists,
            Images: images,
            CanPlay: spotifyTrack.CanPlay);
    }

    private static WaveeItem ToWaveeLocalItem(SpotifyItem sp)
    {
        //spotify:local:Lee+Seung+Gi:The+Dream+Of+A+Moth:%EB%82%B4+%EC%97%AC%EC%9E%90%EB%9D%BC%EB%8B%88%EA%B9%8C+%28+Because+You%27re+My+Girl%29:244
        // Local: `spotify:local:{artist}:{album_title}:{track_title}:{duration_in_seconds}`
        var parts = sp.Id.ToString().Split(':');
        var artist = WebUtility.UrlDecode(parts[2]);
        var album = WebUtility.UrlDecode(parts[3]);
        var title = WebUtility.UrlDecode(parts[4]);
        var duration = int.Parse(parts[5]);
        var pseudoAlbumId = SpotifyId.FromUri("spotify:local:album:" + WebUtility.UrlEncode(album));
        var trackAlbum = new WaveeSongAlbum(pseudoAlbumId, album);

        var pseudoArtistId = SpotifyId.FromUri("spotify:local:artist:" + WebUtility.UrlEncode(artist));
        var trackArtist = new WaveeSongArtist(pseudoArtistId, artist);

        var songitem = new WaveeSongItem(sp.Id, title, TimeSpan.FromSeconds(duration), trackAlbum, [trackArtist], [], false);
        return songitem;
    }

    private static WaveeItem ToWaveeItemUnknown(SpotifyItem sp)
    {
        var id = sp.Id.ToString();
        if (id.StartsWith("spotify:local:"))
        {
            return ToWaveeLocalItem(sp);
        }

        return new WaveeUnknownItem(sp.Id);
    }

    private class Local : SpotifyItem
    {
        public Local(SpotifyId id)
        {
            Id = id;
        }
    }
}