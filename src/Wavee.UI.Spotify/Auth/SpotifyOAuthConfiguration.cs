using System.Threading.Tasks;
using Wavee.UI.Spotify.Auth.Storage;

namespace Wavee.UI.Spotify.Auth;

public sealed class SpotifyOAuthConfiguration
{
    public SpotifyOAuthConfiguration(OpenBrowserRequest openBrowserRequest, IOAuthStorage? storage = null)
    {
        OpenBrowserRequest = openBrowserRequest;
        Storage = storage ?? new FileOAuthStorage();
    }

    public OpenBrowserRequest OpenBrowserRequest { get; }
    public IOAuthStorage Storage { get; }
}

public interface IOAuthStorage
{
    ValueTask<byte[]> Get();
    ValueTask Store(byte[] data);   
}

public delegate ValueTask OpenBrowserRequest(string url);