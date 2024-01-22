using System.Diagnostics;
using Eum.Spotify.context;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using Spotify.Metadata;
using Wavee.Contexting;
using Wavee.Spfy.Exceptions;
using Wavee.Spfy.Items;
using Wavee.Spfy.Playback.Contexts;
using Wavee.Spfy.Playback.Decrypt;
using Wavee.VorbisDecoder;
using static LanguageExt.Prelude;
using Context = Eum.Spotify.context.Context;

namespace Wavee.Spfy.Playback;

public readonly record struct SpotifyContextTrackKey(SpotifyContextTrackKeyType Type, string Value);

public enum SpotifyContextTrackKeyType
{
    Id,
    Provider,
    Uid,
    Index,
    PageIndex
}

public sealed class WaveeSpotifyPlaybackClient
{
    private readonly IHttpClient _httpClient;
    private readonly Func<ValueTask<string>> _tokenFactory;
    private readonly Func<SpotifyId, ByteString, ValueTask<Option<byte[]>>> _audiokeyFactory;
    private readonly ILogger _logger;
    private readonly WaveePlayer _waveePlayer;
    private readonly WaveeSpotifyMetadataClient _metadataClient;
    private readonly Guid _connectionId;

    internal WaveeSpotifyPlaybackClient(IHttpClient httpClient,
        Func<ValueTask<string>> tokenFactory,
        Func<SpotifyId, ByteString, ValueTask<Option<byte[]>>> audiokeyFactory,
        ILogger logger,
        Option<ICachingProvider> caching,
        WaveePlayer waveePlayer, WaveeSpotifyMetadataClient metadataClient, Guid connectionId)
    {
        _httpClient = httpClient;
        _tokenFactory = tokenFactory;
        _audiokeyFactory = audiokeyFactory;
        _logger = logger;
        _waveePlayer = waveePlayer;
        _metadataClient = metadataClient;
        _connectionId = connectionId;
    }

    public ValueTask Play(SpotifyId itemId, Option<int> startIndex)
    {
        var startIndexAsValueTask = startIndex.Map(x => new ValueTask<int>(x));
        if (itemId.Type is AudioItemType.Track or AudioItemType.PodcastEpisode)
        {
            // just play the track !
            var context = new SingularTrackContext(() => itemId.Type switch
            {
                AudioItemType.Track => CreateTrackSpotifyStream(itemId),
                AudioItemType.PodcastEpisode => CreateTrackEpisodeStream(itemId),
            });
            return _waveePlayer.Play(context);
        }
        else if (itemId.Type is AudioItemType.Playlist or AudioItemType.Album)
        {
            var playlistContext =
                new SpotifyPlaylistOrAlbumContext(_connectionId, itemId, startIndexAsValueTask, CreateSpotifyStream);
            return _waveePlayer.Play(playlistContext);
        }
        else if (itemId.Type is AudioItemType.Artist)
        {
            var artistContext =
                new SpotifyArtistContext(_connectionId, itemId, startIndexAsValueTask, CreateSpotifyStream);
            return _waveePlayer.Play(artistContext);
        }

        //What other types are there ?
        throw new NotSupportedException();
    }

    internal Task<WaveeStream> CreateSpotifyStream(SpotifyId id, CancellationToken cancellationToken)
    {
        return id.Type switch
        {
            AudioItemType.Track => CreateTrackSpotifyStream(id),
            AudioItemType.PodcastEpisode => CreateTrackEpisodeStream(id),
            _ => throw new SpotifyItemNotSupportedException(id)
        };
    }

    private async Task<WaveeStream> CreateTrackSpotifyStream(SpotifyId trackId)
    {
        var item = await _metadataClient.FetchTrack(trackId, true, CancellationToken.None);
        if (item is not SpotifySimpleTrack simpleTrack)
            throw new SpotifyItemNotSupportedException(trackId);

        var fileMaybe = simpleTrack.AudioFiles.Where(x =>
                x.Format is AudioFile.Types.Format.OggVorbis320 or AudioFile.Types.Format.OggVorbis160
                    or AudioFile.Types.Format.OggVorbis96)
            .OrderByDescending(x => x.Format)
            .HeadOrNone();
        if (fileMaybe.IsNone)
            throw new SpotifyItemNotSupportedException(trackId);
        var file = fileMaybe.ValueUnsafe();

        var token = await _tokenFactory();
        var audioKey = await _audiokeyFactory(trackId, ByteString.CopyFrom(file.FileId.Span));
        if (audioKey.IsNone)
        {
            //TODO: actually some tracks do not have audiokey !
            Debugger.Break();
            throw new NotImplementedException();
        }

        //storage-resolve
        var storageResolveResponse = await _httpClient.StorageResolve(file.FileIdBase16, token);
        var cdnUrl = storageResolveResponse.Cdnurl.First();
        var encryptedStream = await _httpClient.CreateEncryptedStream(cdnUrl);
        var decryptedStream = new SpotifyDecryptedStream(encryptedStream,
            audioKey.ValueUnsafe(),
            0xa7);

        var oggReader = new VorbisWaveReader(decryptedStream, false);
        return new WaveeStream(oggReader, simpleTrack);
    }

    private async Task<WaveeStream> CreateTrackEpisodeStream(SpotifyId trackId)
    {
        throw new NotImplementedException();
    }

    public async Task<Context> ResolveContext(string itemId)
    {
        var token = await _tokenFactory();
        var resp = await _httpClient.ResolveContext(itemId, token);
        return resp;
    }

    public async Task<ContextPage> ResolveContextRaw(string pageUrl, CancellationToken none)
    {
        var token = await _tokenFactory();
        var resp = await _httpClient.ResolveContextRaw(pageUrl, token);
        return resp;
    }
}