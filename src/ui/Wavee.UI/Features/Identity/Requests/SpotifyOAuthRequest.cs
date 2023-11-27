using Wavee.Spotify.Application.Authentication.Modules;

namespace Wavee.UI.Features.Identity.Requests;

public sealed class SpotifyOAuthRequest
{
    public required string Url { get; init; }
    public TaskCompletionSource<OpenBrowserResult> BrowserRequested { get; init; } = null!;
}