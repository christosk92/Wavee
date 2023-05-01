using System.Net.Sockets;

namespace Wavee.Spotify.Infrastructure.Live;

internal readonly struct TcpIO : Infrastructure.Traits.TcpIO
{
    readonly TcpClient _client;

    public TcpIO(TcpClient client)
    {
        _client = client;
    }

    public bool Connected
    {
        get
        {
            try
            {
                return !(_client.GetStream().Socket.Poll(1, SelectMode.SelectRead) && _client.GetStream().Socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }

    public int Timeout => _client.ReceiveTimeout;

    public Unit SetTimeout(int timeout)
    {
        _client.ReceiveTimeout = timeout;
        _client.SendTimeout = timeout;
        return unit;
    }

    public async ValueTask<Unit> Connect(string host, int port, CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(host, port, cancellationToken);
        return unit;
    }

    public async ValueTask<Unit> Write(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        await _client.GetStream().WriteAsync(data, cancellationToken);
        return unit;
    }

    public async ValueTask<Memory<byte>> Read(int length, CancellationToken cancellationToken = default)
    {
        Memory<byte> buffer = new byte[length];
        await _client.GetStream().ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }
}