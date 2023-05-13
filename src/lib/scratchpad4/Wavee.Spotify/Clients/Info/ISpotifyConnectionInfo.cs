namespace Wavee.Spotify.Clients.Info;

public interface ISpotifyConnectionInfo
{
    ValueTask<Option<string>> CountryCode { get; }
    ValueTask<Option<HashMap<string, string>>> ProductInfo { get; }
}