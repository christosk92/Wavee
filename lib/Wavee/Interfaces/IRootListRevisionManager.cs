using Eum.Spotify.playlist4;

namespace Wavee.Interfaces;

/// <summary>
/// Interface for handling root list revisions.
/// </summary>
public interface IRootListRevisionManager
{
    Task ProcessRevisionAsync(PlaylistModificationInfo modificationInfo, CancellationToken cancellationToken = default);
}