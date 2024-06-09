using System.Collections.Generic;
using System.Text.Json;

namespace Wavee.UI.Spotify.Playback;

internal sealed class SpotifyRequestHandler : ISpotifyRequestHandler
{
    public void HandleRequest(string identity, JsonElement root, Dictionary<string, string> messageHeaders)
    {
        throw new System.NotImplementedException();
    }
    
    public void Dispose()
    {
        // TODO release managed resources here
    }
}