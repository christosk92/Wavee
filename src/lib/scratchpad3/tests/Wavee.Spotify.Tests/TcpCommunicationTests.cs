using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using LanguageExt;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Spotify.Connection;
using Wavee.Spotify.Crypto;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Tests;

public class TcpCommunicationTests
{
    [Fact]
    public async Task TestMessageExchange()
    {
        int serverPort = 12345;
        //Simulate spotify
        var serverTask = Task.Run(async () =>
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, serverPort);
            listener.Start();
            using TcpClient serverClient = await listener.AcceptTcpClientAsync();
            using NetworkStream serverStream = serverClient.GetStream();

            // Echo back received messages
            byte[] buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                await serverStream.WriteAsync(buffer, 0, bytesRead);
            }
        });

        // Connect to the loopback TCP server
        var tcpClientResult = await Tcp<WaveeRuntime>
            .Connect("localhost", (ushort)serverPort).Run(WaveeCore.Runtime);
        if (tcpClientResult.IsFail)
        {
            throw new Exception("Could not connect to server");
        }

        var client = tcpClientResult.Match(Succ: t => t, Fail: e => throw e);
        using NetworkStream stream = client.GetStream();
        // Send a message to the server
        string messageToSend = "Hello, World!";
        byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
        var run = await Tcp<WaveeRuntime>.Write(stream, messageBytes).Run(WaveeCore.Runtime);
        if (run.IsFail)
        {
            throw new Exception("Could not write to server");
        }

        // Receive the message from the server
        var receivedBytesResult =
            await Tcp<WaveeRuntime>.ReadExactly(stream, messageBytes.Length).Run(WaveeCore.Runtime);
        var receivedBytes = receivedBytesResult.Match(Succ: t => t, Fail: e => throw e);

        string receivedMessage = Encoding.UTF8.GetString(receivedBytes.Span);

        // Assert that the sent and received messages are the same
        Assert.Equal(messageToSend, receivedMessage);
    }


    private static byte[] testKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    [Fact]
    public async Task ProcessMessagesTest()
    {
        // Set up the test server
        var testServer = new TcpListener(IPAddress.Loopback, 0);
        testServer.Start();
        var testServerTask = Task.Run(async () =>
        {
            using var serverClient = await testServer.AcceptTcpClientAsync();
            using var serverStream = serverClient.GetStream();

            // Test server logic: read, process, and respond to client messages
            var serverBuffer = new byte[256];
            int bytesRead;
            while ((bytesRead = await serverStream.ReadAsync(serverBuffer)) > 0)
            {
                // Echo the received message back to the client
                await serverStream.WriteAsync(serverBuffer.AsMemory(0, bytesRead));
            }
        });


        // Connect to the test server
        var hostPortResponse = ("127.0.0.1", (ushort)((IPEndPoint)testServer.LocalEndpoint).Port);

        var encryptionRecordAfterAuth =
            Ref(new SpotifyEncryptionRecord(testKey, 0, testKey, 0));

        var channel = Channel.CreateUnbounded<SpotifyPacket>();
        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(hostPortResponse.Item1, hostPortResponse.Item2);
        var stream = tcpClient.GetStream();

        var processMessagesTask =
            Task.Run(() =>
            {
                return SpotifyRuntime.ProcessMessages<WaveeRuntime>(channel, stream, encryptionRecordAfterAuth,
                        Option<ISpotifyListener>.None)
                    .Run(WaveeCore.Runtime);
            });

        // Test sending and receiving messages
        var testMessage = Encoding.UTF8.GetBytes("This is a test message.");
        var testPacket = new SpotifyPacket(SpotifyPacketType.Ping, testMessage);

        var testMessageWithoutEncryption = testMessage.ToArray();
        var responseBuffer = new byte[256];
        await channel.Writer.WriteAsync(testPacket);

        // var responseMaybe = await Authentication<WaveeRuntime>.ReadDecryptedMessage(stream, encryptionRecordAfterAuth)
        //     .Run(WaveeCore.Runtime);
        // var (packet, newRecord) = responseMaybe.Match(Succ: t => t, Fail: e => throw e);

        //
        int responseBytes = await stream.ReadAsync(responseBuffer);

        var decryptedResponse = new byte[responseBytes];
        System.Array.Copy(responseBuffer, decryptedResponse, responseBytes);


        atomic(() => encryptionRecordAfterAuth.Swap(x =>
            x.Decrypt(decryptedResponse)));

        var command = (SpotifyPacketType)decryptedResponse[0];
        var data = decryptedResponse[3..^SpotifyEncryptionRecord.MAC_SIZE];

        Assert.Equal(testPacket.Command, command);
        Assert.Equal(testPacket.Data.ToArray(), data.ToArray());

        // Close the connection and clean up
        tcpClient.Close();
        testServer.Stop();
        var completed = channel.Writer.TryComplete();
        await testServerTask;
        await processMessagesTask;
    }
}