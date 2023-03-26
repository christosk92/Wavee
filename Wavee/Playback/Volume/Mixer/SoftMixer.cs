using Microsoft.Extensions.Logging;
using Wavee.Playback.Volume.Helper;
using Wavee.Playback.Volume.VolumeControl;

namespace Wavee.Playback.Volume.Mixer;

internal sealed class SoftMixer : IMixer
{
    private AtomicDouble volume;
    private IVolumeCtrl volumeCtrl;
    private readonly ILogger<SoftMixer> _logger;

    public SoftMixer(MixerConfig config, ILogger<SoftMixer> logger)
    {
        _logger = logger;
        volumeCtrl = config.VolumeCtrl;
        Console.WriteLine($"Mixing with softvol and volume control: {volumeCtrl}");

        volume = new AtomicDouble(0.5);
    }

    public ushort Volume()
    {
        double mappedVolume = volume.Load();
        return volumeCtrl.AsUnmapped(mappedVolume, _logger);
    }

    public void SetVolume(ushort volumeValue)
    {
        double mappedVolume = volumeCtrl.ToMapped(volumeValue, _logger);
        volume.Store(mappedVolume);
    }

    public IVolumeGetter GetSoftVolume()
    {
        return new SoftVolume(volume);
    }

    public static string NAME = "softvol";
}