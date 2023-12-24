namespace Wavee.Vorbis;

/// <summary>
/// `FormatOptions` is a common set of options that all demuxers use. 
/// </summary>
/// <param name="PrebuildSeekIndex">
/// If a `FormatReader` requires a seek index, but the container does not provide one, build the
/// seek index during instantiation instead of building it progressively. Default: `false`.
/// </param>
/// <param name="SeekIndexFillRate">
/// If a seek index needs to be built, this value determines how often in seconds of decoded
/// content an entry is added to the index. Default: `20`.
///
/// Note: This is a CPU vs. memory trade-off. A high value will increase the amount of IO
/// required during a seek, whereas a low value will require more memory. The default chosen is
/// a good compromise for casual playback of music, podcasts, movies, etc. However, for
/// highly-interactive applications, this value should be decreased.
/// </param>
/// <param name="EnableGapless">
/// Enable support for gapless playback. Default: `false`.
///
/// When enabled, the reader will provide trim information in packets that may be used by
/// decoders to trim any encoder delay or padding.
///
/// When enabled, this option will also alter the value and interpretation of timestamps and
/// durations such that they are relative to the non-trimmed region.
/// </param>
public readonly record struct FormatOptions(bool PrebuildSeekIndex, ushort SeekIndexFillRate, bool EnableGapless)
{
    public static FormatOptions Default => new(false, 20, false);
}