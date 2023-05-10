namespace Wavee.Spotify.Id;

public sealed class AudioIdException : Exception
{
    public AudioIdException(SpotifyIdError err, ReadOnlySpan<char> uri) : base(
        $"Error while parsing Spotify ID: {uri.ToString() ?? "empty"} : " +
        err switch
        {
            SpotifyIdError.InvalidId =>
                "ID cannot be parsed",
            SpotifyIdError.InvalidFormat =>
                "not a valid Spotify URI",
            SpotifyIdError.InvalidRoot =>
                "URI does not belong to Spotify",
            _ => throw new ArgumentOutOfRangeException(
                nameof(err), err, null)
        })
    {
        Error = err;
        Uri = uri.ToString();
    }

    public string? Uri { get; }
    public SpotifyIdError Error { get; }
}