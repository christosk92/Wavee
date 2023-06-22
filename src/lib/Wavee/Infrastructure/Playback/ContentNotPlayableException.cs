namespace Wavee.Infrastructure.Playback;

public sealed class ContentNotPlayableException : Exception
{
    public ContentNotPlayableException(string reason) : base(reason)
    {
        
    }
}