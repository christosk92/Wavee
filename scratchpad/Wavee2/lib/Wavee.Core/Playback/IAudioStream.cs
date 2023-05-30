using LanguageExt;
using Wavee.Core.Contracts;

namespace Wavee.Core.Playback;

public interface IAudioStream : IDisposable
{
    Stream AsStream();
    ITrack Track { get; }
    HashMap<string, string> Metadata { get; }
}