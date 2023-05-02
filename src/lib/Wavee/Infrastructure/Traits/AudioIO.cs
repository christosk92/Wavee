namespace Wavee.Infrastructure.Traits;

public interface AudioIO
{
    ValueTask<Unit> Write(ReadOnlyMemory<byte> data);
    ValueTask<Unit> Write(ReadOnlyMemory<double> data);
    Unit Start();
    Unit Stop();
}

/// <summary>
/// Type-class giving a struct the trait of supporting Audio IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasAudio<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the audio synchronous effect environment
    /// </summary>
    /// <returns>Audio synchronous effect environment</returns>
    Eff<RT, AudioIO> AudioEff { get; }
}