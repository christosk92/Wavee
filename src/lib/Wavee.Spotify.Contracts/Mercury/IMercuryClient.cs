using LanguageExt;
using Spotify.Metadata;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Contracts.Mercury.Search;

namespace Wavee.Spotify.Contracts.Mercury;

public interface IMercuryClient
{
    ValueTask<MercuryResponse> Send(MercuryMethod method, string uri, Option<string> contentType);

    ValueTask<SearchResponse> Search(string query,
        string types,
        int offset = 0,
        int limit = 10,
        CancellationToken ct = default);

    ValueTask<Track> GetTrack(string id, CancellationToken cancellationToken = default);
    ValueTask<Episode> GetEpisode(string id, CancellationToken cancellationToken = default);

    ValueTask<string> FetchBearer(CancellationToken ct = default);
}