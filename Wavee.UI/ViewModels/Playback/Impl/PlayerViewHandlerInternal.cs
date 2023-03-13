using System.Threading.Channels;
using Wavee.UI.ViewModels.AudioItems;
using Wavee.UI.ViewModels.Playback.PlayerEvents;

namespace Wavee.UI.ViewModels.Playback.Impl;

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

    public virtual void Dispose()
    {
    }

    public abstract Task LoadTrackList(IPlayContext context);

    public abstract void Seek(double position);
}