using Eum.Spotify.connectstate;

namespace Wavee.Spotify.Domain.Remote;

public readonly record struct SpotifyDevice(string Id, DeviceType Type, string Name, double? Volume, IReadOnlyDictionary<string, string> Metadata);