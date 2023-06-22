using Spotify.Metadata;
using Wavee.Id;
using Wavee.Infrastructure.Mercury;
using Wavee.Token.Live;

namespace Wavee.Metadata.Live;

internal readonly struct LiveSpotifyMetadataClient : ISpotifyMetadataClient
{
    private readonly Func<IMercuryClient> _mercuryFactory;
    private readonly ValueTask<string> _country;
    public LiveSpotifyMetadataClient(Func<IMercuryClient> mercuryFactory, Task<string> country)
    {
        _mercuryFactory = mercuryFactory;
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
}