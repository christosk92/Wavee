using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using LanguageExt.Common;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Sys.IO;

/// <summary>
/// Tcp IO 
/// </summary>
public static class Tcp<RT>
    where RT : struct, HasTCP<RT>
{
    /// <summary>
    /// Connect to a remote host 
    /// </summary>
    /// <param name="host">
    /// The name of the remote host to connect to.
    /// </param>
    /// <param name="post">
    /// The port number of the remote host to connect to.
    /// </param>
    /// <typeparam name="RT">Runtime</typeparam>
    /// <returns>A new tcp client.</returns>
    [Pure, MethodImpl(AffOpt.mops)]
    public static Aff<RT, TcpClient> Connect(string host, ushort post) =>
        from ct in cancelToken<RT>()
        from tcp in default(RT).TcpEff.MapAsync(e => e.Connect(host, post, ct))
        select tcp;

    /// <summary>
    /// Write data to the remote host
    /// </summary>
    /// <param name="stream">The tcp stream to read from</param>
    /// <param name="data">
    /// The data to write to the remote host
    /// </param>
    /// <typeparam name="RT">Runtime</typeparam>
    /// <returns>Unit</returns>
    [Pure, MethodImpl(AffOpt.mops)]
    public static Aff<RT, Unit> Write(NetworkStream stream, ReadOnlyMemory<byte> data) =>
        from ct in cancelToken<RT>()
        from _ in default(RT).TcpEff.MapAsync(e => e.Write(stream, data, ct))
        select unit;

    /// <summary>
    /// Read the exact number of bytes from the remote host. This will block until the number of bytes are read.
    /// </summary>
    /// <param name="stream">The tcp stream to read from.</param>
    /// <param name="numberOfBytes">
    /// The number of bytes to read from the remote host.
    /// </param>
    /// <typeparam name="RT">Runtime</typeparam>
    /// <returns>
    /// A portion in memory of the data read from the remote host.
    /// </returns>
    [Pure, MethodImpl(AffOpt.mops)]
    public static Aff<RT, Memory<byte>> ReadExactly(NetworkStream stream, int numberOfBytes) =>
        from ct in cancelToken<RT>()
        from res in default(RT).TcpEff.MapAsync(e => e.ReadExactly(stream, numberOfBytes, ct))
        select res;

    [Pure, MethodImpl(AffOpt.mops)]
    public static Eff<RT, Unit> SetTimeout(NetworkStream stream, int timeout) =>
        default(RT).TcpEff.Map(e => e.SetTimeout(stream, timeout));
}