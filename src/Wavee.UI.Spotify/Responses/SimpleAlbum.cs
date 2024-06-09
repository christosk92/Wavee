using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.Spotify.Responses;

public sealed class SimpleAlbum : ISimpleAlbum
{
    public SimpleAlbum(string id, string name, 
        IContributor contributor, UrlImage[] images)
    {
        Id = id;
        Name = name;
        Contributor = contributor;
        Images = images;
        if (contributor is SpotifyArtistContributor artist)
        {
            artist.Album = this;
        }
    }
    
    public string Id { get; }
    public IContributor Contributor { get; }
    public string Name { get; }
    public UrlImage[] Images { get; }
}