using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.Spotify.Responses;

public sealed class SpotifyArtistContributor : IContributor
{
    public SpotifyArtistContributor(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }
    public UrlImage[] Images { get; }
    public string Color { get; set; }
    public ISimpleAlbum Album { get; set; }
}