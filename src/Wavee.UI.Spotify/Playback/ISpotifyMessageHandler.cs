using System;
using System.Collections.Generic;
using System.Text.Json;
using Eum.Spotify.connectstate;

namespace Wavee.UI.Spotify.Playback;

internal interface ISpotifyMessageHandler : IDisposable
{
    IObservable<Cluster> Cluster { get; }
    void HandleUri(string uri, JsonElement root, Dictionary<string,string> messageHeaders);
}