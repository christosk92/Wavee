using Wavee.Spotify.Clients.Mercury;

namespace Wavee.Spotify;

public interface ISpotifyClient
{
    IMercuryClient Mercury { get; }
}