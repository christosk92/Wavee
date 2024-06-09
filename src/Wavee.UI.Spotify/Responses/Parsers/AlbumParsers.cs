using System.Linq;
using System.Text.Json;
using Spotify.Metadata;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.Spotify.Common;

namespace Wavee.UI.Spotify.Responses.Parsers;

public static class AlbumParsers
{
    public static ISimpleAlbum ToSimpleAlbum(this Album album)
    {
        var id = RegularSpotifyId.FromRaw(album.Gid.Span, SpotifyIdItemType.Album);
        var name = album.Name;
        var artist = album.Artist.FirstOrDefault();
        var images = album.CoverGroup.Image
            .Select(x=> x.ToUrlImage())
            .ToArray();
        
        return new SimpleAlbum(id.AsString, name, artist.ToContributor(), images);
    }
    public static ISimpleAlbum ParseAlbum(this JsonElement element)
    {
        var id = element.GetProperty("uri").GetString();
        var name = element.GetProperty("name").GetString();

        var coverArt = element.GetProperty("coverArt");
        var images = coverArt.GetProperty("sources").ParseImages();
        using var artists = element.GetProperty("artists").GetProperty("items").EnumerateArray();
        artists.MoveNext();
        var artist = artists.Current.ParseContributor();
        var album = new SimpleAlbum(id,
            name,
            artist,
            images);
        return album;
    }
}