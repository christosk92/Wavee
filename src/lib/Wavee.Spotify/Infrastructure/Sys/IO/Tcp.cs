using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Wavee.Spotify.Infrastructure.Traits;

namespace Wavee.Spotify.Infrastructure.Sys.IO;

internal static class Tcp<RT>
    where RT : struct, HasCancel<RT>, HasTCP<RT>
{
    /// <summary>
    /// Connect to a TCP server
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Unit> Connect(string host, ushort port) =>
        from ct in cancelToken<RT>()
        from _ in default(RT).TcpEff.MapAsync(e => e.Connect(host, port, ct))
        select unit;

    /// <summary>
    /// Write to a TCP server
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Unit> Write(ReadOnlyMemory<byte> packet) =>
        from ct in cancelToken<RT>()
        from _ in default(RT).TcpEff.MapAsync(e => e.Write(packet, ct))
        select unit;

    /// <summary>
    /// Read from a TCP server
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, Memory<byte>> Read(int length) =>
        from ct in cancelToken<RT>()
        from res in default(RT).TcpEff.MapAsync(e => e.Read(length, ct))
        select res;
}