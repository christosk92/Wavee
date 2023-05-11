namespace Wavee.Spotify.Clients.Playback;

internal static class SpotifyPlaybackConstants
{
    internal const int ChunkSize = 2 * 2 * 128 * 1024;
    internal const ulong SPOTIFY_OGG_HEADER_END = 0xa7;
}