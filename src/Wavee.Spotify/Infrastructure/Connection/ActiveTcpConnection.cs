using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Eum.Spotify;
using Wavee.Spotify.Core.Models.Connection;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Connection;

namespace Wavee.Spotify.Infrastructure.Connection;

internal delegate bool IsCorrectPackage(SpotifyRefPackage pkg);

internal sealed class ActiveTcpConnection : IDisposable
{
    private readonly ISpotifyTcpClientFactory _clientFactory;
    private readonly BlockingCollection<SpotifyPackage> _sendQueue;
    private readonly List<SpotifyPackageReceiver> _waitingForResponse = new();
    private readonly IAuthenticationService _authenticationService;

    private Thread? _listenThread;
    private Thread? _sendThread;

    private IDisposable? _client;
    private readonly object _audioKeySeqLock = new();
    private uint _audioKeySeq = 0;


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

            foreach (var receiver in _waitingForResponse)
            {
                receiver.Task.TrySetException(x);
            }
        }

        CreatePingPongListener();

        _listenThread = new Thread(() => ListenLoop(client, _waitingForResponse, onError));
        _listenThread.Start();

        _sendThread = new Thread(() => SendLoop(client, _sendQueue));
        _sendThread.Start();
        ApWelcome = apwelcome;
        return apwelcome;
    }

    private void CreatePingPongListener()
    {
        static bool IsPingPong(SpotifyRefPackage pkg)
        {
            return pkg.Type is SpotifyPacketType.Ping;
        }
        static bool IsPongAck(SpotifyRefPackage pkg)
        {
            return pkg.Type is SpotifyPacketType.PongAck;
        }
        
        var pingtcs = new TaskCompletionSource<SpotifyPackage>();
        _waitingForResponse.Add(new SpotifyPackageReceiver(pingtcs, IsPingPong, true));
        Task.Factory.StartNew(async () =>
        {
            try
            {
                while (true)
                {
                    if (pingtcs is null)
                    {
                        // Create new one
                        pingtcs = new TaskCompletionSource<SpotifyPackage>();
                        _waitingForResponse.Add(new SpotifyPackageReceiver(pingtcs, IsPingPong, true));
                    }
                    else
                    {
                        // wait for it to complete
                        await pingtcs.Task;
                        //Add pong waiter
                        var pongtcs = new TaskCompletionSource<SpotifyPackage>();
                        _waitingForResponse.Add(new SpotifyPackageReceiver(pongtcs, IsPongAck, true));

                        // Send pong
                        _sendQueue.Add(new SpotifyPackage
                        {
                            Type = SpotifyPacketType.Pong,
                            Data = new byte[4]
                        });

                        // timeout pong after 5 seconds
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await pongtcs.Task;
                        
                        // Now do it again !
                        pingtcs = null;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        });
    }

    private static void ListenLoop(
        ISpotifyTcpClient stream,
        List<SpotifyPackageReceiver> waitingForResponse,
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
                foreach (var receiver in waitingForResponse)
                {
                    if (receiver.IsCorrectPackage(message))
                    {
                        receiver.Task.SetResult(new SpotifyPackage
                        {
                            Type = message.Type,
                            Data = message.Data.ToArray()
                        });
                        if (receiver.RemoveOnceDone)
                        {
                            waitingForResponse.Remove(receiver);
                        }

                        break;
                    }
                }

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

    public uint GetNextAudioKeySequence()
    {
        lock (_audioKeySeqLock)
        {
            var current = _audioKeySeq;
            _audioKeySeq++;
            return current;
        }
    }

    private sealed class SpotifyPackageReceiver
    {
        public SpotifyPackageReceiver(TaskCompletionSource<SpotifyPackage> task, IsCorrectPackage isCorrectPackage,
            bool removeOnceDone)
        {
            Task = task;
            IsCorrectPackage = isCorrectPackage;
            RemoveOnceDone = removeOnceDone;
        }

        public TaskCompletionSource<SpotifyPackage> Task { get; }
        public IsCorrectPackage IsCorrectPackage { get; }
        public bool RemoveOnceDone { get; }
    }

    private sealed class SendSpotifyPackage
    {
        public SendSpotifyPackage(SpotifyPackageReceiver response, SpotifyPackage package)
        {
            Response = response;
            Package = package;
        }

        public SpotifyPackageReceiver Response { get; }
        public SpotifyPackage Package { get; }
    }

    public Task<SpotifyPackage> Send(SpotifyPackage spotifyPackage, IsCorrectPackage isCorrectPackage)
    {
        var tcs = new TaskCompletionSource<SpotifyPackage>();
        var receiver = new SpotifyPackageReceiver(tcs, isCorrectPackage, true);
        _waitingForResponse.Add(receiver);
        _sendQueue.Add(spotifyPackage);
        return tcs.Task;
    }
}