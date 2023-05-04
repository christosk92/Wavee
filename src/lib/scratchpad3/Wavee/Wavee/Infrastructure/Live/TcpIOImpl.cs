using System.Net.Sockets;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Live;

internal sealed class TcpIOImpl : TcpIO
{
    public static readonly Traits.TcpIO Default =
        new TcpIOImpl();

    public async ValueTask<TcpClient> Connect(string host, ushort port, CancellationToken ct = default)
    {
        var client = new TcpClient();
        await client.ConnectAsync(host, port, ct);
        return client;
    }

    public async ValueTask<Unit> Write(NetworkStream stream, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        await stream.WriteAsync(data, ct);
        return unit;
    }

    public async ValueTask<Memory<byte>> ReadExactly(NetworkStream stream, int numberOfBytes,
        CancellationToken ct = default)
    {
        Memory<byte> buffer = new byte[numberOfBytes];
        await stream.ReadExactlyAsync(buffer, ct);
        return buffer;
    }

    public Unit SetTimeout(NetworkStream stream, int timeout)
    {
        stream.ReadTimeout = timeout;
        return unit;
    }
}