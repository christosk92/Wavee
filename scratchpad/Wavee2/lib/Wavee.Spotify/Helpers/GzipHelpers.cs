using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using LanguageExt;

namespace Wavee.Spotify.Helpers;

public static class GzipHelpers
{
    public static StreamContent GzipCompress(ReadOnlyMemory<byte> data)
    {
        using var inputStream = data.AsStream();
        if (inputStream.Position == inputStream.Length)
        {
            inputStream.Seek(0, SeekOrigin.Begin);
        }

        var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.SmallestSize, true))
        {
            inputStream.CopyTo(gzipStream);
        }

        inputStream.Close();

        compressedStream.Seek(0, SeekOrigin.Begin);
        var strContent = new StreamContent(compressedStream);
        strContent.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
        strContent.Headers.ContentEncoding.Add("gzip");
        strContent.Headers.ContentLength = compressedStream.Length;
        return strContent;
    }

    public static MemoryStream GzipDecompress(Stream compressedStream)
    {
        if (compressedStream.Position == compressedStream.Length)
        {
            compressedStream.Seek(0, SeekOrigin.Begin);
        }

        var uncompressedStream = new MemoryStream(GetGzipUncompressedLength(compressedStream));
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false);
        gzipStream.CopyTo(uncompressedStream);

        uncompressedStream.Seek(0, SeekOrigin.Begin);
        return uncompressedStream;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetGzipUncompressedLength(Stream stream)
    {
        Span<byte> uncompressedLength = stackalloc byte[4];
        stream.Position = stream.Length - 4;
        stream.Read(uncompressedLength);
        stream.Seek(0, SeekOrigin.Begin);
        return BitConverter.ToInt32(uncompressedLength);
    }
}