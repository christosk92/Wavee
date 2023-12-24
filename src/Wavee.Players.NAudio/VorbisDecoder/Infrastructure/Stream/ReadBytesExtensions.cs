using System.Buffers.Binary;
using LanguageExt;
using LanguageExt.Common;

namespace Wavee.Vorbis.Infrastructure.Stream;

internal static class ReadBytesExtensions
{
    public static int ReadVectored(this System.IO.Stream stream, ArraySegment<byte>[] buffers)
    {
        int totalBytesRead = 0;
        foreach (var buffer in buffers)
        {
            int bytesRead = stream.Read(buffer.Array, buffer.Offset, buffer.Count);
            if (bytesRead == 0)
            {
                break; // End of stream
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }

    /// <summary>
    /// Reads eight bytes from the stream and interprets them as an unsigned 64-bit little-endian
    /// </summary>
    /// <param name="reader"></param>
    /// <typeparam name="TB"></typeparam>
    /// <returns></returns>
    public static Result<ulong> ReadULong<TB>(this TB reader) where TB : IReadBytes
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        var result = reader.ReadBufferExactly(bytes);
        if (result.IsFaulted)
            return new Result<ulong>(result.Error());
        return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }

    public static Result<uint> ReadUInt<TB>(this TB reader) where TB : IReadBytes
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        var readResult = reader.ReadBufferExactly(bytes);
        if (readResult.IsFaulted)
            return new Result<uint>(readResult.Error());
        return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }
}