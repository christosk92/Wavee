using System.Buffers.Binary;
using System.Diagnostics;
using Eum.Spotify;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Infrastructure.Live;
using Wavee.Spotify.Infrastructure.Sys;
using Wavee.Spotify.Infrastructure.Traits;
using Wavee.Spotify.Models.Internal;

namespace Wavee.Spotify.Infrastructure.Common.Mercury;

internal readonly struct InternalMercurcyClient<RT> where RT : struct, HasTCP<RT>
{
    private readonly Func<Eff<Option<ulong>>> _getSeq;
    private readonly Func<ulong, Eff<Task<MercuryResponse>>> _registerCallback;
    private readonly Func<SpotifyPacket, Aff<RT, Unit>> _send;

    private readonly RT _runtime;

    internal InternalMercurcyClient(Func<SpotifyPacket, Aff<RT, Unit>> sendFromMercury,
        Func<ulong, Eff<Task<MercuryResponse>>> registerMercuryCallback, Func<Eff<Option<ulong>>> getMercurySeq,
        RT runtime1)
    {
        _send = sendFromMercury;
        _registerCallback = registerMercuryCallback;
        _getSeq = getMercurySeq;
        _runtime = runtime1;
    }

    public async Task<MercuryResponse> Get(string uri)
    {
        var aff =
            Mercury<RT>.Get(uri, _getSeq, _send, _registerCallback);
        var fin = await aff.Run(_runtime);

        var task = fin
            .Match(
                Succ: x => x,
                Fail: e => throw new Exception(e.ToString()));

        return await Task.Run(async () => await task);
    }
}

internal readonly struct MercuryClient<RT> : IMercuryClient where RT : struct, HasTCP<RT>
{
    private readonly InternalMercurcyClient<RT> _client;

    internal MercuryClient(Func<SpotifyPacket, Aff<RT, Unit>> sendFromMercury,
        Func<ulong, Eff<Task<MercuryResponse>>> registerMercuryCallback, Func<Eff<Option<ulong>>> getMercurySeq,
        RT runtime1)
    {
        _client = new InternalMercurcyClient<RT>(sendFromMercury, registerMercuryCallback, getMercurySeq,
            runtime1);
    }

    public Task<MercuryResponse> Get(string uri) => _client.Get(uri);

    internal static Unit Handle(SpotifyPacket packet, Ref<HashMap<ulong, MercuryPending>> mercuryCallbacks)
    {
        var data = packet.Payload;
        var seqLen = SeqLen(ref data);
        var seq = Seq(ref data, seqLen);

        var flags = Flag(ref data);
        var count = Count(ref data);

        var pending = mercuryCallbacks.Value.Find(seq).Match(
            Some: p => p,
            None: () => new MercuryPending(
                Parts: LanguageExt.Seq.empty<ReadOnlyMemory<byte>>(),
                Partial: None,
                Callback: None,
                Flag: false
            ));

        for (int i = 0; i < count; i++)
        {
            var part = ParsePart(ref data);
            // pending = pending.WithPartial(part);
            // part = pending.Partial.Value();

            if (pending.Partial.IsSome)
            {
                var partial = pending.Partial.ValueUnsafe();
                pending.WithPartial(None);

                //extend the partial
                Memory<byte> newPartial = new byte[partial.Length + partial.Length];
                partial.CopyTo(newPartial);
                data.CopyTo(newPartial[partial.Length..]);

                part = newPartial;
                pending = pending.WithPartial(Some(partial));
            }

            if (i == count && (flags == 2))
            {
                pending = pending.WithPartial(part);
            }
            else
            {
                pending = pending.WithPart(part);
            }

            // r = r.AddOrUpdate(seq, pending);
        }

        if (flags == 0x1)
        {
            pending = pending.WithFlag(true);
        }

        atomic(() => mercuryCallbacks.Swap(f => f.AddOrUpdate(seq, pending)));

        var cmd = packet.Command;

        //if we are don remove the callback and set the result
        if (pending.Flag)
        {
            var completedRequest = CompleteRequest(seq, cmd, pending);
            atomic(() => mercuryCallbacks.Swap(f =>
            {
                var find = f.Find(seq);
                if (find.IsSome)
                {
                    find.IfSome(x => x.Callback.IfSome(cb => { cb.SetResult(completedRequest); }));
                    return f.Remove(seq);
                }

                return f;
            }));
        }
        else
        {
            //update the pending
            atomic(() => mercuryCallbacks.Swap(f => f.AddOrUpdate(seq, pending)));
        }

        return unit;
    }

    private static MercuryResponse CompleteRequest(ulong seq, PacketType cmd, MercuryPending pending)
    {
        var headerData = pending.Parts[0];
        var header = Header.Parser.ParseFrom(headerData.Span);

        var payload = pending.Parts.Skip(1);
        var parts = payload.Count;
        var totalLength = payload.Sum(c => c.Length);
        var response = new MercuryResponse(
            Seq: seq,
            Uri: header.Uri,
            StatusCode: header.StatusCode,
            Payload: payload,
            TotalLength: totalLength
        );


        if (cmd is PacketType.MercuryEvent)
        {
            //TODO:
            Debugger.Break();
        }

        return response;
    }


    private static ReadOnlyMemory<byte> ParsePart(ref ReadOnlyMemory<byte> data)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(data.Span[..2]);
        data = data[2..];
        var body = data[..size];
        data = data[size..];
        return body;
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

    private static ushort SeqLen(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        data = data[2..];
        return l;
    }

    private static ulong Seq(ref ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        data = data[len..];
        return l;
    }
}

public interface IMercuryClient
{
    Task<MercuryResponse> Get(string uri);
}