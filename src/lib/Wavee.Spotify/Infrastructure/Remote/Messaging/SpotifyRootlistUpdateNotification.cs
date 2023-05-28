using Eum.Spotify.playlist4;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Infrastructure.Remote.Messaging;

public readonly record struct SpotifyRootlistUpdateNotification(string Username);
public readonly record struct SpotifyPlaylistUpdateNotification(AudioId Id, Diff Delta);