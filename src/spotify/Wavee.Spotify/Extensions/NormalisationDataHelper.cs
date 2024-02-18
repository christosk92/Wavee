using System.Buffers.Binary;
using Wavee.Spotify.Playback;

namespace Wavee.Spotify.Extensions;

/// <summary>
///     Spotify provides these as `f32`, but audio metadata can contain up to `f64`.
///     Also, this negates the need for casting during sample processing.
/// </summary>
/// <param name="TrackGainDb"></param>
/// <param name="TrackPeak"></param>
/// <param name="AlbumGainDb"></param>
/// <param name="AlbumPeak"></param>
public static class NormalisationDataHelper
{
    private const ulong SPOTIFY_NORMALIZATION_HEADER_START_OFFSET = 144;

    internal static NormalisationData? ParseFromOgg(ISpotifyDecryptedStream decryptedFile)
    {
        var newpos = decryptedFile.Seek((long)SPOTIFY_NORMALIZATION_HEADER_START_OFFSET, SeekOrigin.Begin);
        if (newpos != (long)SPOTIFY_NORMALIZATION_HEADER_START_OFFSET)
            return null;

        Span<byte> trackGainDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> trackPeakDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> albumGainDb_Bytes = stackalloc byte[sizeof(float)];
        Span<byte> albumPeakDb_Bytes = stackalloc byte[sizeof(float)];

        var read = decryptedFile.Read(trackGainDb_Bytes);
        var trackGainDb = BinaryPrimitives.ReadSingleLittleEndian(trackGainDb_Bytes);

        var i = decryptedFile.Read(trackPeakDb_Bytes);
        var read1 = decryptedFile.Read(albumGainDb_Bytes);
        var i1 = decryptedFile.Read(albumPeakDb_Bytes);

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

public readonly record struct NormalisationData
{
    public float TrackGainDb { get; init; }
    public float TrackPeak { get; init; }
    public float AlbumGainDb { get; init; }
    public float AlbumPeak { get; init; }
}