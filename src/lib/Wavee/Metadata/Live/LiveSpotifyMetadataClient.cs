using Eum.Spotify;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Cache;
using Wavee.Id;
using Wavee.Infrastructure.Mercury;
using Wavee.Metadata.Artist;
using Wavee.Token.Live;

namespace Wavee.Metadata.Live;

internal readonly struct LiveSpotifyMetadataClient : ISpotifyMetadataClient
{
    private readonly ISpotifyCache _cache;
    private readonly Func<IGraphQLQuery, Task<HttpResponseMessage>> _query;
    private readonly Func<IMercuryClient> _mercuryFactory;
    private readonly ValueTask<string> _country;

    public LiveSpotifyMetadataClient(Func<IMercuryClient> mercuryFactory, Task<string> country,
        Func<IGraphQLQuery, Task<HttpResponseMessage>> query, ISpotifyCache cache)
    {
        _mercuryFactory = mercuryFactory;
        _query = query;
        _cache = cache;
        _country = new ValueTask<string>(country);
    }

    public async Task<Track> GetTrack(SpotifyId id, CancellationToken cancellationToken = default)
    {
        const string query = "hm://metadata/4/track/{0}?country={1}";
        var finalUri = string.Format(query, id.ToBase16(), await _country);

        var mercury = _mercuryFactory();
        var response = await mercury.Get(finalUri, cancellationToken);
        if (response.Header.StatusCode == 200)
        {
            return Track.Parser.ParseFrom(response.Payload.Span);
        }

        throw new MercuryException(response);
    }

    public ValueTask<ArtistOverview> GetArtistOverview(SpotifyId artistId, bool destroyCache, CancellationToken ct = default)
    {
        LiveSpotifyMetadataClient tmpThis = this;
        var result = tmpThis._cache
            .GetRawEntity(artistId.ToString())
            .Bind(f => destroyCache ? Option<ReadOnlyMemory<byte>>.None : Option<ReadOnlyMemory<byte>>.Some(f))
            .Match(
                Some: data =>
                {
                    var res =  new ValueTask<ArtistOverview>(ArtistOverview.ParseFrom(data));
                    return res;
                },
                None: () =>
                {
                    static async Task<ArtistOverview> Fetch(SpotifyId artistid, LiveSpotifyMetadataClient tmpthis)
                    {
                        var query = new QueryArtistOverviewQuery(artistid, false);
                        var response = await tmpthis._query(query);
                        if (response.IsSuccessStatusCode)
                        {
                            var stream = await response.Content.ReadAsByteArrayAsync();
                            var artistOverview = ArtistOverview.ParseFrom(stream);
                            tmpthis._cache.SaveRawEntity(artistid.ToString(), stream);
                            return artistOverview;
                        }

                        throw new MercuryException(new MercuryResponse(
                            Header: new Header
                            {
                                StatusCode = (int)response.StatusCode
                            }, ReadOnlyMemory<byte>.Empty
                        ));
                    }

                    return new ValueTask<ArtistOverview>(Fetch(artistId, tmpThis));
                }
            );

        return result;
    }
}