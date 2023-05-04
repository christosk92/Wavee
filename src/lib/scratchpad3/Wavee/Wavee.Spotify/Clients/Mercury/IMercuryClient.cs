using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify.Clients.Mercury;

public interface IMercuryClient
{
    ValueTask<MercuryResponse> Send(MercuryMethod method, string uri, Option<string> contentType);
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
        //Setup a listener
        var listenerResult = SpotifyRuntime.GetChannelReader(_connectionId);
        var getWriter = SpotifyRuntime.GetSender(_connectionId);

        var response =
            await MercuryRuntime.Send(method, uri, contentType, _nextMercurySequence, getWriter, listenerResult);

        var run = SpotifyRuntime.RemoveListener(_connectionId, listenerResult);

        return response;
    }
}