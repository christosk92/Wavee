namespace Wavee.Vorbis.Infrastructure.Stream;

public interface ISeekBuffered
{
    /// <summary>
    /// Seek within the buffered data to an absolute position in the stream. Returns the position
    /// seeked to.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    ulong SeekBuffered(ulong position);
}