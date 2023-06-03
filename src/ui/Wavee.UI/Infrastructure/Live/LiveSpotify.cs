using Eum.Spotify;
using LanguageExt;
using System.Buffers.Binary;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Text.Json;
using Eum.Spotify.playlist4;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Cache;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Remote;
using System.Web;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Spotify.Collection.Proto.V2;
using Wavee.Core.Ids;
using Eum.Spotify.extendedmetadata;
using Spotify.Metadata;
using System.Text;
using Wavee.Infrastructure.IO;
using Wavee.Spotify.Infrastructure.ApResolve;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.Remote.Contracts;

namespace Wavee.UI.Infrastructure.Live;

internal sealed class LiveSpotify : Traits.SpotifyIO
{
    private Option<SpotifyClient> _connection = Option<SpotifyClient>.None;
    private readonly SpotifyConfig _config;

    public LiveSpotify(SpotifyConfig config)
    {
        _config = config;
    }

    public Aff<SelectedListContent> GetRootList(CancellationToken ct) =>
        from client in Eff(() => _connection.ValueUnsafe())
        from spclient in SuccessEff(ApResolver.SpClient.First())
            .Map(x => $"https://{x}")
            .Map(x =>
                $"{x}/playlist/v2/user/{client.WelcomeMessage.CanonicalUsername}/rootlist?decorate=revision,length,attributes,timestamp,owner")
        from bearer in client.Mercury.GetAccessToken(ct).ToAff().Map(x => new AuthenticationHeaderValue("Bearer", x))
        from result in HttpIO.Get(spclient, bearer, LanguageExt.HashMap<string, string>.Empty, ct)
            .ToAff().MapAsync(async r =>
            {
                await using var stream = await r.Content.ReadAsStreamAsync(ct);
                return SelectedListContent.Parser.ParseFrom(stream);
            })
        select result;

    public Aff<JsonDocument> FetchDesktopHome(string types, int limit, int offset,
        int contentLimit, int contentOffset,
        CancellationToken ct) =>
        from client in Eff(() => _connection.ValueUnsafe())
        let apiurl = $"https://api.spotify.com/v1/views/desktop-home?types={types}&offset={offset}&limit={limit}&content_limit={contentLimit}&content_offset={contentOffset}"
        from bearer in client.Mercury.GetAccessToken(ct).ToAff().Map(x => new AuthenticationHeaderValue("Bearer", x))
        from result in HttpIO.Get(apiurl, bearer, LanguageExt.HashMap<string, string>.Empty, ct)
            .ToAff().MapAsync(async r =>
            {
                await using var stream = await r.Content.ReadAsStreamAsync(ct);
                return await JsonDocument.ParseAsync(stream, default, ct);
            })
        select result;

    public Aff<T> GetFromPublicApi<T>(string endpoint, CancellationToken cancellation) =>
        from client in Eff(() => _connection.ValueUnsafe())
        let apiUrl = $"https://api.spotify.com/v1{endpoint}"
        from bearer in client.Mercury.GetAccessToken(cancellation).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from result in HttpIO.Get(apiUrl, bearer, LanguageExt.HashMap<string, string>.Empty, cancellation)
            .ToAff().MapAsync(async r =>
            {
                var result = await r.Content.ReadFromJsonAsync<T>(cancellationToken: cancellation);
                r.Dispose();
                return result;
            })
        select result;

    public Aff<Unit> AddToPlaylist(AudioId playlistId,
        string lastRevision,
        Seq<AudioId> audioIds, Option<int> position) =>
        from client in Eff(() => _connection.ValueUnsafe())
        from spclient in SuccessEff(ApResolver.SpClient.First())
            .Map(x => $"https://{x}")
            .Map(x =>
                $"{x}/playlist/v2/playlist/{playlistId.ToBase62()}/changes")
        from bearer in client.Mercury.GetAccessToken(CancellationToken.None).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from content in Eff(() =>
        {
            ReadOnlyMemory<byte> baseBytes = ReadOnlyMemory<byte>.Empty;

            var lst = new ListChanges
            {
                BaseRevision = ByteString.FromBase64(lastRevision),
            };
            var baseDelta = new Delta();
            baseDelta.Info = new Eum.Spotify.playlist4.ChangeInfo();
            baseDelta.Info.Source = new Eum.Spotify.playlist4.SourceInfo();
            baseDelta.Info.Source.Client = Eum.Spotify.playlist4.SourceInfo.Types.Client.Client;
            var baseOp = new Op();
            baseOp.Kind = Op.Types.Kind.Add;
            baseOp.Add = new Eum.Spotify.playlist4.Add();
            if (position.IsSome)
            {
                baseOp.Add.FromIndex = position.ValueUnsafe();
            }
            else
            {
                baseOp.Add.AddLast = true;
            }

            foreach (var item in audioIds)
            {
                var time = DateTimeOffset.UtcNow;
                //in milliseconds
                var now = (long)time.ToUnixTimeMilliseconds();
                baseOp.Add.Items.Add(new Item
                {
                    Attributes = new ItemAttributes
                    {
                        Timestamp = now,
                    },
                    Uri = item.ToString(),
                });
            }
            baseDelta.Ops.Add(baseOp);
            lst.Deltas.Add(baseDelta);
            baseBytes = lst.ToByteArray();
            //gzip
            var gzip = GzipHelpers.GzipCompress(baseBytes);
            return (HttpContent)gzip;
        })

