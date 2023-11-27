using LanguageExt;
using LanguageExt.Common;

namespace Wavee.Vorbis.Infrastructure.Stream;

public interface IReadBytes
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <exception cref="IOException"></exception>

    Result<Unit> ReadBufferExactly(Span<byte> buffer);
    
    /// <summary>
    /// Reads four bytes from the stream and returns them in read-order or an error.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    Span<byte> ReadQuadBytes();
    
    
    /// <summary>
    /// Reads a single byte from the stream and returns it or an error.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    byte ReadByte();

    ulong Position();
}
