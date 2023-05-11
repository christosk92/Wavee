using LibVLCSharp.Shared;
using Wavee.Infrastructure.Traits;

namespace Wavee.Infrastructure.Live;

internal readonly struct VlcAudioOutputIO : Traits.AudioOutputIO
{
    private readonly VlcHolder _vlcHolder;

    public VlcAudioOutputIO(VlcHolder vlcHolder)
    {
        _vlcHolder = vlcHolder;
    }

    public Task<Unit> PutStream(Stream audioStream, bool closeOtherStreams)
    {
        var tcs = new TaskCompletionSource<Unit>();
        _vlcHolder.SetStream(audioStream, 
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
}