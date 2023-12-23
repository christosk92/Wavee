using System.Collections.Concurrent;
using System.Net.Sockets;
using Eum.Spotify;
using Wavee.Spotify.Core.Interfaces;
using Wavee.Spotify.Core.Interfaces.Connection;
using Wavee.Spotify.Core.Models.Connection;

namespace Wavee.Spotify.Infrastructure.Connection;

internal sealed class ActiveTcpConnection : IDisposable
{
    private readonly ISpotifyTcpClientFactory _clientFactory;
    private readonly BlockingCollection<SpotifyPackage> _sendQueue;
    private readonly IAuthenticationService _authenticationService;

    private Thread? _listenThread;
    private Thread? _sendThread;

    private IDisposable? _client;
    public ActiveTcpConnection(
        ISpotifyTcpClientFactory tcpClientFactory,
        IAuthenticationService authenticationService)
    {
        _clientFactory = tcpClientFactory;
        _authenticationService = authenticationService;
        _sendQueue = new();
    }

    public event EventHandler<Exception>? OnError;
    public APWelcome? ApWelcome { get; private set; }

    public async Task<APWelcome> ConnectAsync(string host, int port)
    {
        var client = _clientFactory.Create();
        _client = client;
        var credentials = await _authenticationService.GetCredentials();
        var apwelcome = await client.ConnectAsync(host, port, credentials.credentials, credentials.deviceId);

        void onError(Exception x)
        {
            OnError?.Invoke(this, x);
        }

        _listenThread = new Thread(() => ListenLoop(client, onError));
        _listenThread.Start();

        _sendThread = new Thread(() => SendLoop(client, _sendQueue));
        _sendThread.Start();
        ApWelcome = apwelcome;
        return apwelcome;
    }

    private static void ListenLoop(
        ISpotifyTcpClient stream,
        Action<Exception> onError)
    {
        try
        {
            int seq = 1;
            while (stream.Connected)
            {
                var message = stream.Receive(seq);
                // Process received message
                // Increment sequence for each iteration
                seq++;
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException)
        {
            // Handle exceptions and possibly trigger reconnection
            onError(ex);
        }
    }


    private static void SendLoop(
        ISpotifyTcpClient client,
        BlockingCollection<SpotifyPackage> send)
    {
        //We do not care about errors here, they will be handled by the listen loop
        int sequence = 1;
        while (client.Connected)
        {
            if (send.TryTake(out SpotifyPackage message, -1))
            {
                // Send message
                client.Send(new SpotifyRefPackage
                {
                    Type = message.Type,
                    Data = message.Data.Span
                }, sequence);
                // Increment sequence for each iteration
                sequence++;
            }
        }
    }

    public void Dispose()
    {
        _sendQueue.Dispose();
        _client?.Dispose();
    }
}