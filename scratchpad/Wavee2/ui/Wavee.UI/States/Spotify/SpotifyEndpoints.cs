namespace Wavee.UI.States.Spotify;

public static class SpotifyEndpoints
{
    public static class PublicApi
    {
        private const string Base = "https://api.spotify.com/v1";

        public static string GetMe => $"{Base}/me";
    }
}