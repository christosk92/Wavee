using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading.Channels;
using CommunityToolkit.HighPerformance;
using Eum.Spotify;
using Serilog;
using Serilog.Core;
using Wavee.Infrastructure.Authentication;
using Wavee.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Handshake;
using Wavee.Token.Live;

namespace Wavee.Infrastructure.Connection;

internal static class SpotifyConnection
{
    private record SpotifyConnectionInfo(NetworkStream Stream, ChannelWriter<BoxedSpotifyPackage> Sender, Func<PackageReceiveCondition, SpotifyPackageCallback> Receiver, List<SpotifyPackageCallbackFull> Callbacks);
    private readonly record struct SpotifyPackageCallbackFull(Channel<BoxedSpotifyPackage> Reader, PackageReceiveCondition PackageReceiveCondition, Action<BoxedSpotifyPackage> Incoming);

    private static readonly Dictionary<Guid, SpotifyConnectionInfo> Connections = new();

    public static SpotifyPackageCallback CreateListener(this Guid connectionId, PackageReceiveCondition condition)
    {
        var connection = Connections[connectionId];
        return connection.Receiver(condition);
    }

    public static void Send(Guid connectionId, BoxedSpotifyPackage toSend)
    {
        var connection = Connections[connectionId];
        connection.Sender.TryWrite(toSend);
    }

    public static void Close(Guid connectionId)
    {
        if (Connections.TryGetValue(connectionId, out var connection))
        {
            connection.Sender.Complete();
            connection.Stream.Close();

            //remove all callbacks
            foreach (var callback in connection.Callbacks)
            {
                callback.Reader.Writer.TryComplete();
            }

            MercuryParsers.Reset(connectionId);
            connection.Callbacks.Clear();
        }
    }

    public static void Dispose(Guid connectionId)
    {
        var connection = Connections[connectionId];
        connection.Sender.Complete();
        connection.Stream.Dispose();
        Connections.Remove(connectionId);

        //remove all callbacks
        foreach (var callback in connection.Callbacks)
        {
            callback.Reader.Writer.TryComplete();
        }
        MercuryParsers.Reset(connectionId);
        connection.Callbacks.Clear();
    }

    private static void SetupConnectionListener(APWelcome welcomeMessage, NetworkStream stream,
        SpotifyEncryptionKeys keys, Guid connectionId, Action<Exception> onConnectionLost)
    {
        //Two way communication
        //Send and receive
        object _lock = new();
        var packages = new List<BoxedSpotifyPackage>();

        //Send
        var hostToServer = Channel.CreateUnbounded<BoxedSpotifyPackage>();
        Connections[connectionId] = new SpotifyConnectionInfo(stream, hostToServer.Writer, Receiver: (condition) =>
        {
            lock (_lock)
            {
                var newChannel = Channel.CreateUnbounded<BoxedSpotifyPackage>();
                var callbackFull =
                    new SpotifyPackageCallbackFull(newChannel, condition,
                        (pkg) => { newChannel.Writer.TryWrite(pkg); });
                var callback = new SpotifyPackageCallback(newChannel.Reader, () =>
                {
                    Connections[connectionId].Callbacks.Remove(callbackFull);
                    newChannel.Writer.TryComplete();
                });
                Connections[connectionId].Callbacks.Add(callbackFull);

                //check if we have any packages that match the condition already
                foreach (var package in packages)
                {
                    var asRefPackage = new SpotifyUnencryptedPackage(package.Type, package.Payload.Span);
                    if (condition(ref asRefPackage))
                    {
                        callbackFull.Incoming(package);
                    }
                }

                return callback;
            }
        }, new List<SpotifyPackageCallbackFull>());
        object _reconnectionLock = new();
        bool called = false;

        Task.Factory.StartNew(async () =>
        {
            //Send:
            int sequence = 1; //1 because 0 is reserved for the handshake
            await foreach (var package in hostToServer.Reader.ReadAllAsync())
            {
                try
                {
                    Log.Debug("Sending package {PackageType} with sequence {Sequence}", package.Type, sequence);
                    Send(stream, new SpotifyUnencryptedPackage(
                            type: package.Type,
                            payload: package.Payload.Span
                        ),
                        sendKey: keys.SendKey.Span,
                        sequence: sequence);
                    sequence++;
                }
                catch (Exception e)
                {
                    lock (_reconnectionLock)
                    {
                        if (!called)
                        {
                            Log.Error(e, "Error while receiving package. Attempting reconnection.");
                            called = true;
                            Close(connectionId);
                            onConnectionLost(e);
                        }
                    }

                    return;
                }
            }
        });

        Task.Factory.StartNew(() =>
        {
            //Receive:
            int sequence = 1; //1 because 0 is reserved for the handshake
            while (true)
            {
                try
                {
                    var package = Receive(stream, keys.ReceiveKey.Span, sequence);
                    Log.Debug("Received package {PackageType} with sequence {Sequence}", package.Type, sequence);
                    sequence++;
                    lock (_lock)
                    {
                        bool wasInteresting = false;
                        foreach (var callback in Connections[connectionId].Callbacks)
                        {
                            if (callback.PackageReceiveCondition(ref package))
                            {
                                var boxed = new BoxedSpotifyPackage(package.Type, package.Payload.ToArray());
                                callback.Incoming(boxed);
                                wasInteresting = true;
                            }
                        }

                        if (!wasInteresting)
                        {
                            //We don't have any callbacks for this package, so we store it for later
                            packages.Add(new BoxedSpotifyPackage(package.Type, package.Payload.ToArray()));
                        }
                    }
                }
                catch (Exception e)
                {
                    lock (_reconnectionLock)
                    {
                        if (!called)
                        {
                            Log.Error(e, "Error while receiving package. Attempting reconnection.");
                            called = true;
                            Close(connectionId);
                            onConnectionLost(e);
                        }
                    }

                    return;
                }
            }
        });

        //Setup a ping listener
        var pingListener = connectionId.CreateListener((ref SpotifyUnencryptedPackage package) =>
        {
            if (package.Type is SpotifyPacketType.Ping or SpotifyPacketType.PongAck)
            {
                return true;
            }

            return false;
        });
        Task.Factory.StartNew(async () =>
        {
            await foreach (var package in pingListener.Reader.ReadAllAsync())
            {
                if (package.Type is SpotifyPacketType.Ping)
                {
                    var empty4bytes = new byte[4];
                    Log.Debug("Received ping");
                    Send(connectionId, new BoxedSpotifyPackage(
                        type: SpotifyPacketType.Pong,
                        empty4bytes
                    ));
                }
                else
                {
                    Log.Debug("Received pong acknowledgment");
                }
            }
        });

        Log.Information("Connection setup complete with {ConnectionId}", connectionId);
    }

