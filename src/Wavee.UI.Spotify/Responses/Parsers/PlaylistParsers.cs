using System.Text.Json;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.Spotify.Responses.Parsers;

public static class PlaylistParsers
{
    public static ISimplePlaylist ParsePlaylist(this JsonElement element)
    {
        var id = element.GetProperty("uri").GetString();
        var name = element.GetProperty("name").GetString();
        var description = element.GetProperty("description").GetString();

        using var imagesRoot = element.GetProperty("images").GetProperty("items").EnumerateArray();
        UrlImage[] images;
        string? color = default;
        if (imagesRoot.MoveNext())
        {
            var imagesItem = imagesRoot.Current;
            images = imagesItem.GetProperty("sources").ParseImages();
        }
        else
        {
            images = [];
        }
        return new SpotifySimplePlaylist(id,
            name,
            description,
            images);

    }
}