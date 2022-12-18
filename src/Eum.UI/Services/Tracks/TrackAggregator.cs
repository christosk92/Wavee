using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using DynamicData;
using Eum.Artwork;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Helpers;
using Eum.Connections.Spotify.Models.Users;
using Eum.Enums;
using Eum.Logging;
using Eum.Spotify.extendedmetadata;
using Eum.Spotify.metadata;
using Eum.UI.Items;
using Flurl;
using Flurl.Http;
using Google.Protobuf;
using LiteDB;
using MoreLinq;
using Nito.AsyncEx;

namespace Eum.UI.Services.Tracks
{
    public class TrackAggregator : ITrackAggregator
    {
        private readonly ILiteDatabase _database;
        private readonly ILiteCollection<CachedPlayItem> _tracks;
        public TrackAggregator(ILiteDatabase database)
        {
            _database = database;
            _tracks = _database.GetCollection<CachedPlayItem>("PlayItems");
        }
        public async Task<IEnumerable<EumTrack>> GetTracks(ItemId[] ids, CancellationToken ct = default)
        {
            var uris = ids.Select(a => a.Uri).Distinct().ToArray();

            var results = _tracks.Query()
                .Where(x => uris.Contains(x.Id))
                //.OrderBy(x => Array.FindIndex(uris, a => a == x.Id))
                //.Select(x => new EumTrack(x))
                //  .Limit(10)
                .ToArray();

            
            var didNotFoundItems = uris.ExceptBy(results.Select(a => a.Id),
                a => a)
                .Select(a => new ItemId(a));
            var newTracks = await FetchNewItems(didNotFoundItems, ct);

            var cachedPlayItems = (newTracks as CachedPlayItem[] ?? newTracks.ToArray())
                .Where(a => a != null);
            _tracks.Upsert(cachedPlayItems);
            static int[] FindAllIndexof<T>(T[] values, Func<T, bool> val)
            {
                return values.Select((b, i) => val(b) ? i : -1).Where(i => i != -1).ToArray();
            }
            var matches = new Dictionary<string, int>();
            var data = cachedPlayItems.Concat(results)
                .OrderBy(a =>
                {

                    int selectIndex = -1;
                    var find = FindAllIndexof(ids, id => id.Uri == a.Id);
                    if (find.Length > 1)
                    {
                        if (matches.ContainsKey(a.Id))
                        {
                            selectIndex = find[matches[a.Id]];
                            matches[a.Id] += 1;
                        }
                        else
                        {
                            matches[a.Id] = 1;
                            selectIndex = find[0];
                        }
                    }
                    else
                    {
                        selectIndex = find[0];
                    }

                    return selectIndex;
                })
                .Select(a => new EumTrack(a));

            return data;
        }

        public async ValueTask<EumTrack> GetTrack(ItemId itemId, CancellationToken ct = default)
        {
            var tryGetItem = _tracks.Query()
                .Where(a => a.Id == itemId.Uri)
                .FirstOrDefault();
            if (tryGetItem != null)
            {
                return Project(tryGetItem);
            }

            var item = await FetchSingleItem(itemId, ct);

            if (item != null)
            {
                _tracks.Upsert(item);
                return Project(item);
            }

            throw new NotSupportedException();
        }

