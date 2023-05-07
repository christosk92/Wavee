using System.Buffers.Binary;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Playback.Streams;
using Wavee.Spotify.Sys.Remote;

namespace Wavee.Spotify.Playback.Normalisation;

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
    private const ulong SPOTIFY_NORMALIZATION_HEADER_START_OFFSET = 144;

    public static double GetNormalisationFactor(SpotifyRemoteConfig config,
        NormalisationData? loadedTrackNormalisationData)
    {
        return 1;
    }

    internal static Option<NormalisationData> ParseFromOgg<RT>(DecryptedSpotifyStream<RT> decryptedFile) where RT : struct, HasHttp<RT>
    {
        var newpos = decryptedFile.Seek((long)SPOTIFY_NORMALIZATION_HEADER_START_OFFSET, SeekOrigin.Begin);
        if (newpos != (long)SPOTIFY_NORMALIZATION_HEADER_START_OFFSET)
            return Option<NormalisationData>.None;

        Span<byte> trackGainDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> trackPeakDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> albumGainDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> albumPeakDb_Bytes = stackalloc byte[sizeof(float)];

        decryptedFile.Read(trackGainDb_Bytes);
        decryptedFile.Read(trackPeakDb_Bytes);
        decryptedFile.Read(albumGainDb_Bytes);
        decryptedFile.Read(albumPeakDb_Bytes);

        var trackGainDb = BinaryPrimitives.ReadSingleLittleEndian(trackGainDb_Bytes);
        var trackPeakDb = BinaryPrimitives.ReadSingleLittleEndian(trackPeakDb_Bytes);
        var albumGainDb = BinaryPrimitives.ReadSingleLittleEndian(albumGainDb_Bytes);
        var albumPeakDb = BinaryPrimitives.ReadSingleLittleEndian(albumPeakDb_Bytes);

        return new NormalisationData
        {
            TrackPeak = trackPeakDb,
            TrackGainDb = trackGainDb,
            AlbumPeak = albumPeakDb,
            AlbumGainDb = albumGainDb
        };
    }
}