namespace Wavee.Playback.Factories;

public interface IAudioFormat
{
    double[] ReadSamples(int count);

    int Channels
    {
        get;
    }

    TimeSpan CurrentTime
    {
        get;
    }

    int SampleRate
    {
        get;
    }

    TimeSpan TotalTime
    {
        get;
    }
    
    void Seek(TimeSpan position);
}