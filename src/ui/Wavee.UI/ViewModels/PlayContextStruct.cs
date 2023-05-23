using Eum.Spotify.context;
using Google.Protobuf.Collections;
using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.UI.ViewModels;

public readonly record struct PlayContextStruct(
    AudioId ContextId, 
    int Index,
    RepeatedField<ContextPage> NextPages,
    int PageIndex);