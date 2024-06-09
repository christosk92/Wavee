using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Wavee.UI.Spotify.Playback;

internal interface ISpotifyRequestHandler : IDisposable
{
    void HandleRequest(string identity, JsonElement root, Dictionary<string,string> messageHeaders);
}