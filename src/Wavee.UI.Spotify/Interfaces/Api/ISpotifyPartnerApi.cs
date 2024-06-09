using System.Threading.Tasks;
using Refit;
using Wavee.UI.Spotify.Requests;

namespace Wavee.UI.Spotify.Interfaces.Api;

internal interface ISpotifyPartnerApi
{
    [Get(SpotifyUrls.Partner.Query)]
    Task<T> Query<T>(SpotifyQueryRequest<T> request);
}