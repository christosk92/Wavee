using System.Net.Sockets;

namespace Wavee.Spotify.Infrastructure.LegacyAuth.Functions;

/// <summary>
/// Contains methods for reading and writing to a TCP socket.
/// </summary>
internal static class TcpIO
{
    /// <summary>
    /// Creates a new <see cref="TcpClient"/> and connects to the specified host and port.
    /// </summary>
    /// <param name="host">
    ///  The DNS name of the remote host to which you intend to connect.
    /// </param>
    /// <param name="port">
    ///  The port number of the remote host to which you intend to connect.
    /// </param>
    /// <returns>
    ///  A new <see cref="TcpClient"/> that is connected to the specified host and port.
    /// </returns>
    public static TcpClient Connect(string host, ushort port)
    {
        return new TcpClient(host, port);
    }
}