using Wavee.Spotify.Core.Models.Common;

namespace Wavee.Spotify.Core.Models.Exceptions;

public sealed class SpotifyItemNotSupportedException : Exception
{
    public SpotifyItemNotSupportedException(SpotifyId id) : base($"The item with id {id} is not supported.")
    {
        
    }
}