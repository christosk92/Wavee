namespace Wavee.Spotify.Domain.Common;

public readonly record struct SpotifyImage(string Url, ushort? Width, ushort? Height)
{
    public string? ColorDark { get; init; }
}