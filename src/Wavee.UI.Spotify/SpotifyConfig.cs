using System;
using Wavee.UI.Spotify.Auth;
using Wavee.UI.Spotify.Interfaces;

namespace Wavee.UI.Spotify;

public class SpotifyConfig
{
    public SpotifyConfig(SpotifyOAuthConfiguration oauth)
    {
        DeviceId = Guid.NewGuid().ToString("N");
        Auth = new SpotifyOAuthModule(oauth, DeviceId); 
    }
    public ISpotifyAuthModule Auth { get; }
    public string DeviceId { get; }
}