using LanguageExt;
using Wavee.Core.Id;

namespace Wavee.Player.States;

public interface IWaveePlaybackState
{
    Option<AudioId> TrackId { get; }
}