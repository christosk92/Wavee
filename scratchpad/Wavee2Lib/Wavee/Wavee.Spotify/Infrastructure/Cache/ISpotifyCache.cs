using Google.Protobuf;
using LanguageExt;
using Spotify.Metadata;

namespace Wavee.Spotify.Infrastructure.Cache;

public interface ISpotifyCache
{
    Option<Stream> AudioFile(AudioFile file);
    Unit SaveAudioFile(AudioFile file, byte[] data);
}