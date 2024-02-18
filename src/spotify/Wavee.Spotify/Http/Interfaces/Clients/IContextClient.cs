using Eum.Spotify.context;

namespace Wavee.Spotify.Http.Interfaces.Clients;

internal interface IContextClient
{
    Task<Context> ResolveContext(string contextUrl, CancellationToken cancellationToken);
    Task<Context> ResolveContextRaw(string contextUrl, CancellationToken cancellationToken);
}