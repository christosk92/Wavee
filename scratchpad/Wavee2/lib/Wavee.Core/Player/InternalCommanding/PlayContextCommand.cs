using LanguageExt;
using Wavee.Core.Playback;

namespace Wavee.Core.Player.InternalCommanding;

internal readonly record struct PlayContextCommand(WaveeContext Context, Option<int> StartFromIndexInContext) : IInternalPlayerCommand;