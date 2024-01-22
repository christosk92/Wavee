namespace Wavee.Vorbis.Infrastructure.Stream;

/// <summary>
/// 
/// </summary>
/// <param name="BufferLength">
/// The maximum buffer size. Must be a power of 2. Must be > 32kB.
/// </param>
public readonly record struct MediaSourceStreamOptions(int BufferLength)
{
    public static MediaSourceStreamOptions Default { get; } = new(64 * 1024);
}