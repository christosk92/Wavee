using System.IO.Compression;

namespace Wavee.Spotify.Extensions;

internal static class Gzip
{
    
    public static ArraySegment<byte> CompressAlt(ReadOnlyMemory<byte> data)
    {
        using var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
        {
            gzipStream.Write(data.Span);
        }

        if (compressedStream.TryGetBuffer(out var buffer))
        {
            return buffer;
        }
        else
        {
            return compressedStream.ToArray();
        }
    }

    internal static unsafe Span<byte> UnsafeDecompressAlt(ReadOnlySpan<byte> compressedData)
    {
        fixed (byte* pBuffer = &compressedData[0])
        {
            using var uncompressedStream = new MemoryStream();
            using (var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedData.Length))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                gzipStream.CopyTo(uncompressedStream);
            }

            if (uncompressedStream.TryGetBuffer(out var buffer))
            {
                return buffer.AsSpan();
            }
            else
            {
                return uncompressedStream.ToArray();
            }
        }
    }

    internal static unsafe Memory<byte> UnsafeDecompressAltAsMemory(ReadOnlySpan<byte> compressedData)
    {
        fixed (byte* pBuffer = &compressedData[0])
        {
            using var uncompressedStream = new MemoryStream();
            using (var compressedStream = new UnmanagedMemoryStream(pBuffer, compressedData.Length))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
            {
                gzipStream.CopyTo(uncompressedStream);
            }

            if (uncompressedStream.TryGetBuffer(out var buffer))
            {
                return buffer;
            }
            else
            {
                return uncompressedStream.ToArray();
            }
        }
    }
}