using Eum.Spotify.context;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Player;
using Wavee.Spotify.Infrastructure.Mercury.Models;

namespace Wavee.Spotify.Infrastructure.Mercury;

public interface ISpotifyMercuryClient
{
    Task<string> GetAccessToken(CancellationToken ct = default);
    Task<SpotifyContext> ContextResolve(string contextUri, CancellationToken ct = default);
    Task<SpotifyContext> ContextResolveRaw(string pageUrl, CancellationToken ct = default);
    Task<Track> GetTrack(AudioId id, string country, CancellationToken ct = default);
    Task<Episode> GetEpisode(AudioId id, string country, CancellationToken ct = default);
    Task<string> Autoplay(string id, CancellationToken ct = default);
    Task<TrackOrEpisode> GetMetadata(AudioId id, string country, CancellationToken ct = default);
    Task<MercuryPacket> Get(string url, CancellationToken ct = default);
}

public readonly record struct SpotifyContext(string Url, HashMap<string, string> Metadata, Seq<ContextPage> Pages, HashMap<string, Seq<string>> Restrictions);
