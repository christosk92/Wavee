using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.Spotify.Interfaces.Api;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyEpisodeClient(ISpClient SpHttpClient) : IEpisodeClient
{
    public Task<IEpisode> GetEpisode(IItemId episodeId, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}