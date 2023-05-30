using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Models.Response;

internal readonly record struct SpotifyTrackArtist
    (AudioId Id, string Name, ArtistWithRole.Types.ArtistRole Role) : ITrackArtist
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