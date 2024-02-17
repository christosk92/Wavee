using System.Net;
using Eum.Spotify;
using Google.Protobuf;
using Wavee.Spotify.Authenticators;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Auth.OAuth;

internal sealed class OAuthFlow : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _url;
    private readonly TaskCompletionSource<string> _token;

    private OAuthFlow(Uri listenTo)
    {
        _token = new TaskCompletionSource<string>();
        _listener = new HttpListener();

        var host = listenTo.ToString().Replace(listenTo.AbsolutePath, string.Empty) + "/";
        _listener.Prefixes.Add(host);
    }

    public Task<string> TokenTask => _token.Task;

    private async Task StartListener()
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

    public static async Task<LoginCredentials> GetToken(IAPIConnector apiConnector,
        OpenBrowserRequest openBrowserRequest, CancellationToken cancel)
    {
        var newUrl = OAuthUtils.CreateNewUrl(out string redirect, out string codeVerifier);
        // Setup A HTTP Listener
        using var listener = new OAuthFlow(new Uri(redirect));
        try
        {
            await openBrowserRequest(newUrl);
            _ = Task.Run(async () => await listener!.StartListener(), CancellationToken.None);
            var result = await listener.TokenTask.WaitAsync(CancellationToken.None);
            if (!string.IsNullOrEmpty(result))
            {
                return await FinalStep(apiConnector, result, redirect, codeVerifier, cancel);
            }


            return null;
        }
        finally
        {
            try
            {
                listener.StopListener();
            }
            catch (Exception)
            {
            }
        }
    }

    private static async Task<LoginCredentials> FinalStep(IAPIConnector apiConnector, string result, string redirect,
        string codeVerifier, CancellationToken cancel)
    {
        var code = OAuthUtils.MatchRegex().Match(result).Groups[1].Value;

        var form = new List<KeyValuePair<string?, string?>>
        {
            new KeyValuePair<string?, string?>("client_id", Constants.SpotifyClientId),
            new KeyValuePair<string?, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string?, string?>("code", code),
            new KeyValuePair<string?, string?>("redirect_uri", redirect),
            new KeyValuePair<string?, string?>("code_verifier", codeVerifier),
        };

        var credentials = await SendOAuthRequest<AuthorizationCodeTokenResponse>(apiConnector, form, cancel);
        return new LoginCredentials
        {
            Username = credentials.Username,
            AuthData = ByteString.CopyFromUtf8(credentials.AccessToken),
            Typ = AuthenticationType.AuthenticationSpotifyToken
        };
    }

    private static Task<T> SendOAuthRequest<T>(
        IAPIConnector apiConnector,
        List<KeyValuePair<string?, string?>> form,
        CancellationToken cancel = default)
    {
        return apiConnector.Post<T>(new Uri(SpotifyUrls.Auth.Token), null, new FormUrlEncodedContent(form),
            RequestContentType.FormUrlEncoded,
            Empty,
            cancel);
    }

    private static Dictionary<string, string> Empty { get; } = new Dictionary<string, string>();

    public void Dispose()
    {
        ((IDisposable)_listener).Dispose();
    }
}