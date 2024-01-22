using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Eum.Spotify;
using Google.Protobuf;

namespace Wavee.Spfy;

internal static partial class OAuth
{
    public static async Task<LoginCredentials> AuthenticateWithBrowser(OpenBrowser openBrowser, IHttpClient httpClient)
    {
        var url = CreateNewUrl(out var redirectUri, out var codeVerifier);
        var redirect = await openBrowser(url, s => MatchRegex().Match(s).Success);

        var code = MatchRegex().Match(redirect).Groups[1].Value;
        // POST https://accounts.spotify.com/api/token
        //Content-Type: application/x-www-form-urlencoded
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var body = new Dictionary<string, string>
        {
            { "client_id", Constants.SpotifyClientId },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri },
            { "code_verifier", codeVerifier }
        };
        var tokenResponse = await httpClient.SendLoginRequest(body, CancellationToken.None);

        return new LoginCredentials
        {
            AuthData = ByteString.CopyFromUtf8(tokenResponse.AccessToken),
            Username = tokenResponse.Username,
            Typ = AuthenticationType.AuthenticationSpotifyToken
        };
    }

    private static string CreateNewUrl(out string redirect, out string codeVerifier)
    {
        const string redirectUri = "http://127.0.0.1:5001/login";
        const string scopes =
            "playlist-modify ugc-image-upload user-follow-read user-read-email user-read-private app-remote-control streaming user-follow-modify user-modify-playback-state user-library-modify playlist-modify-public playlist-read user-read-birthdate user-top-read playlist-read-private playlist-read-collaborative user-modify-private playlist-modify-private user-modify user-library-read user-personalized user-read-play-history user-read-playback-state user-read-currently-playing user-read-recently-played user-read-playback-position";
        const string codeChallengeMethod = "S256";
        const string utmSource = "spotify";
        const string utmMedium = "desktop-win32-store";
        const string responseType = "code";

        redirect = redirectUri;
        var flowctx = Guid.NewGuid().ToString();
        codeVerifier = GenerateNonce();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        var query = new Dictionary<string, string>
        {
            { "utm_campaign", "organic" },
            { "scope", scopes },
            { "utm_medium", utmMedium },
            { "response_type", responseType },
            { "flow_ctx", flowctx },
            { "redirect_uri", redirectUri },
            { "code_challenge_method", codeChallengeMethod },
            { "client_id", Constants.SpotifyClientId },
            { "code_challenge", codeChallenge },
            { "utm_source", utmSource }
        };

        var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        foreach (var (key, value) in query)
        {
            queryString[key] = value;
        }

        var urlBuilder = new UriBuilder("https://accounts.spotify.com")
        {
            Path = "/en/oauth2/v2/auth",
            Query = queryString.ToString()
        };
        var url = urlBuilder.ToString();
        return url;
    }

    private static string GenerateNonce()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz123456789";
        var nonce = new char[128];
        for (int i = 0; i < nonce.Length; i++)
        {
            var numberToPick = RandomNumberGenerator.GetInt32(
                fromInclusive: 0,
                toExclusive: chars.Length
            );
            nonce[i] = chars[numberToPick];
        }

        return new string(nonce);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var b64Hash = Convert.ToBase64String(hash);
        var code = UrlSafeRegex().Replace(b64Hash, "-");
        code = UrlSafeRegex_2().Replace(code, "_");
        code = UrlSafeRegex_3().Replace(code, "");
        return code;
    }


    [GeneratedRegex("\\+")]
    private static partial Regex UrlSafeRegex();

    [GeneratedRegex("\\/")]
    private static partial Regex UrlSafeRegex_2();

    [GeneratedRegex("=+$")]
    private static partial Regex UrlSafeRegex_3();

    [GeneratedRegex("code=(.*)")]
    private static partial Regex MatchRegex();
}