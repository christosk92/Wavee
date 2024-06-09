using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.UI.Spotify.Common;
using Wavee.UI.Spotify.Exceptions;
using Wavee.UI.Spotify.Interfaces.Api;
using Wavee.UI.Spotify.Responses.Parsers;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyTrackClient(ISpClient SpHttpClient) : ITrackClient
{
    private readonly Dictionary<string, ITrack> _cache = new();
    
    public async Task<ITrack> GetTrack(IItemId id, CancellationToken cancellationToken = default)
    {
        if (id is not RegularSpotifyId { Type: SpotifyIdItemType.Track } regularSpotifyId)
        {
            throw new SpotifyException(SpotifyFailureReason.InvalidId);
        }
        
        var base16Id = regularSpotifyId.ToBase16();
        if (_cache.TryGetValue(base16Id, out var cachedTrack))
        {
            return cachedTrack;
        }
        
        var originalTrack = await SpHttpClient.GetTrack(base16Id, cancellationToken);
        var result =  originalTrack.ToTrack();
        _cache[base16Id] = result;
        return result;
    }
}