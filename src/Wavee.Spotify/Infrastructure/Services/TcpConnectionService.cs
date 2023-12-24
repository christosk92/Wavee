using System.Buffers.Binary;
using System.Runtime.Intrinsics.Arm;
using Eum.Spotify;
using Eum.Spotify.clienttoken.http.v0;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Connection;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients.Playback;
using Wavee.Spotify.Interfaces.Connection;

namespace Wavee.Spotify.Infrastructure.Services;

internal sealed class TcpConnectionService : ITcpConnectionService
{
    private ActiveTcpConnection? _connection;

    private readonly ISpotifyTcpClientFactory _tcpClientFactory;
    private readonly IAuthenticationService _authenticationService;
    private readonly IApResolverService _apResolverService;
    private readonly SemaphoreSlim _reconnectSemaphore;
    private readonly SemaphoreSlim _connectionSemaphore;

    private bool _dontReconnect;

    public TcpConnectionService(
        IApResolverService apResolverService,
        IAuthenticationService authenticationService,
        ISpotifyTcpClientFactory tcpClientFactory)
    {
        _apResolverService = apResolverService;
        _authenticationService = authenticationService;
        _tcpClientFactory = tcpClientFactory;

        _reconnectSemaphore = new SemaphoreSlim(1, 1);
        _connectionSemaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<APWelcome> ConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _connectionSemaphore.WaitAsync(cancellationToken);
            if (_connection?.ApWelcome is not null)
            {
                return await Task.FromResult(_connection.ApWelcome);
            }

            var (host, port) = await _apResolverService.GetAccessPoint(cancellationToken);
            _connection = new ActiveTcpConnection(_tcpClientFactory, _authenticationService);
            _connection.OnError += ConnectionOnOnError;

            return await _connection.ConnectAsync(host, port);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<SpotifyAudioKey> RequestAudioKeyAsync(SpotifyId itemId, string fileId,
        CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);
        
        var waiter = SendAudioRequest(itemId, fileId);
        await waiter.WaitAsync(cancellationToken);
        return await Task.FromResult(waiter.Result);
    }

    private Task<SpotifyAudioKey> SendAudioRequest(SpotifyId itemId, string fileId)
    {
        var raw = itemId.ToRaw();
        var seq = _connection!.GetNextAudioKeySequence();

        //base 16string to bytes
        Span<byte> fileMemory = stackalloc byte[fileId.Length / 2];
        for (int i = 0; i < fileMemory.Length; i++)
        {
            fileMemory[i] = Convert.ToByte(fileId.Substring(i * 2, 2), 16);
        }

        Memory<byte> data = new byte[raw.Length + fileMemory.Length + 2 + sizeof(uint)];

        fileMemory.CopyTo(data.Span);
        raw.CopyTo(data.Span.Slice(fileMemory.Length));
        BinaryPrimitives.WriteUInt32BigEndian(data.Span.Slice(fileMemory.Length + raw.Length), seq);
        BinaryPrimitives.WriteUInt16BigEndian(data.Span.Slice(fileMemory.Length + raw.Length + sizeof(uint)), 0x0000);

        var waiter = _connection.Send(new SpotifyPackage
        {
            Type = SpotifyPacketType.RequestKey,
            Data = data
        }, pkg => IsAudioPackage(pkg, seq));

        return waiter.ContinueWith(rawPkg =>
        {
            if (rawPkg.IsCompletedSuccessfully)
            {
                var res = rawPkg.Result;

                var key = res.Data.Slice(4, 16).ToArray();
                return new SpotifyAudioKey(key, true);
            }

            throw rawPkg.Exception?.InnerException ?? new Exception("Unknown error");
        });
    }

    private static bool IsAudioPackage(SpotifyRefPackage incoming, uint checkSeqAgainst)
    {
        if (incoming.Type is SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError)
        {
            var seq = BinaryPrimitives.ReadUInt32BigEndian(incoming.Data.Slice(0, 4));
            return seq == checkSeqAgainst;
        }

        return false;
    }

    public APWelcome? WelcomeMessage => _connection?.ApWelcome;

    private async void ConnectionOnOnError(object? sender, Exception e)
    {
        var instanceBefore = _connection;
        await _reconnectSemaphore.WaitAsync();
        try
        {
            if (_dontReconnect || _connection is null || _connection != instanceBefore)
            {
                return;
            }

            _connection?.Dispose();
            _connection = null;
            await ConnectAsync(CancellationToken.None);
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _reconnectSemaphore.Dispose();
    }
}