using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text;
using AesCtr;
using AesCtrBouncyCastle;
using AesCtrNative;
using Eum.Spotify.storage;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Core.Infrastructure.IO;
using Wavee.Core.Playback;
using Wavee.Core.Player;
using Wavee.Core.Player.PlaybackStates;
using Wavee.Spotify.Infrastructure.ApResolver;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Mercury.Token;
using Wavee.Spotify.Infrastructure.Playback.Cdn;
using Wavee.Spotify.Infrastructure.Playback.Key;
using Wavee.Spotify.Infrastructure.Playback.Streams;

namespace Wavee.Spotify.Infrastructure.Playback;

public sealed class SpotifyPlaybackClient
{
    private readonly TokenClient _tokenClient;
    private readonly MercuryClient _mercuryClient;
    private readonly AudioKeyClient _audioKeyClient;
    private readonly SpotifyPlaybackConfig _config;

    private readonly Ref<Option<string>> _country;
    private readonly Ref<HashMap<string, string>> _productInfo;

    public SpotifyPlaybackClient(TokenClient tokenClient, MercuryClient mercuryClient,
        AudioKeyClient audioKeyClient,
        SpotifyPlaybackConfig config,
        Ref<Option<string>> country,
        Ref<HashMap<string, string>> productInfo)
    {
        _tokenClient = tokenClient;
        _mercuryClient = mercuryClient;
        _config = config;
        _country = country;
        _productInfo = productInfo;
        _audioKeyClient = audioKeyClient;

        //listen to end of context
        WaveePlayer.StateChanged
            .Where(x => x.PlaybackState is PermanentEndOfContextPlaybackState)
            .Select(async c =>
            {
                //potential autoplay
                if (!config.Autoplay)
                {
                    return unit;
                }

                var autoplay = await _mercuryClient.AutoplayQuery(c.Context.ValueUnsafe().Id);
                await PlayContext(autoplay, None);
                return unit;
            })
            .Subscribe();
    }

    public async ValueTask PlayContext(string contextUri, Option<int> startFromIndexInContext)
    {
        var country = _country.Value.IfNone("US");
        var cdnUrl = _productInfo.Value.Find("image_url").IfNone("https://i.scdn.co/image/{file_id}");
        var context = await BuildContext(contextUri, country, cdnUrl);
        await WaveePlayer.PlayContext(context, startFromIndexInContext);
    }

    private async Task<WaveeContext> BuildContext(string contextUri, string country, string cdnUrl)
    {
        var initialContext = await _mercuryClient.ContextResolve(contextUri);
        var futureTracks = BuildFutureTracks(
            _audioKeyClient,
            initialContext,
            _mercuryClient,
            _tokenClient,
            _config,
            country,
            cdnUrl);

        return new WaveeContext(
            Id: contextUri,
            Name: initialContext.Metadata.Find("context_description").IfNone(contextUri.ToString()),
            FutureTracks: futureTracks,
            ShuffleProvider: None
        );
    }

    private static IEnumerable<FutureTrack> BuildFutureTracks(
        AudioKeyClient audioKeyClient,
        SpotifyContext context,
        MercuryClient mercuryClient,
        TokenClient tokenClient,
        SpotifyPlaybackConfig playbackConfig,
        string country, string cdnUrl)
    {
        foreach (var page in context.Pages)
        {
            //check if the page has tracks
            //if it does, yield return each track
            //if it doesn't, fetch the next page (if next page is set). if not go to the next page
            if (page.Tracks.Count > 0)
            {
                foreach (var track in page.Tracks)
                {
                    var id = AudioId.FromUri(track.Uri);
                    var uid = track.HasUid ? track.Uid : Option<string>.None;
                    var trackMetadata = track.Metadata.ToHashMap();
                    if (uid.IsSome)
                    {
                        trackMetadata = trackMetadata.Add("spotify_uid", uid.ValueUnsafe());
                    }

                    trackMetadata = trackMetadata.Add("country", country);
                    trackMetadata = trackMetadata.Add("cdnurl", cdnUrl);

                    yield return new FutureTrack(id,
                        Metadata: trackMetadata,
                        () => StreamFuture(audioKeyClient, mercuryClient, tokenClient, id,
                            trackMetadata,
                            playbackConfig));
                }
            }
            else
            {
                //fetch the page if page url is set
                //if not, go to the next page
                if (page.HasPageUrl)
                {
                    var pageUrl = page.PageUrl;
                    var pageResolve = mercuryClient.ContextResolveRaw(pageUrl).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    foreach (var track in BuildFutureTracks(audioKeyClient, pageResolve, mercuryClient, tokenClient,
                                 playbackConfig, country, cdnUrl))
                    {
                        yield return track;
                    }
                }
                else if (page.HasNextPageUrl)
                {
                    var pageUrl = page.NextPageUrl;
                    var pageResolve = mercuryClient.ContextResolveRaw(pageUrl).ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    foreach (var track in BuildFutureTracks(audioKeyClient, pageResolve, mercuryClient, tokenClient,
                                 playbackConfig, country, cdnUrl))
                    {
                        yield return track;
                    }
                }
            }
        }
    }

