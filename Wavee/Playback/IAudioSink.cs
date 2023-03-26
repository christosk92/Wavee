using Wavee.Playback.Packets;

namespace Wavee.Playback;

public interface IAudioSink
{
    void Write(IAudioPacket packet, AudioConverter converter);
    void Start();
    void Stop();
}

public abstract class AudioConverter
{
    public short[] F64ToS16(double[] samples)
    {
        const double scale = 32767.0;

        return samples
            .Select(sample => (short)Math.Round(sample * scale))
            .ToArray();
    }

    public float[] F64ToF32(double[] samples)
    {
        throw new NotImplementedException();
    }
}