using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.UI.Core.Contracts.Album;
using Wavee.UI.Core.Contracts.Artist;

namespace Wavee.UI.Core.Sys.Live;

internal sealed class SpotifyAlbumClient : IAlbumView
{
    private readonly SpotifyClient _client;

    public SpotifyAlbumClient(SpotifyClient client)
    {
        _client = client;
    }

    public Task<AlbumView> GetAlbumViewAsync(AudioId id, CancellationToken ct = default)
    {
        //fetching the mobile version also gives us the artists image
        const string fetch_uri = "hm://album/v1/album-app/album/{0}/android?format=json&catalogue=premium&locale={1}&country={2}";
        var country = _client.CountryCode.IfNone("US");
        var locale = "en";

        var uri = string.Format(fetch_uri, id.ToString(), locale, country);

        return _client.Mercury.Get(uri, ct)
            .Map(response =>
            {
                if (response.Header.StatusCode != 200)
                {
                    throw new MercuryException(response);
                }
                return AlbumView.From(response.Payload, id);
            });
    }
}