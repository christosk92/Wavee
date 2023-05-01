using Wavee.Spotify.Crypto;

namespace Wavee.Spotify.Infrastructure.Traits;

internal interface TcpIO
{
    bool Connected { get; }
    int Timeout { get; }

    Unit SetTimeout(int timeout);

    /// <summary>
    /// Connect to a TCP server
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<Unit> Connect(string host, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write to a TCP server
    /// </summary>
    /// <param name="length"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<Unit> Write(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read from a TCP server
    /// </summary>
    /// <param name="length"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<Memory<byte>> Read(int length, CancellationToken cancellationToken = default);
}

/// <summary>
/// Type-class giving a struct the trait of supporting TCP IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
internal interface HasTCP<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the TCP synchronous effect environment
    /// </summary>
    /// <returns>TCP synchronous effect environment</returns>
    Eff<RT, TcpIO> TcpEff { get; }
}