using System.Buffers.Binary;
using System.Text;
using System.Threading.Channels;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Sys;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Token;
using Wavee.Spotify.Configs;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Playback.Infrastructure.Key;

namespace Wavee.Spotify.Infrastructure;

internal delegate bool PackageListenerRequest(SpotifyPacket packet);

internal readonly record struct PackageListenerRecord(PackageListenerRequest Request,
    ChannelWriter<SpotifyPacket> Writer);

internal sealed class SpotifyConnection<RT>
    where RT : struct, HasLog<RT>, HasWebsocket<RT>, HasHttp<RT>, HasAudioOutput<RT>
{
    private readonly CancellationTokenSource _cts;
    private readonly ChannelWriter<SpotifyPacket> _channelWriter;
    internal readonly RT _runtime;
    private readonly InternalSpotifyConnectionInfo _info;

    private readonly AtomHashMap<Guid, PackageListenerRecord> _packetListeners =
        LanguageExt.AtomHashMap<Guid, PackageListenerRecord>.Empty;

    private readonly AtomSeq<SpotifyPacket> _packetsWithoutPurpose = LanguageExt.AtomSeq<SpotifyPacket>.Empty;

    public SpotifyConnection(InternalSpotifyConnectionInfo info,
        ChannelReader<SpotifyPacket> coreChannelReader,
        ChannelWriter<SpotifyPacket> channelWriter,
        RT runtime)
    {
        _cts = new CancellationTokenSource();
        _info = info;
        _channelWriter = channelWriter;
        _runtime = runtime;

        var pingPongChannel = Channel.CreateUnbounded<SpotifyPacket>();

        _packetListeners.Add(Guid.NewGuid(), new PackageListenerRecord(
            x => x.Command is SpotifyPacketType.Ping or SpotifyPacketType.PongAck,
            pingPongChannel.Writer));

        _ = Task.Factory.StartNew(
                async () =>
                {
                    await foreach (var packet in pingPongChannel.Reader.ReadAllAsync(_cts.Token))
                    {
                        PingPong(
                            runtime,
                            channelWriter,
                            packet);
                    }
                },
                TaskCreationOptions.LongRunning)
            .Result;

        _ = Task.Factory.StartNew(
                async () =>
                {
                    await StartChannelListener(coreChannelReader, _packetListeners, _packetsWithoutPurpose, _cts.Token);
                },
                TaskCreationOptions.LongRunning)
            .Result;
    }

    public string DeviceId => _info.Deviceid;
    public SpotifyConfig Config => _info.Config;

    // ReSharper disable once HeapView.BoxingAllocation
    public ISpotifyConnectionInfo Info => new SpotifyConnectionInfo<RT>(_runtime,
        CountryCodeAff(_packetListeners, _packetsWithoutPurpose),
        ProductInfoAff(_packetListeners, _packetsWithoutPurpose));

    public IMercuryClient Mercury => new MercuryClient(
        connectionId: _info.ConnectionId,
        channelWriter: _channelWriter,
        addPackageListener: request =>
        {
            var newId = Guid.NewGuid();
            var newChannel = Channel.CreateUnbounded<SpotifyPacket>();
            _packetListeners.Add(newId, new PackageListenerRecord(request, newChannel.Writer));
            return (newId, newChannel.Reader);
        },
        removePackageListener: id =>
        {
            var listener = _packetListeners[id];
            listener.Writer.TryComplete();
            _packetListeners.Remove(id);
        }
    );

    public ITokenClient Token => new TokenClient(
        connectionId: _info.ConnectionId,
        mercuryClient: Mercury
    );

    public Aff<RT, Either<AesKeyError, AudioKey>>
        FetchAudioKeyFunc(AudioId id, ByteString fileId, CancellationToken ct) =>
        FetchAudioKeyFunc(_channelWriter,
            addPackageListener: request =>
            {
                var newId = Guid.NewGuid();
                var newChannel = Channel.CreateUnbounded<SpotifyPacket>();
                _packetListeners.Add(newId, new PackageListenerRecord(request, newChannel.Writer));
                return (newId, newChannel.Reader);
            },
            removePackageListener: id =>
            {
                var listener = _packetListeners[id];
                listener.Writer.TryComplete();
                _packetListeners.Remove(id);
            },
            _info.ConnectionId,
            id,
            fileId,
            ct);


    private static readonly Atom<HashMap<Guid, uint>> AudioKeySequence = Atom(LanguageExt.HashMap<Guid, uint>.Empty);

    private static Aff<RT, Either<AesKeyError, AudioKey>> FetchAudioKeyFunc(
        ChannelWriter<SpotifyPacket> channelWriter,
        Func<PackageListenerRequest, (Guid ListenerId, ChannelReader<SpotifyPacket> Reader)> addPackageListener,
        Action<Guid> removePackageListener,
        Guid infoConnectionId,
        AudioId id, ByteString fileId, CancellationToken cancellationToken)
    {
        return Aff<RT, Either<AesKeyError, AudioKey>>(async rt =>
        {
            var nextSeqMap = atomic(() => AudioKeySequence.Swap(x => x.AddOrUpdate(infoConnectionId,
                Some: x => x + 1,
                None: () => 0
            )));
            var nextSeq = nextSeqMap.ValueUnsafe().Find(infoConnectionId)
                .IfNoneUnsafe(() => throw new Exception("Should not happen"));

            var (listenerId, reader) = addPackageListener(x =>
                (x.Command is SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError) &&
                BinaryPrimitives.ReadUInt32BigEndian(x.Data.Slice(0, 4).Span) == nextSeq);

            var buildPacket = AesPacketBuilder.BuildRequest(id, fileId, nextSeq);
            channelWriter.TryWrite(buildPacket);

            await foreach (var packet in reader.ReadAllAsync(cancellationToken))
            {
                removePackageListener(listenerId);
                switch (packet.Command)
                {
                    case SpotifyPacketType.AesKey:
                        var key = packet.Data.Slice(4, 16);
                        return Right(new AudioKey(key));
                    case SpotifyPacketType.AesKeyError:
                        var errorCode = packet.Data.Span[4];
                        var errorType = packet.Data.Span[5];
                        return Left(new AesKeyError(errorCode, errorType));
                }
            }

            throw new Exception("Should not happen");
        });
    }


    private static Unit PingPong(
        RT runtime,
        ChannelWriter<SpotifyPacket> pingPongWriter,
        SpotifyPacket arg)
    {
        switch (arg.Command)
        {
            case SpotifyPacketType.Ping:
            {
                var serverTimestamp =
                    BinaryPrimitives.ReadUInt32BigEndian(arg.Data.Span);
                var clientTimestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _ = Log<RT>.logInfo(
                        $"Received ping request from server with timestamp: {serverTimestamp}, Client: {clientTimestamp}")
                    .Run(runtime);

                //ping back
                var pong = new SpotifyPacket(SpotifyPacketType.Pong, new byte[4]);
                pingPongWriter.TryWrite(pong);
                break;
            }
            case SpotifyPacketType.PongAck:
            {
                var pkg = arg.Data.Span;
                _ = Log<RT>.logInfo("Received Pong acknowledgment from server.").Run(runtime);
                break;
            }
        }

        return unit;
    }

    private static Aff<RT, Option<HashMap<string, string>>> ProductInfoAff(
        AtomHashMap<Guid, PackageListenerRecord> packetListeners,
        AtomSeq<SpotifyPacket> packetsWithoutPurpose)
    {
        return Aff<RT, Option<HashMap<string, string>>>(async rt =>
        {
            await Task.Delay(1000);
            return Option<HashMap<string, string>>.None;
        });
    }

    private static Aff<RT, Option<string>> CountryCodeAff(
        AtomHashMap<Guid, PackageListenerRecord> packetListeners,
        AtomSeq<SpotifyPacket> packetsWithoutPurpose)
    {
        if (packetsWithoutPurpose.Any(f => f.Command is SpotifyPacketType.CountryCode))
        {
            var packet = packetsWithoutPurpose.First(f => f.Command is SpotifyPacketType.CountryCode);
            var countryCodeString = Encoding.UTF8.GetString(packet.Data.Span);
            return SuccessEff(Some(countryCodeString));
        }

        var channel = Channel.CreateUnbounded<SpotifyPacket>();
        var listenerId = Guid.NewGuid();
        packetListeners.Add(listenerId,
            new PackageListenerRecord(f => f.Command is SpotifyPacketType.CountryCode, channel.Writer));


        return Aff<RT, Option<string>>(async rt =>
        {
            var packet = await channel.Reader.ReadAsync();
            packetsWithoutPurpose.Add(packet);
            var countryCodeString = Encoding.UTF8.GetString(packet.Data.Span);
            channel.Writer.TryComplete();
            packetListeners.Remove(listenerId);
            return Some(countryCodeString);
        });
    }

    private static async Task StartChannelListener(ChannelReader<SpotifyPacket> coreChannelReader,
        AtomHashMap<Guid, PackageListenerRecord> packetListeners,
        AtomSeq<SpotifyPacket> packetsWithoutPurpose,
        CancellationToken ct = default)
    {
        await foreach (var packet in coreChannelReader.ReadAllAsync(ct))
        {
            if (packetListeners.Any())
            {
                foreach (var listener in packetListeners)
                {
                    if (listener.Value.Request(packet))
                    {
                        listener.Value.Writer.TryWrite(packet);
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