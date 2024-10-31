using System.Buffers;
using System.IO.Compression;
using System.Text.Json;
using CommunityToolkit.HighPerformance;

namespace Wavee.Helpers;

internal sealed class GzipDecompression
{
    // Efficient method to decode and decompress base64 gzip payload
    public static byte[] DecodeAndDecompressBase64Gzip(JsonElement base64Array)
    {
        using var ms = new MemoryStream();
        Read(base64Array, ms);
        ms.Position = 0;
        return DecompressGzip(ms);
    }

    private static void Read(JsonElement base64Array, Stream writeTo)
    {
        
        if (base64Array.ValueKind is JsonValueKind.Array)
        {
            using var enumerator = base64Array.EnumerateArray();
            while (enumerator.MoveNext())
            {
                try
                {
                    var token = enumerator.Current;
                    ReadOnlySpan<byte> r = token.GetBytesFromBase64();
                    writeTo.Write(r);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        else
        {
            ReadOnlySpan<byte> r = base64Array.GetBytesFromBase64();
            writeTo.Write(r);
        }
        // int estimatedSize = 0;
        // foreach (JsonElement element in base64Array.EnumerateArray())
        // {
        //     string base64String = element.GetString();
        //     estimatedSize += (base64String.Length * 3) / 4; // Approximate the size of the decoded byte array
        // }
        // return estimatedSize;
    }
    //
    // // Estimate the size of Base64 encoded data
    // private static Span<byte> ReadBytes(JsonElement base64Array)
    // {
    //     int estimatedSize = 0;
    //
    //     using var enumerator = base64Array.EnumerateArray();
    //     while (enumerator.MoveNext())
    //     {
    //         var token = enumerator.Current
    //         //estimatedSize += (token.ToString().Length * 3) / 4; // Base64 to byte size approximation
    //     }
    //
    //     return estimatedSize;
    // }

    // Decompress the gzip stream using Span<byte>
    internal static byte[] DecompressGzip(Stream compressedStream)
    {
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(81920); // 80KB buffer
        int bytesRead;
        while ((bytesRead = gzipStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, bytesRead);
        }

        ArrayPool<byte>.Shared.Return(buffer);
        return output.ToArray();
    }

    public static ReadOnlySpan<byte> DecompressGzip(ReadOnlyMemory<byte> c)
    {
        // Use a MemoryStream to read the compressed data
        using var compressedStream = c.AsStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        using (var output = new MemoryStream())
        {
            // Allocate a buffer for decompression
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                int bytesRead;
                while ((bytesRead = gzipStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Write the decompressed data to the output MemoryStream
                    output.Write(buffer, 0, bytesRead);
                }

                // Convert the decompressed data to a string
                //   return Encoding.UTF8.GetString(output.GetBuffer(), 0, (int)output.Length);
                return output.GetBuffer().AsSpan(0, (int)output.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}