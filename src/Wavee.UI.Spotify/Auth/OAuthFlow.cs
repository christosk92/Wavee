using System;
using System.Net;
using System.Threading.Tasks;

namespace Wavee.UI.Spotify.Auth;

internal sealed class OAuthFlow : IDisposable
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