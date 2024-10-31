using Wavee.Core.Decoders.VorbisDecoder.Format.Tags;

namespace Wavee.Core.Decoders.VorbisDecoder.Format;


/// <summary>
/// A `Cue` is a designated point of time within a media stream.
///
/// A `Cue` may be a mapping from either a source track, a chapter, cuesheet, or a timestamp
/// depending on the source media. A `Cue`'s duration is the difference between the `Cue`'s
/// timestamp and the next. Each `Cue` may contain an optional index of points relative to the `Cue`
/// that never exceed the timestamp of the next `Cue`. A `Cue` may also have associated `Tag`s.
/// </summary>
/// <param name="Index">
/// A unique index for the `Cue`.
/// </param>
/// <param name="StartTs">
/// The starting timestamp in number of frames from the start of the stream.
/// </param>
/// <param name="Tags">
/// A list of `Tag`s associated with the `Cue`.
/// </param>
/// <param name="Points">
/// A list of `CuePoints`s that are contained within this `Cue`. These points are children of
/// the `Cue` since the `Cue` itself is an implicit `CuePoint`.
/// </param>
public readonly record struct Cue(uint Index, ulong StartTs, List<string> Tags, List<CuePoint> Points);

/// <summary>
/// A `CuePoint` is a point, represented as a frame offset, within a `Cue`.
///
/// A `CuePoint` provides more precise indexing within a parent `Cue`. Additional `Tag`s may be
/// associated with a `CuePoint`.
/// </summary>
/// <param name="StartOffsetTs">
/// The offset of the first frame in the `CuePoint` relative to the start of the parent `Cue`.
/// </param>
/// <param name="Tags">
/// A list of `Tag`s associated with the `CuePoint`.
/// </param>
public readonly record struct CuePoint(ulong StartOffsetTs, List<Tag> Tags);