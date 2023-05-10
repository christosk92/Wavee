using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Channels;
using LanguageExt.Effects.Traits;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Spotify.Infrastructure;

internal sealed class SpotifyConnection<RT> : ISpotifyConnection where RT : struct, HasCancel<RT>
{
    private readonly CancellationTokenSource _cts;
    private readonly ChannelWriter<SpotifyPacket> _channelWriter;
    private readonly ChannelReader<SpotifyPacket> _coreChannelReader;
    private readonly RT _runtime;
    private readonly InternalSpotifyConnectionInfo _info;

    private readonly AtomHashMap<Func<SpotifyPacket, bool>, Func<SpotifyPacket, bool>> _packetListeners =
        LanguageExt.AtomHashMap<Func<SpotifyPacket, bool>, Func<SpotifyPacket, bool>>.Empty;

    private readonly AtomSeq<SpotifyPacket> _packetsWithoutPurpose = LanguageExt.AtomSeq<SpotifyPacket>.Empty;

    public SpotifyConnection(InternalSpotifyConnectionInfo info, ChannelReader<SpotifyPacket> coreChannelReader,
        ChannelWriter<SpotifyPacket> channelWriter,
        RT runtime)
    {
        _cts = new CancellationTokenSource();
        _info = info;
        _coreChannelReader = coreChannelReader;
        _channelWriter = channelWriter;
        _runtime = runtime;

        //start a ping listener
        _packetListeners.Add(x => x.Command is SpotifyPacketType.Ping or SpotifyPacketType.PongAck, PingPong);

        _ = Task.Factory.StartNew(
                async () =>
                {
                    await StartChannelListener(coreChannelReader, _packetListeners, _packetsWithoutPurpose, _cts.Token);
                },
                TaskCreationOptions.LongRunning)
            .Result;
    }

    private bool PingPong(SpotifyPacket arg)
    {
        switch (arg.Command)
        {
            case SpotifyPacketType.Ping:
            {
                //ping back
                var pong = new SpotifyPacket(SpotifyPacketType.Pong, new byte[4]);
                _channelWriter.TryWrite(pong);
                break;
            }
            case SpotifyPacketType.PongAck:
            {
                break;
            }
        }

        return true;
    }


    public ISpotifyConnectionInfo Info => new SpotifyConnectionInfo<RT>(_runtime,
        CountryCodeAff(_packetListeners, _packetsWithoutPurpose),
        ProductInfoAff(_packetListeners, _packetsWithoutPurpose));

    private static Aff<RT, Option<HashMap<string, string>>> ProductInfoAff(
        AtomHashMap<Func<SpotifyPacket, bool>, Func<SpotifyPacket, bool>> packetListeners,
        AtomSeq<SpotifyPacket> packetsWithoutPurpose)
    {
        return Aff<RT, Option<HashMap<string, string>>>(async rt =>
        {
            await Task.Delay(1000);
            return Option<HashMap<string, string>>.None;
        });
    }

    private static Aff<RT, Option<string>> CountryCodeAff(
        AtomHashMap<Func<SpotifyPacket, bool>, Func<SpotifyPacket, bool>> packetListeners,
        AtomSeq<SpotifyPacket> packetsWithoutPurpose)
    {
        if (packetsWithoutPurpose.Any(f => f.Command is SpotifyPacketType.CountryCode))
        {
            var packet = packetsWithoutPurpose.First(f => f.Command is SpotifyPacketType.CountryCode);
            var countryCodeString = Encoding.UTF8.GetString(packet.Data.Span);
            return SuccessEff(Some(countryCodeString));
        }

        var tcs = new TaskCompletionSource<Option<string>>();
        var listener = new Func<SpotifyPacket, bool>(packet =>
        {
            if (packet.Command is SpotifyPacketType.CountryCode)
            {
                var countryCodeString = Encoding.UTF8.GetString(packet.Data.Span);
                tcs.SetResult(Some(countryCodeString));
            }

            return false;
        });

        packetListeners.Add(f => f.Command is SpotifyPacketType.CountryCode, listener);

        return Aff<RT, Option<string>>(async rt =>
        {
            await tcs.Task;
            return tcs.Task.Result;
        });
    }

    private static async Task StartChannelListener(ChannelReader<SpotifyPacket> coreChannelReader,
        AtomHashMap<Func<SpotifyPacket, bool>, Func<SpotifyPacket, bool>> packetListeners,
        AtomSeq<SpotifyPacket> packetsWithoutPurpose,
        CancellationToken ct = default)
    {
        await foreach (var packet in coreChannelReader.ReadAllAsync(ct))
        {
            if (packetListeners.Any())
            {
                foreach (var listener in packetListeners)
                {
                    if (listener.Key(packet))
                    {
                        if (listener.Value(packet))
                        {
                            packetsWithoutPurpose.Add(packet);
                        }
                        packetListeners.Remove(listener.Key);
                    }
                    else
                    {
                        packetsWithoutPurpose.Add(packet);
                    }

                }
            }
            else
            {
                packetsWithoutPurpose.Add(packet);
            }
        }
    }
}