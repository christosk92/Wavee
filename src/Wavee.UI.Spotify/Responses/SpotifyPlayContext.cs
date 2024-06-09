using System;

namespace Wavee.UI.Spotify.Responses;

public readonly record struct SpotifyPlayContext(string Uri, DateTimeOffset PlayedAt, string TrackUri);