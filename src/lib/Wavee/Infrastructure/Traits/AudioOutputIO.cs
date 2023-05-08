using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;

namespace Wavee.Infrastructure.Traits;

public interface AudioOutputIO
{
    ValueTask<Unit> Write(ReadOnlyMemory<byte> data);
    ValueTask<Unit> Write(ReadOnlyMemory<double> data);
    Unit Start();
    Unit Stop();
    Unit DiscardBuffer();
}

/// <summary>
/// Type-class giving a struct the trait of supporting Audio IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasAudioOutput<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the audio synchronous effect environment
    /// </summary>
    /// <returns>Audio synchronous effect environment</returns>
    Eff<RT, AudioOutputIO> AudioOutputEff { get; }
}