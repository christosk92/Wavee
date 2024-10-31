namespace Wavee.Core.Decoders.VorbisDecoder.Infrastructure;

public sealed class SeekError : Exception
{
    public SeekError(SeekErrorType error) : base(error.ToString())
    {
    }
}

public enum SeekErrorType
{
    /// <summary>
    /// The stream is not seekable at all.
    /// </summary>
    Unseekable,
    
    /// <summary>
    /// The stream can only be seeked forward.
    /// </summary>
    ForwardOnly,
    
    /// <summary>
    /// The timestamp to seek to is out of range.
    /// </summary>
    OutOfRange,
    
    /// <summary>
    /// The track ID provided is invalid.
    /// </summary>
    InvalidTrack
}