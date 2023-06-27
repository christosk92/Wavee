using System.Diagnostics;
using LanguageExt;
using Wavee.Id;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;

namespace Wavee.Metadata.Home;

public sealed class SpotifyCollectionItem : ISpotifyHomeItem
{
    public SpotifyId Id => new SpotifyId(0, AudioItemType.UserCollection, ServiceType.Spotify);
}
public sealed class SpotifyPlaylistHomeItem : ISpotifyHomeItem
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required Option<string> Description { get; init; }
    public required CoverImage[] Images { get; init; }
    public required string OwnerName { get; init; }
}

public sealed class SpotifyAlbumHomeItem : ISpotifyHomeItem
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required TrackArtist[] Artists { get; init; }
    public required CoverImage[] Images { get; init; }
}

public sealed class SpotifyPodcastEpisodeHomeItem : ISpotifyHomeItem
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required CoverImage[] Images { get; init; }
    public Option<string> Description { get; init; }
    public TimeSpan Duration { get; set; }
    public TimeSpan Position { get; set; }
    public bool Started { get; set; }
    public string PodcastName { get; set; }
    public required DateTimeOffset ReleaseDate { get; set; }
}

public sealed class SpotifyArtistHomeItem : ISpotifyHomeItem
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; }
    public required CoverImage[] Images { get; init; }
}