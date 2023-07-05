using CommunityToolkit.Mvvm.Input;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.UI.Spotify.Clients;

internal static class SpotifyPlayCommands
{
    static SpotifyPlayCommands()
    {
        FromArtistTopTrack = new AsyncRelayCommand<IPlayParameter>(async id =>
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
        });
    }

    public static AsyncRelayCommand<IPlayParameter> FromArtistTopTrack { get; }
}