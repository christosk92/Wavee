using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Sys.IO;

internal static class AudioInput<RT>
    where RT : struct, HasCancel<RT>, HasAudioInput<RT>
{
    /// <summary>
    /// Read raw (bytes) samples from a stream.
    /// </summary>
    /// <param name="stream">The stream to decode.</param>
    /// <param name="chunkSize">The number of bytes to decode at once.</param>
    /// <param name="position">
    ///  An observable that will be subscribed to, and will seek the decoder to the given position
    /// </param>
    /// <returns>
    /// An async enumerable of raw samples. Each sample is a segment in
    /// </returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<RT, IAsyncEnumerable<ReadOnlyMemory<byte>>> ReadRaw(Stream stream, int chunkSize,
        Ref<TimeSpan> position, Ref<bool> close) =>
        from result in default(RT).AudioInputEff.Map(e => e.ReadRaw(stream, position, close, chunkSize))
        select result;
}