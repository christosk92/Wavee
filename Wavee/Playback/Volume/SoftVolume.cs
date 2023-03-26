using Wavee.Playback.Volume.Helper;

namespace Wavee.Playback.Volume;

public class SoftVolume : IVolumeGetter
{
    private AtomicDouble volume;

    public SoftVolume(double val)
    {
        this.volume = new AtomicDouble(val);
    }
    internal SoftVolume(AtomicDouble volume)
    {
        this.volume = volume;
    }

    public double AttenuationFactor()
    {
        return volume.Load();
    }
}