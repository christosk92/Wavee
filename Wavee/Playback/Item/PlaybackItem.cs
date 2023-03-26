using Wavee.Interfaces.Models;

namespace Wavee.Playback.Item;

public record 
    PlaybackItem
{
    private Uri Uri
    {
        get;
        init;
    }

    public PlaybackItem Copy()
    {
        return this with { };
    }

    public IPlayableItem Item
    {
        get;
        init;
    }
}