using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Eum.Spotify;
using Eum.Spotify.context;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NeoSmart.AsyncLock;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Infrastructure.Mercury;

internal readonly struct MercuryClient : ISpotifyMercuryClient
{
    private readonly record struct AccessToken(string Token, DateTimeOffset ExpiresAt);

    private record AccessTokenLockComposite(AccessToken Token, AsyncLock Lock);

    private static readonly Dictionary<string, ulong> SendSequences = new Dictionary<string, ulong>();
    private static readonly Dictionary<string, AccessTokenLockComposite> AccessTokenLocks = new();

    private readonly string _username;
    private readonly string _countryCode;
    private readonly SendPackage _onPackageSend;
    private readonly Func<PackageReceiveCondition, Channel<BoxedSpotifyPacket>> _onPackageReceive;

    public MercuryClient(string username,
        string countryCode,
        SendPackage onPackageSend,
        Func<PackageReceiveCondition, Channel<BoxedSpotifyPacket>> onPackageReceive)
    {
        _username = username;
        _countryCode = countryCode;
        _onPackageSend = onPackageSend;
        _onPackageReceive = onPackageReceive;
        if (!AccessTokenLocks.ContainsKey(username))
            AccessTokenLocks.Add(username,
                new AccessTokenLockComposite(new AccessToken(string.Empty, DateTimeOffset.MinValue), new AsyncLock()));

        lock (SendSequences)
        {
            SendSequences.TryAdd(username, 0);
        }
    }

    public async Task<string> GetAccessToken(CancellationToken ct = default)
    {
        var (token, lockObj) = AccessTokenLocks[_username];
        using (await lockObj.LockAsync(ct))
        {
            if (token.ExpiresAt > DateTimeOffset.UtcNow)
                return token.Token;

            const string KEYMASTER_URI =
                "hm://keymaster/token/authenticated?scope=user-read-private,user-read-email,playlist-modify-public,ugc-image-upload,playlist-read-private,playlist-read-collaborative,playlist-read&client_id=65b708073fc0480ea92a077233ca87bd&device_id=";
            var tcs = new TaskCompletionSource<MercuryPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
            CreateListener(KEYMASTER_URI, MercuryMethod.Get, null, tcs, ct);
            var finalData = await tcs.Task;
            var tokenData = JsonSerializer.Deserialize<MercuryTokenData>(finalData.Payload.Span);
            var newToken =
                new AccessToken(tokenData.AccessToken,
                    DateTimeOffset.UtcNow.AddSeconds(tokenData.ExpiresIn).Subtract(TimeSpan.FromMinutes(1)));
            AccessTokenLocks[_username] = new AccessTokenLockComposite(newToken, lockObj);
            return tokenData.AccessToken;
        }
    }

    public async Task<SpotifyContext> ContextResolve(string contextUri,
        CancellationToken ct = default)
    {
        const string query = "hm://context-resolve/v1/{0}";
        var url = string.Format(query, contextUri);
        var tcs = new TaskCompletionSource<MercuryPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreateListener(url, MercuryMethod.Get, null, tcs, ct);
        var finalData = await tcs.Task;

        using var jsonDocument = JsonDocument.Parse(finalData.Payload);
        var parsed = Parse(jsonDocument);
        return parsed;
    }

    public async Task<SpotifyContext> ContextResolveRaw(string pageUrl, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<MercuryPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreateListener(pageUrl, MercuryMethod.Get, null, tcs, ct);
        var finalData = await tcs.Task;

        using var jsonDocument = JsonDocument.Parse(finalData.Payload);
        var parsed = Parse(jsonDocument);
        return parsed;
    }

    public async Task<Track> GetTrack(AudioId id, string country, CancellationToken ct = default)
    {
        const string query = "hm://metadata/4/track/{0}?country={1}";
        var finalUri = string.Format(query, id.ToBase16(), country);

        var tcs = new TaskCompletionSource<MercuryPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreateListener(finalUri, MercuryMethod.Get, null, tcs, ct);
        var finalData = await tcs.Task;
        if (finalData.Header.StatusCode != 200)
            throw new Exception("Failed to get track. Failed with status code: " + finalData.Header.StatusCode);
        return Track.Parser.ParseFrom(finalData.Payload.Span);
    }

    public Task<Episode> GetEpisode(AudioId id, string country, CancellationToken ct = default)
    {
        const string query = "hm://metadata/4/episode/{0}?country={1}";
        var finalUri = string.Format(query, id.ToBase16(), country);
        var tcs = new TaskCompletionSource<MercuryPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreateListener(finalUri, MercuryMethod.Get, null, tcs, ct);
        return tcs.Task.ContinueWith(x =>
        {
            var finalData = x.Result;
            if (finalData.Header.StatusCode != 200)
                throw new Exception("Failed to get track. Failed with status code: " + finalData.Header.StatusCode);
            return Episode.Parser.ParseFrom(finalData.Payload.Span);
        }, ct);
    }

    public Task<string> Autoplay(string id, CancellationToken ct = default)
    {
        const string query = "hm://autoplay-enabled/query?uri={0}";
        var finalUri = string.Format(query, id);
        var tcs = new TaskCompletionSource<MercuryPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreateListener(finalUri, MercuryMethod.Get, null, tcs, ct);
        return tcs.Task.ContinueWith(x =>
        {
            var finalData = x.Result;
            if (finalData.Header.StatusCode != 200)
                throw new Exception("Failed to get track. Failed with status code: " + finalData.Header.StatusCode);
            return Encoding.UTF8.GetString(finalData.Payload.Span);
        }, ct);
    }

    public Task<TrackOrEpisode> GetMetadata(AudioId id, string country, CancellationToken ct = default)
    {
        return id.Type switch
        {
            AudioItemType.Track => GetTrack(id, country, ct).Map(x => new TrackOrEpisode(new Lazy<Track>(x))),
            AudioItemType.PodcastEpisode => GetEpisode(id, country, ct).Map(x => new TrackOrEpisode(x)),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
    }

    public Task<MercuryPacket> Get(string url, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<MercuryPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        CreateListener(url, MercuryMethod.Get, null, tcs, ct);
        return tcs.Task;
    }


    private static SpotifyContext Parse(JsonDocument jsonDocument)
    {
        var metadata = jsonDocument.RootElement.TryGetProperty("metadata", out var metadataElement)
            ? metadataElement.EnumerateObject().Fold(new HashMap<string, string>(),
                (acc, x) => acc.Add(x.Name, x.Value.GetString()))
            : Empty;


        var pages = jsonDocument.RootElement.TryGetProperty("pages", out var pagesElement)
            ? pagesElement.Clone().EnumerateArray().Select(ContextHelper.ParsePage).ToSeq()
            : Empty;
        var url = jsonDocument.RootElement.TryGetProperty("url", out var urlElement)
            ? urlElement.GetString()
            : null;

        var tracks = jsonDocument.RootElement.TryGetProperty("tracks", out var tracksElement)
            ? tracksElement.Clone().EnumerateArray().Select(ContextHelper.ParseTrack).ToSeq()
            : Empty;
        //if(pages is empty, add tracks to pages)
        if (pages.IsEmpty && !tracks.IsEmpty)
        {
            pages = Seq1(new ContextPage
            {
                Tracks = { tracks }
            });
        }

        var restrictions = jsonDocument.RootElement.TryGetProperty("restrictions", out var restrictionsElement)
            ? restrictionsElement.EnumerateObject().Fold(new HashMap<string, Seq<string>>(),
                (acc, x) => acc.Add(x.Name, x.Value.Clone().EnumerateArray().Select(y => y.GetString()).ToSeq()!))
            : Empty;
        return new SpotifyContext(url, metadata, pages, restrictions);
    }

    private static readonly object SendLock = new();

    private Unit CreateListener(
        string uri,
        MercuryMethod method,
        string? contentType,
        TaskCompletionSource<MercuryPacket> onCompletion,
        CancellationToken ct)
    {
        string username = _username;
        ulong seq = 0;
        lock (SendLock)
        {
            seq = SendSequences[username];
            SendSequences[username] = seq + 1;
        }

        // var seq = SendSequences.Swap(x => x.AddOrUpdate(username,
        //     None: () => 0,
        //     Some: y => y + 1
        // )).ValueUnsafe()[username];
        Debug.WriteLine($"Sending seq {seq} for {uri}");
        var reader = _onPackageReceive((ref SpotifyUnencryptedPackage check) => Condition(ref check, seq));
        SendInternal(seq, uri, method, contentType);

        Debug.WriteLine($"Waiting for seq {seq} for {uri}");
        var partials = new List<ReadOnlyMemory<byte>>();
        var sw = Stopwatch.StartNew();

        Task.Run(async () =>
        {
            await foreach (var receivedPacket in reader.Reader.ReadAllAsync(ct))
            {
                var data = receivedPacket.Data;
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
                var bodyLength = partials.Skip(1).Fold(0, (acc, x) => acc + x.Length);
                Memory<byte> body = new byte[bodyLength];
                var offset = 0;
                foreach (var part in partials.Skip(1))
                {
                    part.CopyTo(body.Slice(offset));
                    offset += part.Length;
                }

                sw.Stop();
                Debug.WriteLine($"MercuryClient.Get {uri} took {sw.ElapsedMilliseconds}ms");
                reader.Writer.Complete();
                var response = new MercuryPacket(header, body);
                onCompletion.SetResult(response);
            }
        }, ct);

        return default;
    }

    private void SendInternal(ulong seq, string uri, MercuryMethod method, string contentType)
    {
        var toSend = MercuryRequests.Build(
            seq,
            MercuryMethod.Get,
            uri,
            null,
            LanguageExt.Seq<ReadOnlyMemory<byte>>.Empty);
        _onPackageSend(toSend);
    }

    private static bool Condition(ref SpotifyUnencryptedPackage packagetocheck, ulong seq)
    {
        if (packagetocheck.Type is SpotifyPacketType.MercuryEvent
            or SpotifyPacketType.MercuryReq
            or SpotifyPacketType.MercurySub
            or SpotifyPacketType.MercuryUnsub)
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
}

internal delegate Unit SendPackage(SpotifyUnencryptedPackage package);

public readonly record struct MercuryPacket(Header Header, ReadOnlyMemory<byte> Payload);