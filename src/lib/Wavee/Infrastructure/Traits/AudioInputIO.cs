namespace Wavee.Infrastructure.Traits;

internal interface AudioInputIO
{
    bool CanReadRaw { get; }
    bool CanReadSamples { get; }
    IAsyncEnumerable<ReadOnlyMemory<double>> ReadSamples(Stream stream);
    IAsyncEnumerable<ReadOnlyMemory<byte>> ReadRaw(Stream stream, Ref<TimeSpan> position,
        Ref<bool> observable, int chunkSize);
}

/// <summary>
/// Type-class giving a struct the trait of supporting Audio IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
internal interface HasAudioInput<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the audio input synchronous effect environment
    /// </summary>
    /// <returns>Audio input synchronous effect environment</returns>
    Eff<RT, AudioInputIO> AudioInputEff { get; }
}