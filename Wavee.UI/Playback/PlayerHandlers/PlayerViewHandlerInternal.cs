using System.Threading.Channels;
using Wavee.Enums;
using Wavee.UI.Interfaces.Playback;

namespace Wavee.UI.Playback.PlayerHandlers;

internal abstract class PlayerViewHandlerInternal : IDisposable
{
    protected PlayerViewHandlerInternal()
    {
        var channels = Channel.CreateUnbounded<IPlayerViewModelEvent>();
        Events = channels.Reader;
        EventsWriter = channels.Writer;
    }

    public ChannelReader<IPlayerViewModelEvent> Events
    {
        get;
    }
    protected ChannelWriter<IPlayerViewModelEvent> EventsWriter
    {
        get;
    }

    public abstract TimeSpan Position
    {
        get;
    }
    public abstract bool Paused
    {
        get;
    }

    public abstract double Volume
    {
        get;
    }

    public virtual void Dispose()
    {
    }

    public abstract Task LoadTrackList(IPlayContext context, int index);
    public abstract Task LoadTrackListButDoNotPlay(IPlayContext context, int index);


    public abstract ValueTask Seek(double position);

    public abstract ValueTask Resume();
    public abstract ValueTask Pause();

    public abstract ValueTask SkipNext();
    public abstract ValueTask SkipPrevious();
    public abstract ValueTask ToggleShuffle();
    public abstract ValueTask GoShuffle(bool shuffle);
    public abstract ValueTask GoNextRepeatState();
    public abstract ValueTask GoToRepeatState(RepeatState repeatState);

    /// <summary>
    /// Set the volume (0 - 1)
    /// </summary>
    /// <param name="d"></param>
    /// <returns></returns>
    public abstract ValueTask SetVolume(double d);
}