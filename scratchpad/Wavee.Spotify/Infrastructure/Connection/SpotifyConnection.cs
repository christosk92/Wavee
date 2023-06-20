using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Xml;
using Eum.Spotify;
using Wavee.Spotify.Infrastructure.Handshake;

namespace Wavee.Spotify.Infrastructure.Connection;

internal sealed class SpotifyConnection : IDisposable
{
    private readonly List<(PackageReceiveCondition condition, ChannelWriter<BoxedSpotifyPackage> Writer)> _callbacks =
        new();

    private readonly SpotifyEncryptionKeys _keys;
    private readonly APWelcome _welcome;
    private readonly ChannelWriter<BoxedSpotifyPackage> _writer;
    private readonly NetworkStream _stream;

    public SpotifyConnection(NetworkStream stream, SpotifyEncryptionKeys keys, APWelcome welcome)
    {
        _stream = stream;
        _keys = keys;
        _welcome = welcome;

        var channels = Channel.CreateUnbounded<BoxedSpotifyPackage>();
        _writer = channels.Writer;
        Task.Factory.StartNew(async () =>
        {
            int sendNonce = 1;
            await foreach (var package in channels.Reader.ReadAllAsync())
            {
                try
                {
                    Send(_stream, new SpotifyUnencryptedPackage(package.Type, package.Payload.Span), _keys.SendKey.Span,
                        sendNonce);
                    sendNonce++;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    Dispose();
                    throw;
                }
            }
        });

        Task.Factory.StartNew(() =>
        {
            try
            {
                var receiveNonce = 1;
                var empty4Bytes = new byte[4];
                while (true)
                {
                    var package = Receive(stream, _keys.ReceiveKey.Span, receiveNonce);
                    receiveNonce++;
                    switch (package.Type)
                    {
                        case SpotifyPacketType.Ping:
                            var response = new BoxedSpotifyPackage(SpotifyPacketType.Pong, empty4Bytes);
                            Send(response);
                            break;
                        case SpotifyPacketType.PongAck:
                            Debug.WriteLine("PongAck");
                            break;
                        case SpotifyPacketType.CountryCode:
                            var countryCode = Encoding.UTF8.GetString(package.Payload);
                            Debug.WriteLine($"CountryCode: {countryCode}");
                            CountryCode = countryCode;
                            break;
                        case SpotifyPacketType.ProductInfo:
                            var productInfo = Encoding.UTF8.GetString(package.Payload);
                            var xml = new XmlDocument();
                            xml.LoadXml(productInfo);

                            var products = xml.SelectNodes("products");
                            var dc = new Dictionary<string, string>();
                            if (products != null && products.Count > 0)
                            {
                                var firstItemAsProducts = products[0];

                                var product = firstItemAsProducts.ChildNodes[0];

                                var properties = product.ChildNodes;
                                for (var i = 0; i < properties.Count; i++)
                                {
                                    var node = properties.Item(i);
                                    dc.Add(node.Name, node.InnerText);
                                }
                            }

                            //check if product is premium
                            var isPremium = dc["catalogue"] == "premium";
                            if (!isPremium)
                            {
                                Debug.WriteLine("Sorry, this account is not premium. Goodbye");
                                Console.WriteLine("Sorry, this account is not premium. Goodbye");
                                Environment.Exit(0);
                                throw new Exception("Sorry, this account is not premium. Goodbye");
                            }

                            break;
                        default:
                            bool wasInteresting = false;
                            foreach (var callback in _callbacks)
                            {
                                if (callback.condition(ref package))
                                {
                                    wasInteresting = true;
                                    if (!callback.Writer.TryWrite(new BoxedSpotifyPackage(
                                            package.Type,
                                            package.Payload.ToArray()
                                        )))
                                    {
                                        Debugger.Break();
                                        _callbacks.Remove(callback);
                                    }
                                }
                            }

                            if (!wasInteresting)
                            {
                                Debug.WriteLine($"Received unhandled package: {package.Type}");
                            }

                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Dispose();
                throw;
            }
        });
    }


    public void Send(BoxedSpotifyPackage toSend)
    {
        _writer.TryWrite(toSend);
    }

    public Guid ConnectionId { get; } = Guid.NewGuid();
    public APWelcome WelcomeMessage => _welcome;
    public string? CountryCode { get; private set; }

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

    public (ChannelReader<BoxedSpotifyPackage> Reader, Action onDone) ListenForPackage(
        PackageReceiveCondition condition)
    {
        var result = Channel.CreateUnbounded<BoxedSpotifyPackage>();
        _callbacks.Add((condition, result.Writer));
        return (result.Reader, () =>
        {
            result.Writer.TryComplete();
            _callbacks.Remove((condition, result.Writer));
        });
    }

    public void Dispose()
    {
        _stream.Dispose();
        _writer.TryComplete();
    }
}

internal delegate bool PackageReceiveCondition(ref SpotifyUnencryptedPackage packageToCheck);