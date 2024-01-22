using LanguageExt;
using Wavee.Contexting;

namespace Wavee.Spfy.Playback.Contexts;

public interface ISpotifyContext : IWaveePlayerContext
{
    string ContextUri { get; }
    string ContextUrl { get; }
    HashMap<string, string> ContextMetadata { get; }
}