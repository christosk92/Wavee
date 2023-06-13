using System.Net.Sockets;

namespace Wavee.Infrastructure.IO;

public static class TcpIO
{
    public static TcpClient CreateClient(string host, int port)
    {
        var client = new TcpClient();
        client.Connect(host, port);
        return client;
    }

    public static void Send(this NetworkStream stream, ReadOnlySpan<byte> data)
    {
        stream.Write(data);
    }

    public static ReadOnlySpan<byte> Receive(this NetworkStream client,
        int length)
    {
        Span<byte> buffer = new byte[length];
        client.ReadExactly(buffer);
        return buffer;
    }
}