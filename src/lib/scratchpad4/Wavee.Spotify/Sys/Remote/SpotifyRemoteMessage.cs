using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CommunityToolkit.HighPerformance;

namespace Wavee.Spotify.Sys.Remote;

internal readonly record struct SpotifyRemoteMessage(SpotifyRemoteMessageType Type, string Uri,
    ReadOnlyMemory<byte> Payload)
{
    public static SpotifyRemoteMessage ParseFrom(ReadOnlyMemory<byte> data)
    {
        using var jsonDocument = JsonDocument.Parse(data);
        /*
             * {
    "headers": {
        "Transfer-Encoding": "gzip"
    },
    "payloads": [
        "H4sIAAAAAAAA/1XUezhUaRwHcMelubhNo5JZasoWJZwZcyUeOzLGbYyQkppm5pxhMmammUGK6Enl+tjYfYjSkFCirWhDN8SiTbUl1LNFdiuVss+2m3SxvKf9o7++7+9z3uc87/m973vwENkZQbgymMtBmFIGHZXR5HQ6B4E96HJURmez5DIuU4LQUbaMQzGju8FuMDXvnuE8zpno2W1CXEBzo7vRYDc2C3aLY9K5bJjLpIugjdC2n/JNtluv848IjgwTuQaEcyI5UfDO6rc3z83AuZAp3pgEzYUJyWguzLBqHhY4LIhYmGNhgc00xSo8yXcuLLHKai5S8AQKXpKEKNTuEoRiiY1QjUKnRlCKzVeli14rkSVQyBgqVHpUq03S6BVqFcUcM6VaJlH+X2Cz8ckKBMXejY2+vKwPIhHNEVSaFCdWosmokgzR+iBrIlGvQLU0sUat1ZMhuA9yItogaLJChoolCKJFdTpxokSXQCbRuLMNZHFmG0tjMtzpjD7IlUiQKRWoSi9WIGQqiyllwxyY7SGXwQwOjEq4dAnMZtM9PGQSDluK9EEORLJUK1EhYkSh0yglqWKVJBEl43QatV4hT+2DqERy4uw6lV8/J4r8qAiqS9CrNdIqAtnpy3TPuRlKhU7v6cFWcJHwGD7NnyP8TsIP5akiU6Lki02pZs4QZ6TdcH4G5lkGqKm82eZQ10tmGxVvpDGqgpwRFEYZKJvhIaMxUDlMp3FZCBdFEQbKYcIMJpNJk0qQ2WNVB3UVXSjGXYOgHsjoNmT6ACITScuL+7s/uAdS2lh7M2puORYB1Fsssk47xI86Hiv0wt8QBQJsjfdPKxAKguvtXRYYTt6lAMxdmfkyNT2ktaonY1BVskEM0KvulO91u5Dy0vpJx8/HzFwBZseW/ZYyI7xS+ezlmuMxOh3AZsYgoZwRkHCGG+vUeLBaDXB1pX6Pt8h/oMkV9XFo2fwPQO9Fgq3qMEHv2eDHPXY/RHcCJKy1/HHFZT/DqbbiM2VFOz9hX9RzsfGMjVBfn5r6utpm7zOAM5998ofDAseK28dLf8miPwBIGzhnWHmcF5kzSZwcfKTDAbRl7V5YQvZPa5kYEZ9mrbABODD1sdxcF4j/Huc4fTLLbitAM1Xs4SxNUFQeP/ZaaHDPMEC5cMBQ8Ur4bV5EeK3XVE0iwDyGsiFHJCg4Et52WeA0mgawktIywF8WNNwq23ZgMJ4QBfDQH2wedUQYlqvOUY097G4D2N3fj3PvCn3WWhgxtvLNoz8BliWte7VcGcAv31bX1JxexQfYe2/x2bSH/tr6G1cLX3wQPQVICUqwCFgf0Fjl5dioXNLLwJq8Knyp/QtBQ1PEk4mUpx8HAaa2ST8nePN3Hp5X47/A6u1lgNzapRkpM6Eag2XI3a5dYg+AJwrNApZ1BD4ujfsUw68KxdYJ7R9/EkcOsqxzdakY3WrfDvDdFr/7nfLQ2sy6Q10uvFIywF991j5fc5r/5hJh5AM646nE+ilNpxxzEA4f7SJ716dPlGAb91d1exjT16ISOXF1PrX9GMChE0vuE4JCahu2T7W/3nPRgO1m9NC4/UL+/pb67Mqj2oYtADsbceUdNOHIBe3G4EKVcT7AIiveUJH9uvvZ9EL7b9ibTADaxGxy6YkWphxxGFHldPitBXhnl+FFwe++MWVofI0Bna4GmLDa2s6K5Wdb8S7IvvLfQTbAjx56xYG9gSMH9s1LuBaSehvgwd2N0WMZQYIs22UmU5fi3wO0yyRNCkYFJfkVi/HCyso9AIXy6zG66FBq+XRzWbntNLZxzp3zHzyK4mXWjRdMrekhTGDns7Q7/egm34DGHe+rfv7bFbuGPkNNcO6VMNua8+pm7lAntnhHY1GF8XxeYI0zReRC2oFdGYdRfcidXl7yvrKOVSl9F7G7ecvZospxeWD/2ZaHGzbnlVo+h57jsqGPN+f+Of8B4+h4DLEGAAA="
    ],
    "type": "message",
    "uri": "hm://remote/user/7ucghdgquf6byqusqkliltwc2/"
}
             */

        var transferEncoding = TransferEncoding(jsonDocument).IfNone(string.Empty);
        var type = jsonDocument.RootElement.GetProperty("type").GetString();
        return type switch
        {
            "message" => FromMessage(jsonDocument.RootElement, transferEncoding),
            "request" => FromRequest(jsonDocument.RootElement, transferEncoding),
            _ => throw new InvalidOperationException()
        };
    }


    private static SpotifyRemoteMessage FromMessage(JsonElement jsonDocumentRootElement, string transferEncoding)
    {
        var uri = jsonDocumentRootElement.GetProperty("uri").GetString();
        using var payloads = jsonDocumentRootElement.GetProperty("payloads").EnumerateArray();
        var payload = payloads.First();
        var decoded = Decompress(payload.GetBytesFromBase64().AsMemory().AsStream());

        return new SpotifyRemoteMessage(
            Type: SpotifyRemoteMessageType.Message,
            Uri: uri,
            Payload: decoded
        );
    }

    private static SpotifyRemoteMessage FromRequest(JsonElement jsonDocumentRootElement, string transferEncoding)
    {
        var key = jsonDocumentRootElement.GetProperty("key").GetString();
        var decoded = Decompress(jsonDocumentRootElement.GetProperty("payload").GetProperty("compressed")
            .GetBytesFromBase64().AsMemory().AsStream());
        return new SpotifyRemoteMessage(SpotifyRemoteMessageType.Request,
            key,
            decoded
        );
    }

    private static Option<string> TransferEncoding(JsonDocument jsonDocument)
    {
        return jsonDocument.RootElement.TryGetProperty("headers", out var headers)
               && headers.TryGetProperty("Transfer-Encoding", out var contentEncoding)
            ? contentEncoding.GetString()
            : None;
    }

    private static ReadOnlyMemory<byte> Decompress(Stream compressedStream, bool leaveStreamOpen = false)
    {
        if (compressedStream.Position == compressedStream.Length)
        {
            compressedStream.Seek(0, SeekOrigin.Begin);
        }

        using var uncompressedStream = new MemoryStream(GetGzipUncompressedLength(compressedStream));
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveStreamOpen))
        {
            gzipStream.CopyTo(uncompressedStream);
        }

        uncompressedStream.Seek(0, SeekOrigin.Begin);
        return uncompressedStream.ToArray();
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

internal enum SpotifyRemoteMessageType
{
    Message,
    Request
}