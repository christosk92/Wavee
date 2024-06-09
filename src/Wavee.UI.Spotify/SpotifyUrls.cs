namespace Wavee.UI.Spotify;

internal static class SpotifyUrls
{
    public static class Partner
    {
        public const string BaseUrl = "https://api-partner.spotify.com";

        public const string Query = "/pathfinder/v1/query";

        public static class Home
        {
            public const string QueryName = "home";
            public const string Hash = "b86502f653085421347806bb5eeba86480c279e50ee20d79a83666d5e554b7cf";
        }
    }

    public static class Account
    {
        public const string BaseUrl = "https://accounts.spotify.com";

        public const string Token = "/api/token";
    }

    public static class Login
    {
        public const string BaseUrl = "https://login5.spotify.com";

        public const string V3 = "/v3/login";
    }
}