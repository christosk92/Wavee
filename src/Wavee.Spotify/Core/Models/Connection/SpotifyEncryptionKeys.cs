namespace Wavee.Spotify.Core.Models.Connection;

public readonly struct SpotifyEncryptionKeys
{
    public readonly ReadOnlyMemory<byte> SendKey;
    public readonly ReadOnlyMemory<byte> ReceiveKey;
    public readonly ReadOnlyMemory<byte> Challenge;

    public SpotifyEncryptionKeys(ReadOnlyMemory<byte> sendKey, ReadOnlyMemory<byte> receiveKey,
        ReadOnlyMemory<byte> challenge)
    {
        SendKey = sendKey;
        ReceiveKey = receiveKey;
        Challenge = challenge;
    }
}