    private static async Task<IAudioStream> StreamFuture(
        AudioKeyClient audioKeyClient,
        MercuryClient mercuryClient,
        TokenClient tokenClient,
        AudioId id,
        HashMap<string, string> trackMetadata,
        SpotifyPlaybackConfig playbackConfig)
    {
        var sp = await ApResolve.GetSpClient(CancellationToken.None);
        var spUrl = $"https://{sp.host}:{sp.port}";

        var metadata = await mercuryClient.GetMetadata(id, trackMetadata.Find("country").IfNone("US"));
        var stream = await LoadTrack(
            spUrl,
            metadata,
            trackMetadata,
            playbackConfig.PreferredQualityType,
            playbackConfig.CrossfadeDuration,
            tokenClient,
            audioKeyClient,
            CancellationToken.None
        ).Run();
        var r = stream.ThrowIfFail();

        return r;
    }


    public static Aff<SpotifyStream> LoadTrack(string sp,
        TrackOrEpisode trackOrEpisode,
        HashMap<string, string> streamMetadata,
        PreferredQualityType preferredQuality,
        Option<TimeSpan> crossfadeDuration,
        TokenClient getBearer,
        AudioKeyClient audioKeyClient,
        CancellationToken ct) =>
        from audioFile in Eff(() => trackOrEpisode.FindFile(preferredQuality).Match(
            Some: x => x,
            None: () => trackOrEpisode.FindAlternativeFile(preferredQuality)
                .Match(
                    Some: f => f,
                    None: () => throw new Exception("No audio file found")
                )
        ))
        from audioKey in audioKeyClient.GetAudioKey(trackOrEpisode.Id, audioFile.FileId, ct).Map(x => x.Match(
            Left: err => None,
            Right: key => Some(key)
        )).ToAff()
        //TODO: Cache
        from stream in OpenHttpEncryptedStream(getBearer, sp, audioFile.FileId, ct)
        from decryptedStream in OpenDecryptedStream(stream, audioKey)
        from offsetAndNormData in ReadNormalisationData(decryptedStream, audioFile.Format)
        select new SpotifyStream(decryptedStream, trackOrEpisode,
            streamMetadata,
            offsetAndNormData.Item1,
            offsetAndNormData.Item2, stream.Length, crossfadeDuration.Map(x => new CrossfadeController(x)));

    private static Eff<(Option<NormalisationData> Normdata, long Offset)> ReadNormalisationData(Stream stream,
        AudioFile.Types.Format format)
    {
        var isOggVorbis = format == AudioFile.Types.Format.OggVorbis96 ||
                          format == AudioFile.Types.Format.OggVorbis160 ||
                          format == AudioFile.Types.Format.OggVorbis320;

        if (!isOggVorbis) return SuccessEff((Option<NormalisationData>.None, 0L));
        return Eff(() =>
        {
            var normData = NormalisationData.ParseFromOgg(stream);
            const ulong offset = SpotifyPlaybackConstants.SPOTIFY_OGG_HEADER_END;
            return (normData, (long)offset);
        });
    }

    private static Eff<Stream> OpenDecryptedStream(Stream stream, Option<AudioKey> audioKey) =>
        Eff(() =>
        {
            var aes128 = new AesCtrBouncyCastleStream(stream, audioKey.ValueUnsafe().Key.ToArray(),
                SpotifyPlaybackConstants.AUDIO_AES_IV, SpotifyPlaybackConstants.ChunkSize);
            return (Stream)aes128;
            // var aes128 = new Aes128CtrStream(stream, audioKey.ValueUnsafe().Key.ToArray(),
            //     SpotifyPlaybackConstants.AUDIO_AES_IV);
            // return (Stream)(new Aes128CtrWrapperStream(aes128));
        });

    private static Aff<HttpEncryptedSpotifyStream> OpenHttpEncryptedStream(
        TokenClient getBearer,
        string spClientUrl,
        ByteString fileId,
        CancellationToken ct) =>
        from base16Id in SuccessEff(ToBase16(fileId.Span))
        from bearer in getBearer.GetToken(ct).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from storage in HttpIO.GetAsync($"{spClientUrl}/storage-resolve/files/audio/interactive/{base16Id}", bearer,
                LanguageExt.HashMap<string, string>.Empty, ct).ToAff()
            .MapAsync(async x =>
            {
                await using var stream = await x.Content.ReadAsStreamAsync(ct);
                var r = StorageResolveResponse.Parser.ParseFrom(stream);
                x.Dispose();
                return r;
            })
        from cdnUrls in GetCdnUrls(fileId, storage)
        from firstChunkAndLength in GetFirstChunk(cdnUrls, SpotifyPlaybackConstants.ChunkSize, ct)
        select new HttpEncryptedSpotifyStream(cdnUrls, firstChunkAndLength.FirstChunk,
            firstChunkAndLength.TotalLength);


    private static Aff<(ReadOnlyMemory<byte> FirstChunk, long TotalLength)> GetFirstChunk(string url, int chunkSize,
        CancellationToken ct = default) =>
        HttpIO.GetWithContentRange(url, 0, chunkSize, ct)
            .MapAsync(async x =>
            {
                var length = x.Content.Headers.ContentRange?.Length ?? throw new Exception("No content length");
                ReadOnlyMemory<byte> firstChunk = await x.Content.ReadAsByteArrayAsync(ct);
                x.Dispose();
                return (firstChunk, length);
            }).ToAff();

    private static Eff<string> GetCdnUrls(ByteString fileId, StorageResolveResponse storage) => Eff(() =>
    {
        var maybeExpiring = MaybeExpiringUrl.From(storage);
        return new CdnUrl(fileId, maybeExpiring).Urls.First().Url;
    });

    private static string ToBase16(ReadOnlySpan<byte> raw)
    {
        //convert to hex
        var hex = new StringBuilder(raw.Length * 2);
        foreach (var b in raw)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }
}