using System.Buffers.Binary;
using CommunityToolkit.HighPerformance;
using LanguageExt;

namespace Wavee.Infrastructure.Playback;

/// <summary>
///     Spotify provides these as `f32`, but audio metadata can contain up to `f64`.
///     Also, this negates the need for casting during sample processing.
/// </summary>
/// <param name="TrackGainDb"></param>
/// <param name="TrackPeak"></param>
/// <param name="AlbumGainDb"></param>
/// <param name="AlbumPeak"></param>
public readonly record struct NormalisationData(
    double TrackGainDb,
    double TrackPeak,
    double AlbumGainDb,
    double AlbumPeak
)
{
    public Wavee.Player.NormalisationData ToUniversal()
    {
        return new Wavee.Player.NormalisationData(
            this.TrackGainDb,
            this.TrackPeak,
            this.AlbumGainDb,
            this.AlbumPeak
        );
    }
    
    private const ulong SPOTIFY_NORMALIZATION_HEADER_START_OFFSET = 144;

    public static double GetNormalisationFactor(
        NormalisationData? loadedTrackNormalisationData)
    {
        return 1;
    }

    internal static Option<NormalisationData> ParseFromOgg(SpotifyUnoffsettedStream decryptedFile)
    {
        var newpos = decryptedFile.Seek((long)SPOTIFY_NORMALIZATION_HEADER_START_OFFSET, SeekOrigin.Begin);
        if (newpos != (long)SPOTIFY_NORMALIZATION_HEADER_START_OFFSET)
            return Option<NormalisationData>.None;

        Span<byte> trackGainDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> trackPeakDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> albumGainDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> albumPeakDb_Bytes = stackalloc byte[sizeof(float)];

        var read = decryptedFile.Read(trackGainDb_Bytes);
        var trackGainDb = trackGainDb_Bytes.ReadSingleLittleEndian();
        
        var i = decryptedFile.Read(trackPeakDb_Bytes);
        var read1 = decryptedFile.Read(albumGainDb_Bytes);
        var i1 = decryptedFile.Read(albumPeakDb_Bytes);

        var trackPeakDb = trackPeakDb_Bytes.ReadSingleLittleEndian();
        var albumGainDb = albumGainDb_Bytes.ReadSingleLittleEndian();
        var albumPeakDb = albumPeakDb_Bytes.ReadSingleLittleEndian();

        return new NormalisationData
        {
            TrackPeak = trackPeakDb,
            TrackGainDb = trackGainDb,
            AlbumPeak = albumPeakDb,
            AlbumGainDb = albumGainDb
        };
    }
}