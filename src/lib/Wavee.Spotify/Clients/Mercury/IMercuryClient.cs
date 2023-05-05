using Spotify.Metadata;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify.Clients.Mercury;

public interface IMercuryClient
{
    ValueTask<MercuryResponse> Send(MercuryMethod method, string uri, Option<string> contentType);
    ValueTask<Track> GetTrack(string id, CancellationToken cancellationToken = default);
    ValueTask<Episode> GetEpisode(string id, CancellationToken cancellationToken = default);
}

internal readonly struct MercuryClientImpl : IMercuryClient
{
    private readonly Guid _connectionId;
    private readonly Ref<Option<ulong>> _nextMercurySequence;

    public MercuryClientImpl(Guid connectionId, Ref<Option<ulong>> nextMercurySequence)
    {
        _connectionId = connectionId;
        _nextMercurySequence = nextMercurySequence;
    }

    public async ValueTask<MercuryResponse> Send(MercuryMethod method, string uri, Option<string> contentType)
    {
        var listenerResult = SpotifyRuntime.GetChannelReader(_connectionId);
        var getWriter = SpotifyRuntime.GetSender(_connectionId);

        var response =
            await MercuryRuntime.Send(method, uri, contentType, _nextMercurySequence, getWriter, listenerResult);

        var run = SpotifyRuntime.RemoveListener(_connectionId, listenerResult);

        return response;
    }

    public async ValueTask<Track> GetTrack(string id, CancellationToken cancellationToken = default)
    {
        const string uri = "hm://metadata/4/track";
        
        var finalUri = $"{uri}/{id}";
        
        var response = await Send(MercuryMethod.Get, finalUri, Option<string>.None);
        return response.Header.StatusCode switch
        {
            200 => Track.Parser.ParseFrom(response.Body.Span),
            _ => throw new InvalidOperationException()
        };
    }

    public async ValueTask<Episode> GetEpisode(string id, CancellationToken cancellationToken = default)
    {
        const string uri = "hm://metadata/4/episode";
        
        var finalUri = $"{uri}/{id}";
        
        var response = await Send(MercuryMethod.Get, finalUri, Option<string>.None);
        return response.Header.StatusCode switch
        {
            200 => Episode.Parser.ParseFrom(response.Body.Span),
            _ => throw new InvalidOperationException()
        };
    }
}