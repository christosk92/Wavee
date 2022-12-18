using Eum.Connections.Spotify;
using Eum.Logging;
using Eum.Spotify.metadata;
using Eum.UI.Items;
using Eum.UI.Services.Tracks;
using LiteDB;
using System.Net.Http;
using Eum.Connections.Spotify.Models.Artists;

namespace Eum.UI.Services.Albums
{
    public class AlbumProvider : IAlbumProvider
    {
        private readonly ILiteDatabase _database;
        private readonly ILiteCollection<CachedAlbum> _albums;

        private readonly ISpotifyClient _spotifyClient;

        public AlbumProvider(ISpotifyClient spotifyClient, ILiteDatabase database)
        {
            _spotifyClient = spotifyClient;
            _database = database;
            _albums = _database.GetCollection<CachedAlbum>("Albums");
        }
        public async ValueTask<EumAlbum> GetAlbum(ItemId id, string locale, CancellationToken ct = default)
        {
            switch (id.Service)
            {
                case ServiceType.Local:
                    break;
                case ServiceType.Spotify:
                    if (_albums.Exists(a => a.Id == id.Uri))
                    {
                        var cachedAlbum = _albums
                            .Include(x => x.Tracks)
                            .FindById(id.Uri);

                        return new EumAlbum(cachedAlbum);
                    }

                    var mercuryUrl = await _spotifyClient
                        .Albums.Mercury.GetAlbum(id.Uri, locale, _spotifyClient.AuthenticatedUser.CountryCode, ct);
                    var uploadedImage = await UploadImage(_database, mercuryUrl.Cover, ct);
                    var uploadImages = new CachedImage[]
                    {
                        uploadedImage
                    };
                    var tracks = mercuryUrl.Discs
                        .SelectMany(a => a.Select(j => new CachedAlbumPlayItem()
                        {
                            Artists = j.Artists
                                .Select(k => new CachedShortArtist
                                {
                                    Id = k.Uri.Uri,
                                    Name = k.Name
                                }).ToArray(),
                            Duration = j.Duration,
                            Id = j.Uri.Uri,
                            Name = j.Name,
                            ExtraMetadata = new Dictionary<string, string>
                            {
                                {"explicit", j.Explicit.ToString()},
                                {"playable", j.Playable.ToString()},
                                {"playcount", j.PlayCount.ToString()},
                                {"popularity", j.Popularity.ToString()},
                            }
                        })).ToList();
                    _albums.Insert(new CachedAlbum
                    {
                        Id = id.Uri,
                        Images = uploadImages,
                        Name = mercuryUrl.Name,
                        Tracks = tracks
                    });
                    return new EumAlbum(mercuryUrl);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return default;
        }

        private static readonly HttpClient _httpClient = new HttpClient();
        public static async Task<CachedImage?> UploadImage(ILiteDatabase db, UriImage image,
            CancellationToken ct = default)
        {
            //https://i.scdn.co/image/ab67706c0000da8474ca565b7ad0c70e6c2973ce

            var imageExists = db.FileStorage.Exists(image.Uri);
            if (imageExists)
            {
                var item = db.FileStorage.FindById(image.Uri);
                return new CachedImage
                {
                    Id = item.Id,
                    Height = item.Metadata["height"].IsNull ? null : item.Metadata["height"].AsInt32,
                    Width = item.Metadata["width"].IsNull ? null : item.Metadata["width"].AsInt32,
                };
            }

            try
            {
                using var stream = await _httpClient.GetStreamAsync(image.Uri);
                var info = db.FileStorage.Upload(image.Uri,
                    image.Uri, stream, new BsonDocument
                {
                    {"height", null},
                    {"width", null}
                });
                return new CachedImage
                {
                    Height = null,
                    Width = null,
                    Id = image.Uri
                };
            }
            catch (LiteException x)
            {
                S_Log.Instance.LogError(x);
                return null;
            }
        }

    }

}