namespace Wavee.ContextResolve;

public interface IContextResolver
{
    Task<SpotifyContext> Resolve(string uri, CancellationToken cancellationToken);
    Task<SpotifyContext> ResolveRaw(string pageUrl, CancellationToken ct = default);
}