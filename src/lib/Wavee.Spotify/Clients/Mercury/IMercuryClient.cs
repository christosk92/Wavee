using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Eum.Spotify;
using Eum.Spotify.context;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Core.Id;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Infrastructure;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Spotify.Clients.Mercury;

public interface IMercuryClient
{
    Task<MercuryResponse> Get(string uri, CancellationToken ct = default);
    Task<Track> GetTrack(AudioId id, CancellationToken ct = default);
    Task<Episode> GetEpisode(AudioId id, CancellationToken ct = default);
    Task<string> AutoplayQuery(string uri, CancellationToken ct = default);
    Task<SpotifyContext> ContextResolve(string uri, CancellationToken ct = default);
    Task<SpotifyContext> ContextResolveRaw(string uri, CancellationToken ct = default);
}

public readonly record struct SpotifyContext(string Url, HashMap<string, string> Metadata, Seq<ContextPage> Pages,
    HashMap<string, Seq<string>> Restrictions);

internal readonly struct MercuryClient : IMercuryClient
{
    private static readonly AtomHashMap<Guid, ulong> SequenceNumbers =
        LanguageExt.AtomHashMap<Guid, ulong>.Empty;

    private readonly Action<Guid> _removePackageListener;

    private readonly Func<PackageListenerRequest, (Guid ListenerId, ChannelReader<SpotifyPacket> Reader)>
        _addPackageListener;

    private readonly ChannelWriter<SpotifyPacket> _channelWriter;
    private readonly Guid _connectionId;

    public MercuryClient(
        Guid connectionId,
        ChannelWriter<SpotifyPacket> channelWriter,
        Func<PackageListenerRequest, (Guid ListenerId, ChannelReader<SpotifyPacket> Reader)> addPackageListener,
        Action<Guid> removePackageListener)
    {
        _connectionId = connectionId;
        _channelWriter = channelWriter;
        _addPackageListener = addPackageListener;
        _removePackageListener = removePackageListener;
    }

    public async Task<MercuryResponse> Get(string uri, CancellationToken ct = default)
    {
        SequenceNumbers
            .AddOrUpdate(_connectionId, None: () => 0UL, Some: existing => existing + 1);
        var seq = SequenceNumbers.Find(_connectionId).IfNone(0UL);
        var packet = Build(seq,
            MercuryMethod.Get,
            uri,
            None,
            None);
        var (listenerId, listener) = _addPackageListener(spotifyPacket =>
        {
            //check seq number
            if (spotifyPacket.Command is SpotifyPacketType.MercuryEvent
                or SpotifyPacketType.MercuryReq
                or SpotifyPacketType.MercurySub
                or SpotifyPacketType.MercuryUnsub)
            {
                var seqLength = BinaryPrimitives.ReadUInt16BigEndian(spotifyPacket.Data.Span.Slice(0, 2));
                var calculatedSeq = BinaryPrimitives.ReadUInt64BigEndian(spotifyPacket.Data.Span.Slice(2, seqLength));
                if (calculatedSeq != seq)
                    return false;

                return true;
            }

            return false;
        });

        _channelWriter.TryWrite(packet);


        Seq<ReadOnlyMemory<byte>> partials = Seq<ReadOnlyMemory<byte>>();
        await foreach (var readPacket in listener.ReadAllAsync(ct))
        {
            var data = readPacket.Data;
            var seqLen = SeqLenRef(ref data);
            var foundSeq = SeqRef(ref data, seqLen);
            var flags = Flag(ref data);
            var count = Count(ref data);

            for (int i = 0; i < count; i++)
            {
                var part = ParsePart(ref data);
                partials = partials.Add(part);
            }

            if (flags != 1) continue;
            var header = Header.Parser.ParseFrom(partials[0].Span);
            var bodyLength = partials.Skip(1).Fold(0, (acc, x) => acc + x.Length);
            Memory<byte> body = new byte[bodyLength];
            var offset = 0;
            foreach (var part in partials.Skip(1))
            {
                part.CopyTo(body.Slice(offset));
                offset += part.Length;
            }

            _removePackageListener(listenerId);
            return new MercuryResponse(header, body);
        }

        return default;
    }

    public async Task<Track> GetTrack(AudioId id, CancellationToken ct = default)
    {
        const string uri = "hm://metadata/4/track/{0}?market=from_token";
        var response = await Get(string.Format(uri, id.ToBase16()), ct);
        return Track.Parser.ParseFrom(response.Body.Span);
    }

    public async Task<Episode> GetEpisode(AudioId id, CancellationToken ct = default)
    {
        const string uri = "hm://metadata/4/episode/{0}";
        var response = await Get(string.Format(uri, id.ToBase16()), ct);
        return Episode.Parser.ParseFrom(response.Body.Span);
    }

    public async Task<string> AutoplayQuery(string uri, CancellationToken ct = default)
    {
        const string query = "hm://autoplay-enabled/query?uri={0}";
        var response = await Get(string.Format(query, uri), ct);
        var context = Encoding.UTF8.GetString(response.Body.Span);
        return context;
    }

    //ContextResolveRaw
    public async Task<SpotifyContext> ContextResolveRaw(string uri, CancellationToken ct = default)
    {
        var response = await Get(uri, ct);
        using var jsonDocument = JsonDocument.Parse(response.Body);
        var parsed = Parse(jsonDocument);
        return parsed;
    }

    public async Task<SpotifyContext> ContextResolve(string uri, CancellationToken ct = default)
    {
        const string query = "hm://context-resolve/v1/{0}";
        var response = await Get(string.Format(query, uri), ct);
        using var jsonDocument = JsonDocument.Parse(response.Body);
        var parsed = Parse(jsonDocument);
        return parsed;
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

    private static SpotifyPacket Build(ulong seqNumber,
        MercuryMethod mercuryMethod,
        string uri,
        Option<string> contentType,
        Option<Seq<Memory<byte>>> payload)
    {
        Span<byte> seq = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(seq, seqNumber);

        var cmd = mercuryMethod switch
        {
            MercuryMethod.Get => SpotifyPacketType.MercuryReq,
            MercuryMethod.Sub => SpotifyPacketType.MercurySub,
            MercuryMethod.Unsub => SpotifyPacketType.MercuryUnsub,
            MercuryMethod.Send => SpotifyPacketType.MercuryReq,
            _ => throw new ArgumentOutOfRangeException()
        };

        var header = new Header
        {
            Uri = uri,
            Method = mercuryMethod.ToString().ToUpper()
        };

        // if (ContentType != null) header.ContentType = ContentType;
        contentType.IfSome(c => header.ContentType = c);
        ReadOnlyMemory<byte> headerSpan = header.ToByteArray();

        var payloadCount = payload.Count();
        Memory<byte> packet = new byte[
            sizeof(ushort) // seq length
            + seq.Length // seq
            + sizeof(byte) // flags
            + sizeof(ushort) // part count
            + sizeof(ushort) //header length
            + headerSpan.Length // header
            + payloadCount * (sizeof(ushort) + 1) // part length
        ];

        BinaryPrimitives.WriteUInt16BigEndian(packet.Span, (ushort)seq.Length);
        seq.CopyTo(packet.Span.Slice(sizeof(ushort)));
        packet.Span[sizeof(ushort) + seq.Length] = 1; // flags: FINAL
        BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                + seq.Length + 1),
            (ushort)(1 + payloadCount)); // part count

        // header length
        BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                + seq.Length + 1 + sizeof(ushort)),
            (ushort)headerSpan.Length);

        // header
        headerSpan.CopyTo(packet.Slice(sizeof(ushort)
                                       + seq.Length + 1 + sizeof(ushort) + sizeof(ushort)));

        for (var index = 0; index < payloadCount; index++)
        {
            //if we are in this loop, we can assume that the payload is not empty
            var part = payload.ValueUnsafe()[index].Span;
            BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                    + seq.Length + 1 + sizeof(ushort)
                                                                    + sizeof(ushort) + headerSpan.Length
                                                                    + index * (sizeof(ushort) + 1)),
                (ushort)part.Length);
            BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                    + seq.Length + 1 + sizeof(ushort)
                                                                    + sizeof(ushort) + headerSpan.Length
                                                                    + index * (sizeof(ushort) + 1)
                                                                    + sizeof(ushort)),
                (ushort)part.Length);
        }

        return new SpotifyPacket(cmd, packet);
    }
}

public readonly record struct MercuryResponse(Header Header, ReadOnlyMemory<byte> Body);