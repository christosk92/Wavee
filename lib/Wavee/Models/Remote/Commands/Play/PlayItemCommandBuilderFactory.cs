namespace Wavee.Models.Remote.Commands.Play;

/// <summary>
/// Static factory class for initiating <see cref="PlayItemCommand"/> building.
/// </summary>
public static class PlayItemCommandBuilderFactory
{
    /// <summary>
    /// Initiates a builder with an arbitrary context ID.
    /// </summary>
    /// <param name="contextId">The arbitrary context ID.</param>
    /// <returns>An instance of <see cref="DefaultPlayItemCommandBuilder"/>.</returns>
    public static DefaultPlayItemCommandBuilder From(string contextId)
    {
        return new DefaultPlayItemCommandBuilder(contextId);
    }

    /// <summary>
    /// Initiates a builder from a specific playlist.
    /// </summary>
    /// <param name="playlistId">The playlist ID.</param>
    /// <returns>An instance of <see cref="PlaylistPlayItemCommandBuilder"/>.</returns>
    public static PlaylistPlayItemCommandBuilder FromPlaylist(string playlistId)
    {
        return new PlaylistPlayItemCommandBuilder(playlistId);
    }

    /// <summary>
    /// Initiates a builder from liked songs.
    /// </summary>
    /// <returns>An instance of <see cref="LikedSongsPlayItemCommandBuilder"/>.</returns>
    public static LikedSongsPlayItemCommandBuilder FromLikedSongs(string userId)
    {
        return new LikedSongsPlayItemCommandBuilder(userId);
    }
}