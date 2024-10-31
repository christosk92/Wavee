namespace Wavee.Playback.Streaming;

public class NormalizationData
{
    public double TrackGainDb { get; set; }
    public double TrackPeak { get; set; }
    public double AlbumGainDb { get; set; }
    public double AlbumPeak { get; set; }

    public static NormalizationData Default => new NormalizationData
    {
        TrackGainDb = 0.0,
        TrackPeak = 1.0,
        AlbumGainDb = 0.0,
        AlbumPeak = 1.0
    };
}