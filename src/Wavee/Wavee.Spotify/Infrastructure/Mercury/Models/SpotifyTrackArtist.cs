using Spotify.Metadata;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Infrastructure.Mercury.Models;

public readonly record struct SpotifyTrackArtist
    (AudioId Id, string Name, ArtistWithRole.Types.ArtistRole Role) 
{
    public static SpotifyTrackArtist From(ArtistWithRole artist)
    {
        return new SpotifyTrackArtist(
            Id: AudioId.FromRaw(artist.ArtistGid.Span, AudioItemType.Artist, ServiceType.Spotify),
            Name: artist.ArtistName,
            Role: artist.Role
        );
    }
}