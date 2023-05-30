namespace Wavee.UI.States.Spotify;

public static class SpotifyEndpoints
{
    public static class PublicApi
    {
        private const string Base = "https://api.spotify.com/v1";

        public const string GetMe = $"{Base}/me";
        public const string DesktopHome_20_10 = $"{Base}/views/desktop-home?limit=20&content_limit=10";
    }
}