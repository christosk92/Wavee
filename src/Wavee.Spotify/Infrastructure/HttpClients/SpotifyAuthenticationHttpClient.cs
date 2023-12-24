using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eum.Spotify;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Wavee.Spotify.Core;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;
using ClientInfo = Eum.Spotify.login5v3.ClientInfo;

namespace Wavee.Spotify.Infrastructure.HttpClients;

internal sealed partial class SpotifyAuthenticationClient : ISpotifyAuthenticationClient
{
    private readonly HttpClient _client;

    public SpotifyAuthenticationClient(HttpClient client)
    {
        _client = client;
    }

    const string redirectUri = "http://127.0.0.1:5001/login";


    public async Task<LoginCredentials> GetCredentialsFromOAuth(OAuthCallbackDelegate oAuthCallbackDelegate,
        CancellationToken cancellationToken)
    {
        // First open browser
        var (url, codeVerifier) = await OpenBrowser(oAuthCallbackDelegate);

        var code = MyRegex().Match(url).Groups[1].Value;

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var body = new Dictionary<string, string>
        {
            { "client_id", Constants.SpotifyClientId },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri },
            { "code_verifier", codeVerifier }
        };
        request.Content = new FormUrlEncodedContent(body);
        using var response = await _client.SendAsync(request, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var jsondoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var accessToken = jsondoc.RootElement.GetProperty("access_token").GetString();
        jsondoc.RootElement.GetProperty("expires_in").GetInt32();
        jsondoc.RootElement.GetProperty("refresh_token").GetString();
        jsondoc.RootElement.GetProperty("scope").GetString();
        jsondoc.RootElement.GetProperty("token_type").GetString();
        var finalUsername = jsondoc.RootElement.GetProperty("username").GetString();

        return new LoginCredentials
        {
            AuthData = ByteString.CopyFromUtf8(accessToken),
            Username = finalUsername,
            Typ = AuthenticationType.AuthenticationSpotifyToken
        };
    }

    public async Task<LoginResponse> GetCredentialsFromLoginV3(LoginCredentials credentials, string deviceId,
        CancellationToken cancellationToken)
    {
        var loginRequest = new LoginRequest
        {
            ClientInfo = new ClientInfo
            {
                ClientId = Constants.SpotifyClientId,
                DeviceId = deviceId,
            },
            StoredCredential = new StoredCredential
            {
                Username = credentials.Username,
                Data = credentials.AuthData
            }
        };
        var loginRequestBytes = loginRequest.ToByteArray();
        using var httpLoginRequest = new HttpRequestMessage(HttpMethod.Post, "https://login5.spotify.com/v3/login");
        httpLoginRequest.Headers.Add("User-Agent", "Spotify/122400756 Win32_x86_64/0 (PC laptop)");
        using var byteArrayContent = new ByteArrayContent(loginRequestBytes);
        byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        httpLoginRequest.Content = byteArrayContent;
        using var loginResponse = await _client.SendAsync(httpLoginRequest, cancellationToken);
        await using var loginStream = await loginResponse.Content.ReadAsStreamAsync(cancellationToken);
        var loginResponseFinal = LoginResponse.Parser.ParseFrom(loginStream);
        UserInfo = loginResponseFinal.UserInfo;
        return loginResponseFinal;
    }

    public UserInfo? UserInfo { get; private set; }

    private async Task<(string Url, string CodeVerifier)> OpenBrowser(OAuthCallbackDelegate oAuthCallbackDelegate)
    {
        //scope=playlist-modify ugc-image-upload user-follow-read user-read-email user-read-private app-remote-control streaming user-follow-modify user-modify-playback-state user-library-modify playlist-modify-public playlist-read user-read-birthdate user-top-read playlist-read-private playlist-read-collaborative user-modify-private playlist-modify-private user-modify user-library-read user-personalized user-read-play-history user-read-playback-state user-read-currently-playing user-read-recently-played user-read-playback-position
        const string scopes =
            "playlist-modify ugc-image-upload user-follow-read user-read-email user-read-private app-remote-control streaming user-follow-modify user-modify-playback-state user-library-modify playlist-modify-public playlist-read user-read-birthdate user-top-read playlist-read-private playlist-read-collaborative user-modify-private playlist-modify-private user-modify user-library-read user-personalized user-read-play-history user-read-playback-state user-read-currently-playing user-read-recently-played user-read-playback-position";
        const string codeChallengeMethod = "S256";
        const string utmSource = "spotify";
        const string utmMedium = "desktop-win32-store";
        const string responseType = "code";

        var flowctx = Guid.NewGuid().ToString();
        var codeVerifier = GenerateNonce();
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

        var red = await oAuthCallbackDelegate(url);
        return (red, codeVerifier);
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
    private static partial Regex MyRegex();
}