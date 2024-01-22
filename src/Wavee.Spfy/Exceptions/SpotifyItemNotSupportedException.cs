namespace Wavee.Spfy.Exceptions;

public sealed class SpotifyItemNotSupportedException : Exception
{
    public SpotifyItemNotSupportedException(SpotifyId id) : base($"The item with id {id} is not supported.")
    {
        
    }
}