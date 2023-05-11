using LibVLCSharp.Shared;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Live;

internal readonly struct VlcAudioOutputIO : Traits.AudioOutputIO
{
    private static double _volume = 0.5f;
    private readonly VlcHolder _vlcHolder;

    public VlcAudioOutputIO(VlcHolder vlcHolder)
    {
        _vlcHolder = vlcHolder;
    }

    public Task<Unit> PutStream(Stream audioStream, bool closeOtherStreams)
    {
        var tcs = new TaskCompletionSource<Unit>();
        _vlcHolder.SetStream(audioStream, 
            _volume,
            closeOtherStreams,
            () => tcs.SetResult(unit));
        return tcs.Task;
    }

    public Unit Start()
    {
        _vlcHolder.Resume();
        return unit;
    }

    public Unit Pause()
    {
        _vlcHolder.Pause();
        return unit;
    }

    public Unit DiscardBuffer()
    {
        return unit;
    }

    public Unit Seek(TimeSpan pPosition)
    {
        _vlcHolder.Seek(pPosition);
        return unit;
    }

    public Option<TimeSpan> Position => _vlcHolder.Position;
    public Unit Stop()
    {
        _vlcHolder.Stop();
        return unit;
    }

    public Unit SetVolume(double volumeFrac)
    {
        _volume = volumeFrac;
        _vlcHolder.SetVolume(volumeFrac);
        return unit;
    }

    public double Volume => _volume;
}