using Wavee.Core.Extensions;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Http;

public abstract class ApiClient
{
    protected ApiClient(IAPIConnector apiConnector)
    {
        Guard.NotNull(nameof(apiConnector), apiConnector);
        
        Api = apiConnector;
    }

    protected IAPIConnector Api { get; set; }
}