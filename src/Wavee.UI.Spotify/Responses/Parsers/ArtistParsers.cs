using System.Linq;
using System.Text.Json;
using Spotify.Metadata;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.Spotify.Common;

namespace Wavee.UI.Spotify.Responses.Parsers;

public static class ArtistParsers
{
    public static IContributor ToContributor(this Artist artist)
    {
        var id = RegularSpotifyId.FromRaw(artist.Gid.Span, SpotifyIdItemType.Artist);
        var name = artist.Name;
        // var images = artist.PortraitGroup.Image
        //     .Select(x => x.ToUrlImage())
        //     .ToArray();
        return new SpotifyArtistContributor(id.AsString, name);
    }
    public static ISimpleArtist ParseArtist(this JsonElement element)
    {
        var id = element.GetProperty("uri").GetString();
        var name = element.GetProperty("profile").GetProperty("name").GetString();
        var visuals = element.GetProperty("visuals").GetProperty("avatarImage");

        var images = visuals.GetProperty("sources").ParseImages();
        return new SpotifySimpleArtist(id,
            name,
            images);
    }

    public static IContributor ParseContributor(this JsonElement element)
    {
        var id = element.GetProperty("uri").GetString();
        var name = element.GetProperty("profile").GetProperty("name").GetString();
        return new SpotifyArtistContributor(id,
            name);
    }

}