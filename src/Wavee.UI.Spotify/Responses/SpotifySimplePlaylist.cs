using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.Spotify.Responses;

public class SpotifySimplePlaylist : ISimplePlaylist
{
    public SpotifySimplePlaylist(string id, string name, string description, UrlImage[] images)
    {
        Id = id;
        Name = name;
        Description = description;
        Images = images;
    }

    public string Id { get; }
    public string Description { get; }
    public string Name { get; }
    public UrlImage[] Images { get; }
}