        private async Task<IEnumerable<CachedPlayItem>> FetchNewItems(IEnumerable<ItemId> uris,
            CancellationToken ct = default)
        {
            var grouped = uris.GroupBy(a => a.Service)
                .ToArray();
            if (grouped.Length == 0) return Enumerable.Empty<CachedPlayItem>();
            var items = new List<CachedPlayItem>();
            foreach (var group in grouped)
            {
                //depending on the server, fetch the tracks and transform into CachedPlayItem.
                switch (group.Key)
                {
                    case ServiceType.Local:
                        break;
                    case ServiceType.Spotify:
                        {
                            var spotifyClient = Ioc.Default.GetRequiredService<ISpotifyClient>();
                            var request = new BatchedEntityRequest();
                            request.EntityRequest.AddRange(uris.Select(a => new EntityRequest
                            {
                                EntityUri = a.Uri,
                                Query =
                            {
                                new ExtensionQuery
                                {
                                    ExtensionKind = a.Type switch
                                    {
                                        EumEntityType.Track=> ExtensionKind.TrackV4,
                                        EumEntityType.Episode => ExtensionKind.EpisodeV4,
                                        _ => ExtensionKind.UnknownExtension
                                    }
                                }
                            }
                            }));
                            request.Header = new BatchedEntityRequestHeader
                            {
                                Catalogue = "premium",
                                Country = spotifyClient.AuthenticatedUser.CountryCode
                            };
                            using var metadataResponse = await "https://gae2-spclient.spotify.com"
                                .AppendPathSegments("extended-metadata", "v0", "extended-metadata")
                                .WithOAuthBearerToken((await spotifyClient.BearerClient.GetBearerTokenAsync(ct)))
                                .PostAsync(new ByteArrayContent(request.ToByteArray()), cancellationToken: ct);
                            var responseData =
                                BatchedExtensionResponse.Parser.ParseFrom(await metadataResponse.GetStreamAsync());

                            var data = responseData
                                .ExtendedMetadata
                                .SelectMany(a => a.ExtensionData
                                    .Select(k =>
                                    {
                                        var id = new ItemId(k.EntityUri);
                                        switch (id.Type)
                                        {
                                            case EumEntityType.Episode:
                                                return null;
                                                break;
                                            case EumEntityType.Track:
                                                var original = Track.Parser.ParseFrom(k.ExtensionData.Value);
                                                var images = original.Album.CoverGroup?
                                                    .Image.Select(a => new CachedImage
                                                    {
                                                        Height = (int?)(a.HasHeight ? a.Height : null),
                                                        Width = (int?)(a.HasWidth ? a.Width : null),
                                                        Id = HexId(a.FileId)
                                                    })?
                                                    .ToArray() ?? Array.Empty<CachedImage>();
                                                return new CachedPlayItem
                                                {
                                                    Name = original.Name,
                                                    Album = new CachedAlbum
                                                    {
                                                        Id = new SpotifyId(original.Album.Gid, EntityType.Album).Uri,
                                                        Images = images,
                                                        Name = original.Album.Name
                                                    },
                                                    Artists = original.Artist.Select(a => new CachedShortArtist
                                                    {
                                                        Id = new SpotifyId(a.Gid, EntityType.Artist).Uri,
                                                        Name = a.Name
                                                    })
                                                        .ToArray(),
                                                    Duration = original.Duration,
                                                    ExtraMetadata = new Dictionary<string, string>
                                                    {
                                                        {"file", original.File.ToString()},
                                                        {"alternative", original.Alternative.ToString()},
                                                        {"availability", original.Availability.ToString()},
                                                        {"explicit", original.Explicit.ToString()},
                                                        {"restriction", original.Restriction.ToString()}
                                                    },
                                                    Id = id.Uri
                                                };
                                                break;
                                        }

                                        return null;
                                    }));

                            items.AddRange(data);
                            //items.AddRange(responseData.ExtendedMetadata.Select(a=> a.));
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return items;
        }


        private async Task<CachedPlayItem?> FetchSingleItem(ItemId id, CancellationToken ct = default)
        {
            switch (id.Service)
            {
                case ServiceType.Local:
                    // var uri = new SpotifyId(x.EventArgs.Cluster.PlayerState.Track.Uri);
                    // var trackData = x.EventArgs.Cluster.PlayerState.Track.Uri.Split(":")
                    //     .Skip(2).ToArray();
                    //
                    // var artist = Url.Decode(trackData[0], true);
                    // var album = Url.Decode(trackData[1], true);
                    // var title = Url.Decode(trackData[2], true);
                    // var duration = double.Parse(trackData[3]);
                    //
                    // return (x.EventArgs, new Track
                    // {
                    //     Name = title,
                    //     Album = new Album
                    //     {
                    //         Name = album,
                    //
                    //     },
                    //     Artist =
                    //     {
                    //         new Artist
                    //         {
                    //             Name = artist
                    //         }
                    //     },
                    //     Duration = (int)duration
                    // }, null);
                    break;
                case ServiceType.Spotify:
                    switch (id.Type)
                    {
                        case EumEntityType.Episode:
                            break;
                        case EumEntityType.Track:
                            var uri = new SpotifyId(id.Uri);
                            var spotifyClient = Ioc.Default.GetRequiredService<ISpotifyClient>();
                            var original =
                                await spotifyClient.Tracks.MercuryTracks.GetTrack(uri.HexId(), ct);
                            var images = await UploadImages(_database, original.Album.CoverGroup, ct);
                            return new CachedPlayItem
                            {
                                Name = original.Name,
                                Album = new CachedAlbum
                                {
                                    Id = new SpotifyId(original.Album.Gid, EntityType.Album).Uri,
                                    Images = images,
                                    Name = original.Album.Name
                                },
                                Artists = original.Artist.Select(a => new CachedShortArtist
                                {
                                    Id = new SpotifyId(a.Gid, EntityType.Artist).Uri,
                                    Name = a.Name
                                })
                                    .ToArray(),
                                Duration = original.Duration,
                                ExtraMetadata = new Dictionary<string, string>
                                {
                                    {"file", original.File.ToString()},
                                    {"alternative", original.Alternative.ToString()},
                                    {"availability", original.Availability.ToString()},
                                    {"explicit", original.Explicit.ToString()},
                                    {"restriction", original.Restriction.ToString()}
                                },
                                Id = uri.Uri
                            };
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<CachedImage[]> UploadImages(ILiteDatabase db, ImageGroup images,
            CancellationToken ct = default)
        {
            var items = images
                .Image.Select(a => new
                {
                    Height = (int?) (a.HasHeight ? a.Height : null),
                    Width = (int?) (a.HasWidth ? a.Width : null),
                    Url = $"https://i.scdn.co/image/{HexId(a.FileId)}",
                    Id = HexId(a.FileId)
                });


            //https://i.scdn.co/image/ab67706c0000da8474ca565b7ad0c70e6c2973ce
            var streams = items.Select(async a =>
            {
                var imageExists = db.FileStorage.Exists(a.Id);
                if (imageExists)
                    return db.FileStorage.FindById(a.Id);

                try
                {
                    using var stream = await _httpClient.GetStreamAsync(a.Url);
                    var info = db.FileStorage.Upload(a.Id, a.Id, stream, new BsonDocument
                    {
                        {"height", a.Height},
                        {"width", a.Width}
                    });
                    return info;
                }
                catch (LiteException x)
                {
                    S_Log.Instance.LogError(x);
                    if (x.Message.StartsWith("Cannot insert duplicate key in unique index"))
                    {
                        imageExists = db.FileStorage.Exists(a.Id);
                        if (imageExists)
                            return db.FileStorage.FindById(a.Id);
                    }

                    return null;
                }
            });
            var uploadedImages =
                await Task.WhenAll(streams);
            return uploadedImages
                .Where(a=> a != null)
                .Select(a => new CachedImage
                {
                    Height = a.Metadata["height"].IsNull ? null : a.Metadata["height"].AsInt32,
                    Width = a.Metadata["width"].IsNull ? null : a.Metadata["width"].AsInt32,
                    Id = a.Id
                }).ToArray();

        }

        private static EumTrack Project(CachedPlayItem item)
        {
            return new EumTrack(item);
        }
        private static string HexId(ByteString id)
        {
            //Utils.bytesToHex(BASE62.decode(id.getBytes(), 16))
            var hexId =
                id.BytesToHex().ToLower();
            return hexId;
        }
    }

    public class CachedAlbumPlayItem
    {
        [BsonId]
        public string Id { get; set; }
        public string Name { get; set; }
        public CachedShortArtist[] Artists { get; set; }
        public int Duration { get; set; }
        public Dictionary<string, string> ExtraMetadata { get; set; }
    }
    public class CachedPlayItem
    {
        [BsonId]
        public string Id { get; set; }
        public string Name { get; set; }
        public CachedAlbum? Album { get; set; }
        public CachedShortArtist[] Artists { get; set; }
        public int Duration { get; set; }
        public Dictionary<string, string> ExtraMetadata { get; set; }
        public Dictionary<string, string> ExtraMetadataAlbum { get; set; }
    }

    public class CachedShortArtist
    {
        public string Name { get; set; }
        [BsonId]
        public string Id { get; set; }
    }

    public class CachedAlbum
    {
        [BsonId]
        public string Id { get; set; }
        public CachedImage[] Images { get; set; }
        public string Name { get; set; }
        public List<CachedAlbumPlayItem> Tracks { get; set; }
    }
    public class CachedImage
    {
        private AsyncLazy<Stream>? _imageStream;
        public int? Height { get; set; }
        public int? Width { get; set; }
        public string Id { get; set; }
        [BsonIgnore]
        public AsyncLazy<Stream> ImageStream
        {
            get
            {
                return _imageStream ??= new AsyncLazy<Stream>(async () =>
                {
                    var db =
                        Ioc.Default.GetRequiredService<ILiteDatabase>();
                    if (db.FileStorage.Exists(Id))
                    {
                        return db.FileStorage.OpenRead(Id);
                    }
                    //else we download itp
                    var test = new ImageGroup
                    {
                        Image =
                        {
                            new Image[]
                            {
                                new Image
                                {
                                    Width = Width ?? 0,
                                    Height = Height ?? 0,
                                    FileId = ByteString.CopyFrom(Id.HexToBytes())
                                }
                            }
                        }
                    };
                    await
                        TrackAggregator.UploadImages(db, test);

                    var fs =
                        db.FileStorage.OpenRead(Id);
                    return fs;
                });
            }
        }
    }
}
