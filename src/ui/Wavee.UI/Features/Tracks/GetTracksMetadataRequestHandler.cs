using Mediator;
using System;
using Spotify.Metadata;
using Wavee.Spotify.Application.Metadata.Query;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Infrastructure.LegacyAuth;
using System.Linq;

namespace Wavee.UI.Features.Tracks;

public sealed class GetTracksMetadataRequestHandler : IRequestHandler<GetTracksMetadataRequest, IReadOnlyDictionary<string, TrackOrEpisode?>>
{
    private readonly IMediator _mediator;
    private readonly SpotifyTcpHolder _tcpHolder;

    public GetTracksMetadataRequestHandler(IMediator mediator, SpotifyTcpHolder tcpHolder)
    {
        _mediator = mediator;
        _tcpHolder = tcpHolder;
    }

    public async ValueTask<IReadOnlyDictionary<string, TrackOrEpisode?>> Handle(GetTracksMetadataRequest request, CancellationToken cancellationToken)
    {
        var groups = request.Ids.Select(x => SpotifyId.FromUri(x)).GroupBy(x => x.Type)
            .Select(f => new
            {
                Key = f.Key,
                Items = f.Chunk(5000)
            });
        var output = new Dictionary<string, TrackOrEpisode?>();
        foreach (var id in request.Ids)
        {
            output[id] = null;
        }
        //Batch in 5000
        foreach (var group in groups)
        {
            foreach (var batch in group.Items)
            {
                var requestIds = batch.Select(f => f.ToString()).ToArray();
                var metadataRaw = await _mediator.Send(new FetchBatchedMetadataQuery
                {
                    AllowCache = true,
                    Uris = requestIds,
                    Country = _tcpHolder.Country,
                    ItemsType = group.Key
                }, cancellationToken);

                foreach (var requestId in requestIds)
                {
                    if (metadataRaw.TryGetValue(requestId, out var fetchedEntity) && fetchedEntity is not null)
                    {
                        var id = SpotifyId.FromUri(requestId);
                        switch (id.Type)
                        {
                            case SpotifyItemType.Track:
                                var parsed = Track.Parser.ParseFrom(fetchedEntity);
                                if (Match(parsed, request.SearchTerms))
                                {
                                    output[requestId] = new TrackOrEpisode(parsed, null);
                                }
                                break;
                            case SpotifyItemType.PodcastEpisode:
                                var parsedEpisode = Episode.Parser.ParseFrom(fetchedEntity);
                                if (Match(parsedEpisode, request.SearchTerms))
                                {
                                    output[requestId] =
                                        new TrackOrEpisode(null, parsedEpisode);
                                }
                                break;
                        }
                    }
                }
            }
        }

        return output;
    }

    private bool Match(Track track, IReadOnlyCollection<string> requestSearchTerms)
    {
        return requestSearchTerms.Any(f =>
            track.Name.Contains(f, StringComparison.InvariantCultureIgnoreCase)
            || track.Artist.Any(a => a.Name.Contains(f, StringComparison.InvariantCultureIgnoreCase)
                                     || track.Album.Name.Contains(f, StringComparison.InvariantCultureIgnoreCase)));
    }

    private bool Match(Episode parsedEpisode, IReadOnlyCollection<string> requestSearchTerms)
    {
        throw new NotImplementedException();
    }
}