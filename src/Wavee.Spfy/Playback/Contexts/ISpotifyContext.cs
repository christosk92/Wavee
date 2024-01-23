using LanguageExt;
using Wavee.Contexting;

namespace Wavee.Spfy.Playback.Contexts;

public interface ISpotifyContext : IWaveePlayerContext
{
    string ContextUri { get; }
    string ContextUrl { get; }
    HashMap<string, string> ContextMetadata { get; }
}

internal readonly record struct SpotifyContextPage(LinkedList<SpotifyContextTrack> Tracks, uint Index);

internal readonly record struct SpotifyContextTrack(
    SpotifyId Gid,
    Option<string> Uid,
    int Index,
    HashMap<string, string> Metadata);