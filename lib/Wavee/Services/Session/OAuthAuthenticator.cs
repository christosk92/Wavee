using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eum.Spotify;
using Google.Protobuf;

namespace Wavee.Services.Session;

public static partial class OAuthAuthenticator
{
    public static async Task<LoginCredentials> AuthenticateAsync(HttpClient httpClient, string clientId, string scopes, CancellationToken cancellationToken)
    {
        var url = OAuthUtils.CreateNewUrl(clientId, scopes, out var redirect, out var codeVerifier);
        var uri = new Uri(redirect);
        using var flow = new OAuthFlow(uri);
        
        _ = Task.Run(async () => await flow!.StartListener(), cancellationToken);
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

        var token = await flow.TokenTask.WaitAsync(cancellationToken);
        var code = OAuthUtils.MatchRegex().Match(token).Groups[1].Value;
        var tokenResponse = await GetTokenAsync(httpClient, clientId, code, redirect, codeVerifier, cancellationToken);
        return tokenResponse;
    }

    private static async Task<LoginCredentials> GetTokenAsync(
        HttpClient httpClient,
        string clientId,
        string code,
        string redirect,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirect,
            ["code_verifier"] = codeVerifier
        };

        //https://accounts.spotify.com/api/token
        var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Content = new FormUrlEncodedContent(form)
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Failed to get token: {content}");
        }
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var accessToken = json.RootElement.GetProperty("access_token").GetString();
        var username = json.RootElement.GetProperty("username").GetString();
        // return new Credentials(
        //     Username: username,
        //     AuthType: AuthenticationType.AuthenticationSpotifyToken,
        //     AuthData: Encoding.UTF8.GetBytes(accessToken)
        // );
        return new LoginCredentials
        {
            Username = username,
            Typ = AuthenticationType.AuthenticationSpotifyToken,
            AuthData = ByteString.CopyFromUtf8(accessToken)
        };
    }

    public static partial class OAuthUtils
    {
        public static string CreateNewUrl(string clientId, string scopes, out string redirect, out string codeVerifier)
        {
            const string redirectUri = "http://127.0.0.1:5001/login";
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
                { "client_id", clientId },
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
        public static partial Regex MatchRegex();
    }

    private sealed class OAuthFlow : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly TaskCompletionSource<string> _token;

        public OAuthFlow(Uri listenTo)
        {
            _token = new TaskCompletionSource<string>();
            _listener = new HttpListener();

            var host = listenTo.ToString().Replace(listenTo.AbsolutePath, string.Empty) + "/";
            _listener.Prefixes.Add(host);
        }

        public Task<string> TokenTask => _token.Task;

        public async Task StartListener()
        {
            _listener.Start();
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                ProcessRequest(context);
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            context.Response.Redirect("https://open.spotify.com/desktop/auth/success");
            context.Response.Close();

            _token.TrySetResult(context.Request.Url!.ToString());
        }

        public void StopListener()
        {
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
            }
        }

        public void Dispose()
        {
            ((IDisposable)_listener)?.Dispose();
        }
    }
}