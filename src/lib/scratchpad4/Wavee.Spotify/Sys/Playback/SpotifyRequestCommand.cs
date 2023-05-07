namespace Wavee.Spotify.Sys.Playback;

internal readonly record struct SpotifyRequestCommand(
    int MessageId,
    string SentBy,
    SpotifyRequestCommandType Endpoint,
    ReadOnlyMemory<byte> Data);

internal enum SpotifyRequestCommandType
{
    Transfer,
}