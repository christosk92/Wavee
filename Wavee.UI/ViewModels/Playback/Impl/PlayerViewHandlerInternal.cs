using System.Threading.Channels;
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

    public ChannelReader<IPlayerViewModelEvent> Events { get; }
    protected ChannelWriter<IPlayerViewModelEvent> EventsWriter { get; }

    public virtual void Dispose()
    {
    }
}