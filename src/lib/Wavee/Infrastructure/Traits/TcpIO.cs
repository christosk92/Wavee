using System.Net.Sockets;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;

namespace Wavee.Infrastructure.Traits;

public interface TcpIO
{
    ValueTask<TcpClient> Connect(string host, ushort post, CancellationToken ct = default);
    ValueTask<Unit> Write(NetworkStream stream, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    ValueTask<Memory<byte>> ReadExactly(NetworkStream stream, int numberOfBytes, CancellationToken ct = default);
    Unit SetTimeout(NetworkStream stream,int timeout);
}

/// <summary>
/// Type-class giving a struct the trait of supporting Tcp IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasTCP<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the tcp synchronous effect environment
    /// </summary>
    /// <returns>Tcp synchronous effect environment</returns>
    Eff<RT, TcpIO> TcpEff { get; }
}