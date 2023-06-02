using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Eum.Spotify;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NeoSmart.AsyncLock;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Infrastructure.Mercury;

internal readonly struct MercuryClient : ISpotifyMercuryClient
{
    private readonly record struct AccessToken(string Token, DateTimeOffset ExpiresAt);

    private record AccessTokenLockComposite(AccessToken Token, AsyncLock Lock);

    private static readonly Atom<HashMap<string, ulong>> SendSequences = Atom(LanguageExt.HashMap<string, ulong>.Empty);
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

    private Unit CreateListener(
        string uri,
        MercuryMethod method,
        string? contentType,
        TaskCompletionSource<MercuryPacket> onCompletion,
        CancellationToken ct)
    {
        string username = _username;
        var seq = SendSequences.Swap(x => x.AddOrUpdate(username,
            None: () => 0,
            Some: y => y + 1
        )).ValueUnsafe()[username];

        var reader = _onPackageReceive((ref SpotifyUnencryptedPackage check) => Condition(ref check, seq));
        SendInternal(seq, uri, method, contentType);

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

internal readonly record struct MercuryPacket(Header Header, ReadOnlyMemory<byte> Payload);