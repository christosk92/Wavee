using Wavee.Domain.Exceptions;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Exceptions;

public sealed class SpotifyTrackNotFoundException : TrackNotFoundException
{
    internal SpotifyTrackNotFoundException(SpotifyId id, Exception innerException) : base(
        message: $"Could not find track with uri: {id.ToString()}", innerException)
    {

    }
}