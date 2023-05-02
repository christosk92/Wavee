using System.Text.Json;

namespace Wavee.Spotify.Remote.Models;

internal readonly record struct SpotifyRequestMessage(Option<uint> MessageId, Option<string> SentByDeviceId,
    Option<JsonElement> Command, Option<string> Endpoint);