using LanguageExt;
using Spotify.Metadata;

namespace Wavee.Spotify.Infrastructure.Cache;

internal readonly struct SpotifyCache : ISpotifyCache
{
    private readonly Option<string> _root;

    public SpotifyCache(Option<string> root)
    {
        _root = root;
    }

    public Option<Stream> AudioFile(AudioFile file)
    {
        //TODO:
        return Option<Stream>.None;
    }
}