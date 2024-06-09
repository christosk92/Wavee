using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Wavee.UI.Spotify.Common;

internal static class SpotifyWsUtils
{
    public static ReadOnlyMemory<byte> ReadPayload(JsonElement messageRootElement, Dictionary<string, string> headers)
    {
        Memory<byte> payload = Memory<byte>.Empty;
        var gzip = false;
        var plainText = false;
        if (headers.TryGetValue("Transfer-Encoding", out var trnsfEncoding))
        {
            if (trnsfEncoding is "gzip")
            {
                gzip = true;
            }
        }

        if (headers.TryGetValue("Content-Type", out var cntEncoding))
        {
            if (cntEncoding is "text/plain")
            {
                plainText = true;
            }
        }

        if (messageRootElement.TryGetProperty("payloads", out var payloadsArr))
        {
            var payloads = new ReadOnlyMemory<byte>[payloadsArr.GetArrayLength()];
            for (var i = 0; i < payloads.Length; i++)
            {
                if (plainText)
                {
                    ReadOnlyMemory<byte> bytes = Encoding.UTF8.GetBytes(payloadsArr[i].GetString());
                    payloads[i] = bytes;
                }
                else
                {
                    payloads[i] = payloadsArr[i].GetBytesFromBase64();
                }
            }

            var totalLength = payloads.Sum(p => p.Length);
            payload = new byte[totalLength];
            var offset = 0;
            foreach (var payloadPart in payloads)
            {
                payloadPart.CopyTo(payload.Slice(offset));
                offset += payloadPart.Length;
            }
        }
        else if (messageRootElement.TryGetProperty("payload", out var payloadStr))
        {
            if (gzip is true)
            {
                payload = payloadStr.GetProperty("compressed").GetBytesFromBase64();
            }
            else
            {
                payload = payloadStr.GetBytesFromBase64();
            }
        }
        else
        {
            payload = Memory<byte>.Empty;
        }

        switch (gzip)
        {
            case false:
                //do nothing
                break;
            case true:
            {
                payload = UnsafeDecompressAltAsMemory(payload.Span);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }


        return payload;
    }
    
    public static unsafe Memory<byte> UnsafeDecompressAltAsMemory(ReadOnlySpan<byte> compressedData)
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