        from posted in HttpIO.Post(spclient, bearer,
                LanguageExt.HashMap<string, string>.Empty,
                content, CancellationToken.None)
            .ToAff()
            .Map(x => x.EnsureSuccessStatusCode())
        select unit;

    public Aff<Unit> WriteLibrary(WriteRequest writeRequest, CancellationToken ct) =>
        //https://spclient.wg.spotify.com/collection/v2/write
        from client in Eff(() => _connection.ValueUnsafe())
        from spclient in SuccessEff(ApResolver.SpClient.First())
            .Map(x => $"https://{x}")
            .Map(x =>
                $"{x}/collection/v2/write")
        from bearer in client.Mercury.GetAccessToken(CancellationToken.None).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from content in Eff(() =>
        {
            var byteArrCnt = new ByteArrayContent(writeRequest.ToByteArray());
            byteArrCnt.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.collection-v2.spotify.proto");
            return byteArrCnt;
        })
        from posted in HttpIO.Post(spclient, bearer,
                LanguageExt.HashMap<string, string>.Empty,
                content, CancellationToken.None)
            .ToAff()
            .Map(x => x.EnsureSuccessStatusCode())
        select unit;

    public Aff<SelectedListContent> FetchPlaylist(AudioId playlistId) =>
        from client in Eff(() => _connection.ValueUnsafe())
        from spclient in SuccessEff(ApResolver.SpClient.First())
            .Map(x => $"https://{x}")
             .Map(x =>
                $"{x}/playlist/v2/playlist/{playlistId.ToBase62()}")
        from bearer in client.Mercury.GetAccessToken(CancellationToken.None).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from result in HttpIO
            .Get(spclient, bearer, LanguageExt.HashMap<string, string>.Empty, CancellationToken.None)
            .MapAsync(async x =>
            {
                x.EnsureSuccessStatusCode();
                await using var str = await x.Content.ReadAsStreamAsync();
                return SelectedListContent.Parser.ParseFrom(str);
            }).ToAff()
        select result;

    //https://spclient.wg.spotify.com/playlist/v2/playlist/4vDMxOIZDqN1TYXPQX9Wns/diff?revision=137%2C97279b18dcc66ece843bcd2a724c2549dbbe0ec5&handlesContent=
    public Aff<Diff> DiffRevision(AudioId playlistId, ByteString currentRevision) =>
        from client in Eff(() => _connection.ValueUnsafe())
        let revisionString = CalculateRevisionStr(currentRevision)
        from spclient in SuccessEff(ApResolver.SpClient.First())
            .Map(x => $"https://{x}")
            .Map(x =>
                $"{x}/playlist/v2/playlist/{playlistId.ToBase62()}/diff?revision={HttpUtility.UrlEncode(revisionString)}&handlesContent=")
        from bearer in client.Mercury.GetAccessToken(CancellationToken.None).ToAff()
            .Map(x => new AuthenticationHeaderValue("Bearer", x))
        from result in HttpIO
            .Get(spclient, bearer, LanguageExt.HashMap<string, string>.Empty, CancellationToken.None)
            .MapAsync(async x =>
            {
                x.EnsureSuccessStatusCode();
                await using var str = await x.Content.ReadAsStreamAsync();
                return SelectedListContent.Parser.ParseFrom(str).Diff;
            }).ToAff()
        select result;

