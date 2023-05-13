namespace Wavee.Core.Infrastructure.Traits;

public interface AudioOutputIO
{
    Unit Start();
    Unit Pause();
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
    Eff<RT, Option<AudioOutputIO>> AudioOutputEff { get; }
}