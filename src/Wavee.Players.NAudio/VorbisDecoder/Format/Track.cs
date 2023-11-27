using LanguageExt;

namespace Wavee.Vorbis.Format;

/// <summary>
/// A `Track` is an independently coded media bitstream. A media format may contain multiple tracks
/// in one container. Each of those tracks are represented by one `Track`.
/// </summary>
/// <param name="Id">
/// A unique identifier for the track.
/// </param>
/// <param name="CodecParams">
/// The codec parameters for the track.
/// </param>
/// <param name="Language">
/// The language of the track. May be unknown.
/// </param>
public readonly record struct Track(uint Id, CodecParameters CodecParams, Option<string> Language);