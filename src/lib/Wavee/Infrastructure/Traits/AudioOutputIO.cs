using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;

namespace Wavee.Infrastructure.Traits;

public interface AudioOutputIO
{
    Task<Unit> PutStream(Stream audioStream, bool closeOtherStreams);
    Unit Start();
    Unit Pause();
    Unit Seek(TimeSpan to);
    Option<TimeSpan> Position { get; }
    Unit Stop();
    Unit SetVolume(double volumeFrac);
    
    double Volume { get; }
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