    private static string CalculateRevisionStr(ByteString revision)
    {
        //for some reason, (yet unknown)
        //revisions in string form are like this:
        //converting it to plain string, gives for example:
        //0000008997279b18dcc66ece843bcd2a724c2549dbbe0ec5
        //but upon inspection , spotify actually sends this:
        //137,97279b18dcc66ece843bcd2a724c2549dbbe0ec5

        //if you look, yo ucan see the correct string like: 
        //97279b18dcc66ece843bcd2a724c2549dbbe0ec5
        //with some trailing numbers: 00000089
        //if you convert 00000089 to decimal, you get 137
        //so we need to remove the first 8 characters, and prepend it with the decimal value of the first 8 characters

        ReadOnlySpan<byte> str = revision.Span;
        //UINT16 - Big Endian (AB)
        var number = BinaryPrimitives.ReadUInt32BigEndian(str.Slice(0, 4));
        //var number = str[0];
        var rest = ToBase16(str.Slice(4));

        // ReadOnlySpan<char> str = revision.ToStringUtf8();
        // var number = Convert.ToInt32(str.Slice(0, 8).ToString(), 16);

        return $"{number},{rest}";

        static string ToBase16(ReadOnlySpan<byte> input)
        {
            var hex = new StringBuilder(input.Length * 2);
            foreach (byte b in input)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
    }

    public Aff<Seq<TrackOrEpisode>> FetchBatchOfTracks(Seq<AudioId> items, CancellationToken ct = default)
    {
        if (items.IsEmpty)
            return SuccessAff(LanguageExt.Seq<TrackOrEpisode>.Empty);
        return from client in Eff(() => _connection.ValueUnsafe())
            from spclient in SuccessEff(ApResolver.SpClient.First())
                .Map(x => $"https://{x}")
                   .Map(x =>
                       $"{x}/extended-metadata/v0/extended-metadata")
               from bearer in client.Mercury.GetAccessToken(CancellationToken.None).ToAff()
                   .Map(x => new AuthenticationHeaderValue("Bearer", x))
               from content in Eff(() =>
               {
                   var request = new BatchedEntityRequest();
                   request.EntityRequest.AddRange(items.Select(a => new EntityRequest
                   {
                       EntityUri = a.ToString(),
                       Query =
                       {
                        new ExtensionQuery
                        {
                            ExtensionKind = a.Type switch
                            {
                                AudioItemType.Track => ExtensionKind.TrackV4,
                                AudioItemType.PodcastEpisode => ExtensionKind.EpisodeV4,
                                _ => ExtensionKind.UnknownExtension
                            }
                        }
                       }
                   }));
                   request.Header = new BatchedEntityRequestHeader
                   {
                       Catalogue = "premium",
                       Country = client.CountryCode.ValueUnsafe()
                   };
                   var byteArrCnt = new ByteArrayContent(request.ToByteArray());
                   //byteArrCnt.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.collection-v2.spotify.proto");
                   return byteArrCnt;
               })
               from posted in HttpIO.Post(spclient, bearer,
                       LanguageExt.HashMap<string, string>.Empty, 
                       content, CancellationToken.None)
                   .ToAff()
                   .MapAsync(async x =>
                   {
                       x.EnsureSuccessStatusCode();
                       await using var stream = await x.Content.ReadAsStreamAsync(ct);
                       var response = BatchedExtensionResponse.Parser.ParseFrom(stream);
                       var allData = response
                           .ExtendedMetadata
                           .SelectMany(c =>
                           {
                               return c.ExtensionKind switch
                               {
                                   ExtensionKind.EpisodeV4 => c.ExtensionData
                                       .Select(e => new TrackOrEpisode(
                                           Either<Episode, Lazy<Track>>.Left(Episode.Parser.ParseFrom(e.ExtensionData.Value))
                                       )),
                                   ExtensionKind.TrackV4 => c.ExtensionData
                                       .Select(e => new TrackOrEpisode(
                                           Either<Episode, Lazy<Track>>.Right(new Lazy<Track>(() => Track.Parser.ParseFrom(e.ExtensionData.Value)))
                                       )),
                               };
                           });

                       return allData.ToSeq();
                   })
               select posted;
    }


    public async ValueTask<Unit> Authenticate(LoginCredentials credentials, CancellationToken ct = default)
    {
        var core = await SpotifyClient.CreateAsync(_config,credentials);
        _connection = Some(core);
        return Unit.Default;
    }

    public Option<APWelcome> WelcomeMessage()
    {
        var maybe = _connection.Map(x => x.WelcomeMessage);
        return maybe;
    }

    public Option<IObservable<SpotifyRootlistUpdateNotification>> ObserveRootlist()
    {
        return _connection
            .Map(x => x.Remote.RootlistChanged);
    }

    public Option<IObservable<SpotifyLibraryUpdateNotification>> ObserveLibrary()
    {
        return _connection
            .Map(x => x.Remote.LibraryChanged);
    }


    public Option<IObservable<SpotifyRemoteState>> ObserveRemoteState()
    {
        return _connection
            .Map(x => x.Remote.StateUpdates.Select(x => x.ValueUnsafe()));
    }
    public Option<IObservable<Diff>> ObservePlaylist(AudioId id) =>
        _connection
            .Map(x => x.Remote.ObservePlaylist(id));

    public Option<ISpotifyCache> Cache()
    {
        return _connection
            .Map(x => x.Cache);
    }

    public Option<string> CountryCode()
    {
        return _connection
            .Bind(x => x.CountryCode);
    }

    public ISpotifyMercuryClient Mercury()
    {
        return _connection
            .Map(x => x.Mercury)
            .IfNone(() => throw new InvalidOperationException("Mercury client not available"));
    }

    public Option<string> GetOwnDeviceId()
    {
        return _connection
            .Map(x => x.DeviceId);
    }

    public Option<ISpotifyRemoteClient> GetRemoteClient()
    {
        return _connection
            .Map(x => x.Remote);
    }
}