using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.UI.Items;

namespace Eum.UI.Services.Artists
{
    public class ArtistProvider : IArtistProvider
    {
        private readonly ISpotifyClient _spotifyClient;

        public ArtistProvider(ISpotifyClient spotifyClient)
        {
            _spotifyClient = spotifyClient;
        }

        public async ValueTask<EumArtist> GetArtist(ItemId id, string locale, CancellationToken ct = default)
        {
            switch (id.Service)
            {
                case ServiceType.Local:
                    break;
                case ServiceType.Spotify:
                    var mercuryUrl = await _spotifyClient
                        .Artists.Mercury.GetArtistOverview(id.Id, locale, ct);

                    //TODO: caching
                    return new EumArtist(mercuryUrl);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return default;
        }
    }
}