    internal static void Send(NetworkStream stream, SpotifyUnencryptedPackage package, ReadOnlySpan<byte> sendKey,
        int sequence)
    {
        const int MacLength = 4;
        const int HeaderLength = 3;

        var shannon = new Shannon(sendKey);
        Span<byte> encoded = stackalloc byte[HeaderLength + package.Payload.Length + MacLength];
        encoded[0] = (byte)package.Type;

        BinaryPrimitives.WriteUInt16BigEndian(encoded[1..], (ushort)package.Payload.Length);


        package.Payload.CopyTo(encoded[3..]);
        shannon.Nonce((uint)sequence);

        shannon.Encrypt(encoded[..(3 + package.Payload.Length)]);

        Span<byte> mac = stackalloc byte[MacLength];
        shannon.Finish(mac);

        mac.CopyTo(encoded[(3 + package.Payload.Length)..]);
        stream.Write(encoded);
    }

    /// <summary>
    /// Does a blocking read on the stream and returns a <see cref="SpotifyUnencryptedPackage"/> if successful.
    /// </summary>
    /// <param name="stream">
    /// The stream to read from.
    /// </param>
    /// <param name="receiveKey">
    ///  A <see cref="ReadOnlySpan{T}"/> containing the receive key, used for shannon decryption.
    /// </param>
    /// <param name="sequence">
    ///  The current sequence number. Used as a nonce for shannon decryption.
    /// </param>
    /// <returns>
    ///  The received <see cref="SpotifyUnencryptedPackage"/>.
    /// </returns>
    /// <exception cref="InvalidSignatureResult">
    /// The mac of the received package did not match the expected mac.
    /// </exception>
    internal static SpotifyUnencryptedPackage Receive(NetworkStream stream, ReadOnlySpan<byte> receiveKey, int sequence)
    {
        var key = new Shannon(receiveKey);
        Span<byte> header = new byte[3];
        stream.ReadExactly(header);
        key.Nonce((uint)sequence);
        key.Decrypt(header);

        var payloadLength = (short)((header[1] << 8) | (header[2] & 0xFF));
        Span<byte> payload = new byte[payloadLength];
        stream.ReadExactly(payload);
        key.Decrypt(payload);

        Span<byte> mac = stackalloc byte[4];
        stream.ReadExactly(mac);

        Span<byte> expectedMac = stackalloc byte[4];
        key.Finish(expectedMac);

        if (!mac.SequenceEqual(expectedMac))
        {
            throw new InvalidSignatureResult();
            //  throw new Exception("MAC mismatch");
        }

        return new SpotifyUnencryptedPackage((SpotifyPacketType)header[0], payload);
    }

    public static (Guid connectionId, APWelcome welcomeMessage) Create(LoginCredentials credentials, SpotifyConfig config, string deviceId,
        Action<Exception> onConnectionLost,
        Guid? persistentConnectionId)
    {
        const string host = "ap-gae2.spotify.com";
        const ushort port = 4070;

        var tcp = TcpIO.Connect(host, port);
        var stream = tcp.GetStream();
        var keys = Handshake.Handshake.PerformHandshake(stream);
        var welcomeMessage = Auth.Authenticate(stream, keys, credentials, deviceId, config);
        //Setup a new connection listener
        var connectionId = persistentConnectionId ?? Guid.NewGuid();

        SetupConnectionListener(welcomeMessage, stream, keys, connectionId, onConnectionLost);

        return (connectionId, welcomeMessage);
    }
}

internal readonly record struct SpotifyPackageCallback(ChannelReader<BoxedSpotifyPackage> Reader, Action Finished);

internal delegate bool PackageReceiveCondition(ref SpotifyUnencryptedPackage packageToCheck);