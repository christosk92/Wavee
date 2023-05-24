using Eum.Spotify.context;
using Google.Protobuf.Collections;
using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.UI.ViewModels;

public readonly record struct PlayContextStruct(
    AudioId ContextId,
    int Index,
    Option<AudioId> TrackId,
    Option<string> ContextUrl,
    Option<IEnumerable<ContextPage>> NextPages,
    Option<int> PageIndex);