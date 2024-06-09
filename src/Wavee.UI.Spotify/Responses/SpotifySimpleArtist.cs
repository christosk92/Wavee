using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.Spotify.Responses;

public sealed class SpotifySimpleArtist : ISimpleArtist
{
    public SpotifySimpleArtist(string id, string name, UrlImage[] images)
    {
        Id = id;
        Name = name;
        Images = images;
    }

    public string Id { get; }
    public string Name { get; }
    public UrlImage[] Images { get; }
}