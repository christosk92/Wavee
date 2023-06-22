namespace Wavee.Player;

public readonly record struct NormalisationData(double TrackGainDb,
    double TrackPeak,
    double AlbumGainDb,
    double AlbumPeak);