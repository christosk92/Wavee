using Spotify.Metadata;
using Wavee.Core.Contracts;
using Wavee.Core.Id;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Infrastructure;

namespace Wavee.Spotify.Models.Responses;

internal readonly record struct SpotifyTrackArtist(AudioId Id, string Name, ArtistWithRole.Types.ArtistRole Role) : ITrackArtist
{
    public static SpotifyTrackArtist From(ArtistWithRole artist)
    {
        return new SpotifyTrackArtist(
            Id: artist.ToId(),
            Name: artist.ArtistName, 
            Role: artist.Role
        );
    }
}