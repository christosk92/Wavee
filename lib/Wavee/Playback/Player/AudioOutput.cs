namespace Wavee.Playback.Player;

public abstract class AudioOutput
{
    public abstract void Write(Span<float> samplesFloats);

    public abstract void Consume();

    public abstract void Clear();

    public abstract void Pause();

    public abstract void Resume();

    public abstract void Stop();
}