using Wavee.Playback.Decoder;
using Wavee.Playback.Normalisation;

namespace Wavee.Playback.Item;

public class PlayerLoadedTrackData
{
    public PlaybackItem AudioItem
    {
        get;
        init;
    }

    public IAudioDecoder? Decoder
    {
        get;
        init;
    }

    public NormalisationData? NormalisationData
    {
        get;
        init;
    }

    public int BytesPerSecond
    {
        get;
        init;
    }

    public double StreamPositionMs
    {
        get;
        init;
    }

    public bool IsExplicit
    {
        get;
        set;
    }

    public StreamLoaderController StreamLoaderController
    {
        get;
        set;
    }

    public double DurationMs
    {
        get;
        set;
    }
}

public class StreamLoaderController
{
    public void SetStreamMode()
    {
    }
}