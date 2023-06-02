using Eum.Spotify.context;
using LanguageExt;
using Wavee.Player;

namespace Wavee.Spotify.Infrastructure.Mercury;

public interface ISpotifyMercuryClient
{
    Task<string> GetAccessToken(CancellationToken ct = default);
    Task<SpotifyContext> ContextResolve(string contextUri, CancellationToken ct = default);
    Task<SpotifyContext> ContextResolveRaw(string pageUrl, CancellationToken ct = default);
}

public readonly record struct SpotifyContext(string Url, HashMap<string, string> Metadata, Seq<ContextPage> Pages, HashMap<string, Seq<string>> Restrictions);
