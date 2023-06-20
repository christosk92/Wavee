using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Spotify;
using NeoSmart.AsyncLock;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Spotify.Mercury.Live;

internal class LiveMercuryClient : IMercuryClient
{
    private static readonly AsyncLock AccessTokenLock = new();

    private readonly record struct AccessToken(string Token, DateTimeOffset ExpiresAt);

    private static readonly Dictionary<string, AccessToken> AccessTokens = new();

    private readonly object _sequencesLock = new();
    private readonly Dictionary<Guid, ulong> _sequences = new();
    private readonly SpotifyConnectionAccessor _connectionFactory;

    public LiveMercuryClient(SpotifyConnectionAccessor connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async ValueTask<string> GetToken(CancellationToken ct = default)
    {
        using (await AccessTokenLock.LockAsync(ct))
        {
            var connection = _connectionFactory.Access();
            var username = connection.WelcomeMessage.CanonicalUsername;
            
            if (AccessTokens.TryGetValue(username, out var token) && token.ExpiresAt > DateTimeOffset.UtcNow)
                return token.Token;

            const string KEYMASTER_URI =
                "hm://keymaster/token/authenticated?scope=user-read-private,user-read-email,playlist-modify-public,ugc-image-upload,playlist-read-private,playlist-read-collaborative,playlist-read&client_id=65b708073fc0480ea92a077233ca87bd&device_id=";

            var finalData = await GetAsync(KEYMASTER_URI, ct);

            var tokenData = JsonSerializer.Deserialize<MercuryTokenData>(finalData.Payload.Span);
            var newToken =
                new AccessToken(tokenData.AccessToken,
                    DateTimeOffset.UtcNow.AddSeconds(tokenData.ExpiresIn).Subtract(TimeSpan.FromMinutes(1)));
            AccessTokens[username] = newToken;
            return tokenData.AccessToken;
        }
    }

    public async Task<MercuryResponse> GetAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        bool done = false;

        while (!done)
        {
            try
            {
                var connection = _connectionFactory.Access();
                var seq = GetSequence(connection);

                var (listener, onDone) = connection.ListenForPackage((ref SpotifyUnencryptedPackage y) => Condition(ref y, seq));

                var partials = new List<ReadOnlyMemory<byte>>();
                var sw = Stopwatch.StartNew();
                var completionTask = Task.Run(async () =>
                {
                    await foreach (var package in listener.ReadAllAsync(cancellationToken))
                    {
                        var data = package.Payload;
                        var seqLen = SeqLenRef(ref data);
                        var foundSeq = SeqRef(ref data, seqLen);
                        var flags = Flag(ref data);
                        var count = Count(ref data);
                        for (int i = 0; i < count; i++)
                        {
                            var part = ParsePart(ref data);
                            partials.Add(part);
                        }

                        if (flags != 1)
                            continue;
                        var header = Header.Parser.ParseFrom(partials[0].Span);
                        var bodyLength = partials.Skip(1).Sum(x => x.Length);
                        Memory<byte> body = new byte[bodyLength];
                        var offset = 0;
                        foreach (var part in partials.Skip(1))
                        {
                            part.CopyTo(body.Slice(offset));
                            offset += part.Length;
                        }

                        sw.Stop();
                        Debug.WriteLine($"MercuryClient.Get {endpoint} took {sw.ElapsedMilliseconds}ms");
                        var response = new MercuryResponse(header, body);
                        onDone();
                        return response;
                    }

                    throw new TimeoutException();
                }, cancellationToken);
                SendInternal(connection, seq, endpoint, MercuryMethod.Get, null);

                done = true;
                return await completionTask;
            }
            catch (OperationCanceledException canceled)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    done = true;
                    Debug.WriteLine("MercuryClient.Get was canceled");
                    throw;
                }
            }
            catch (Exception x)
            {
                // ignored
                Debug.WriteLine(x);
                done = false;
                await Task.Delay(1000, cancellationToken);
            }
        }

        throw new OperationCanceledException();
    }

    private void SendInternal(SpotifyConnection connection, ulong seq, string uri, MercuryMethod method,
        string contentType)
    {
        var toSend = MercuryRequests.Build(
            seq,
            MercuryMethod.Get,
            uri,
            null,
            Array.Empty<ReadOnlyMemory<byte>>());

        connection.Send(toSend);
    }

    private ulong GetSequence(SpotifyConnection connection)
    {
        lock (_sequencesLock)
        {
            if (!_sequences.TryGetValue(connection.ConnectionId, out var seq))
            {
                seq = 0;
                _sequences.Add(connection.ConnectionId, seq);
            }

            _sequences[connection.ConnectionId] = seq + 1;
            return seq;
        }
    }

    private static bool Condition(ref SpotifyUnencryptedPackage packagetocheck, ulong seq)
    {
        if (packagetocheck.Type is SpotifyPacketType.MercuryEvent
            or SpotifyPacketType.MercuryReq
            or SpotifyPacketType.MercurySub
            or SpotifyPacketType.MercuryUnsub
            or SpotifyPacketType.Unknown0xb6)
        {
            var seqLength = BinaryPrimitives.ReadUInt16BigEndian(packagetocheck.Payload.Slice(0, 2));
            var calculatedSeq = BinaryPrimitives.ReadUInt64BigEndian(packagetocheck.Payload.Slice(2, seqLength));
            if (calculatedSeq != seq)
                return false;
            return true;
        }

        return false;
    }

    private static ReadOnlyMemory<byte> ParsePart(ref ReadOnlyMemory<byte> data)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(data.Span[..2]);
        data = data[2..];
        var body = data[..size];
        data = data[size..];
        return body;
    }

    private static ushort SeqLenRef(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        data = data[2..];
        return l;
    }

    private static ulong SeqRef(ref ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        data = data[len..];
        return l;
    }

    private static ushort SeqLen(ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        return l;
    }

    private static ulong Seq(ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        return l;
    }

    private static ushort Count(ref ReadOnlyMemory<byte> readOnlyMemory)
    {
        var c = BinaryPrimitives.ReadUInt16BigEndian(readOnlyMemory.Span[..2]);
        readOnlyMemory = readOnlyMemory[2..];
        return c;
    }

    private static byte Flag(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..1];
        var l = d[0];
        data = data[1..];
        return l;
    }

    internal readonly record struct MercuryTokenData(
        [property: JsonPropertyName("accessToken")]
        string AccessToken,
        [property: JsonPropertyName("expiresIn")]
        ulong ExpiresIn,
        [property: JsonPropertyName("tokenType")]
        string TokenType,
        [property: JsonPropertyName("scope")] string[] Scope,
        [property: JsonPropertyName("permissions")]
        ushort[] Permissions);
}