using System.IO.Compression;

namespace Wavee.Spotify.Utils;

internal static class Gzip
{
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
}