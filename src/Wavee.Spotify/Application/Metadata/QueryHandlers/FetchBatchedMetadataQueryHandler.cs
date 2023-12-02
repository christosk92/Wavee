using System.Collections.Immutable;
using System.IO.Compression;
using System.Net.Http.Headers;
using Eum.Spotify.connectstate;
using Eum.Spotify.extendedmetadata;
using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Application.Metadata.Query;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.LegacyAuth;
using Wavee.Spotify.Infrastructure.Persistent;
using Wavee.Spotify.Utils;

namespace Wavee.Spotify.Application.Metadata.QueryHandlers;

public sealed class FetchBatchedMetadataQueryHandler : IQueryHandler<FetchBatchedMetadataQuery, IReadOnlyDictionary<string, ByteString?>>
{
    private readonly HttpClient _httpClient;
    private readonly ISpotifyGenericRepository _genericRepository;

    public FetchBatchedMetadataQueryHandler(IHttpClientFactory httpClientFactory, ISpotifyGenericRepository genericRepository)
    {
        _genericRepository = genericRepository;
        _httpClient = httpClientFactory.CreateClient(Constants.SpotifyRemoteStateHttpClietn); ;
    }

    public async ValueTask<IReadOnlyDictionary<string, ByteString?>> Handle(FetchBatchedMetadataQuery query, CancellationToken cancellationToken)
    {
        var uris = query.Uris;
        var existingItems = _genericRepository.GetInBulk(uris);
        var missing = existingItems
            .Where(f => f.Value is null)
            .Select(x => x.Key)
            .ToImmutableArray();


        if (missing.Length is not 0)
        {
            const string url = "https://spclient.com/extended-metadata/v0/extended-metadata";
            var batchRequest = new BatchedEntityRequest
            {
                Header = new BatchedEntityRequestHeader
                {
                    Country = query.Country,
                    Catalogue = "premium"
                }
            };
            var kind = query.ItemsType switch
            {
                SpotifyItemType.Track => ExtensionKind.TrackV4,
                SpotifyItemType.Album => ExtensionKind.AlbumV4,
                SpotifyItemType.Artist => ExtensionKind.ArtistV4,
                SpotifyItemType.PodcastEpisode => ExtensionKind.EpisodeV4,
                SpotifyItemType.PodcastShow => ExtensionKind.ShowV4,
                _ => throw new ArgumentOutOfRangeException()
            };
            batchRequest.EntityRequest.AddRange(missing.Select(x => new EntityRequest
            {
                EntityUri = x,
                Query =
                {
                    new ExtensionQuery
                    {
                        ExtensionKind = kind
                    }
                }
            }));

            using var result = new MemoryStream();
            await using var compressionStream = new GZipStream(result, CompressionMode.Compress);
            var input = batchRequest.ToByteArray();
            await compressionStream.WriteAsync(input, 0, input.Length, cancellationToken);
            await compressionStream.FlushAsync(cancellationToken);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            using var byteArrayContent = new ByteArrayContent(result.ToArray());
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
            //Content-Encoding: gzip
            byteArrayContent.Headers.ContentEncoding.Add("gzip");
            httpRequest.Content = byteArrayContent;


            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            ReadOnlyMemory<byte> r = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            var data = BatchedExtensionResponse.Parser.ParseFrom(Gzip.UnsafeDecompressAlt(r.Span));
            var bytesData = data.ExtendedMetadata
                .SelectMany(f => f.ExtensionData.Select(x => (x.EntityUri, x.ExtensionData.Value)))
                .ToImmutableArray();
            foreach (var byteData in bytesData)
            {
                existingItems[byteData.EntityUri] = byteData.Value;
            }

            _genericRepository.SaveBulk(bytesData);
        }

        return existingItems;
    }
}