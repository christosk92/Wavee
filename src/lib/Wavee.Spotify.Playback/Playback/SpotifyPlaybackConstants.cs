namespace Wavee.Spotify.Playback.Playback;

internal static class SpotifyPlaybackConstants
{
    internal const int ChunkSize = 2 * 2 * 128 * 1024;
    internal const ulong SPOTIFY_OGG_HEADER_END = 0xa7;
    
    public static byte[] AUDIO_AES_IV = new byte[]
    {
        0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93,
    };
}