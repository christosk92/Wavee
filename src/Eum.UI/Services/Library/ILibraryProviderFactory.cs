using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Connection;
using Eum.Connections.Spotify.Models.Library;
using Eum.Enums;
using Eum.Logging;
using Eum.UI.Items;
using Eum.UI.JsonConverters;
using Eum.UI.Users;
using Eum.UI.ViewModels.Artists;
using Eum.UI.ViewModels.Navigation;
using Flurl.Http;
using Flurl.Util;
using Google.Protobuf;
using Nito.AsyncEx;
using ReactiveUI;
using Spotify.Collection.Proto.V2;
using Spotify.CollectionCosmos.Proto;
using SpotifyTcp.Models;

namespace Eum.UI.Services.Library
{
    public interface ILibraryProviderFactory
    {
        ILibraryProvider GetLibraryProvider(EumUser forUser);
    }

    public class LibraryProviderFactory : ILibraryProviderFactory
    {
        private readonly ISpotifyClient _spotifyClient;

        public LibraryProviderFactory(ISpotifyClient spotifyClient)
        {
            _spotifyClient = spotifyClient;
        }

        public ILibraryProvider GetLibraryProvider(EumUser forUser)
        {
            return new LibraryProvider(forUser, _spotifyClient);
        }
    }

    public record CollectionUpdateNotification(ItemIdHolder Id, bool Added);
    public readonly struct ItemIdHolder : IEquatable<ItemIdHolder>
    {
        public ItemId Id { get; init; }
        public DateTimeOffset AddedAt { get; init; }

        public bool Equals(ItemIdHolder other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return obj is ItemIdHolder other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(ItemIdHolder left, ItemIdHolder right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemIdHolder left, ItemIdHolder right)
        {
            return !left.Equals(right);
        }
    }
    public class LibraryProvider : ILibraryProvider
    {
        private readonly ConcurrentDictionary<EntityType, AsyncManualResetEvent> _lock = new();
        private readonly HashSet<ItemIdHolder> _collection = new();
        private readonly ISpotifyClient _spotifyClient;
        private readonly EumUser _user;
        private IDisposable _listener;
        public LibraryProvider(EumUser user, ISpotifyClient spotifyClient)
        {
            _user = user;
            IsInitializing = true;
            _spotifyClient = spotifyClient;
            _lock = new ConcurrentDictionary<EntityType, AsyncManualResetEvent>(new[]
            {
                new KeyValuePair<EntityType, AsyncManualResetEvent>(EntityType.Track,
                    new AsyncManualResetEvent()),
                new KeyValuePair<EntityType, AsyncManualResetEvent>(EntityType.Artist,
                    new AsyncManualResetEvent()),
                new KeyValuePair<EntityType, AsyncManualResetEvent>(EntityType.Album,
                    new AsyncManualResetEvent()),
                new KeyValuePair<EntityType, AsyncManualResetEvent>(EntityType.Show,
                    new AsyncManualResetEvent()),
            });

            _listener = spotifyClient.CollectionUpdate
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(list =>
                {
                    foreach (var kvp in list.GroupBy(a => a.Id.Type))
                    {
                        var assArr = kvp.ToArray();
                        foreach (var collectionUpdate in assArr)
                        {
                            if (collectionUpdate.Removed)
                            {
                                _collection.Remove(new ItemIdHolder
                                {
                                    Id = new ItemId(collectionUpdate.Id.Uri)
                                });
                            }
                            else
                            {
                                _collection.Add(new ItemIdHolder
                                {
                                    Id = new ItemId(collectionUpdate.Id.Uri),
                                    AddedAt = collectionUpdate.AddedAt ?? DateTimeOffset.UtcNow
                                });
                            }
                        }
                        CollectionUpdated?.Invoke(this, (kvp.Key, assArr
                            .Select(a=> new CollectionUpdateNotification(new ItemIdHolder
                            {
                                Id = new ItemId(a.Id.Uri),
                                AddedAt = a.AddedAt ?? DateTimeOffset.UtcNow
                            }, !a.Removed)).ToArray()));
                    }
                });
        }

        public async ValueTask InitializeLibrary(CancellationToken ct = default)
        {
            switch (_user.Id.Service)
            {
                case ServiceType.Local:
                    return;
                case ServiceType.Spotify:
                    var tracksTask = Task.Run(async () => await FetchTracks(ServiceType.Spotify, ct), ct);
                    var albumsTask = Task.Run(async () => await FetchAlbums(ServiceType.Spotify, ct), ct);
                    var artistsTask = Task.Run(async () => await FetchArtists(ServiceType.Spotify, ct), ct);
                    var podcastsTask = Task.Run(async () => await FetchPodcasts(ServiceType.Spotify, ct), ct);
                    var ids = await Task.WhenAll(tracksTask, albumsTask, artistsTask, podcastsTask);
                    foreach (var id in ids.SelectMany(a => a))
                    {
                        _collection.Add(id);
                    }
                    _lock[EntityType.Track].Set();
                    var res = new[]
                    {
                        EntityType.Album,
                        EntityType.Artist,
                        EntityType.Track,
                        EntityType.Show,
                    }.Select(a => (a, _collection.Where(j => j.Id.Type == a)
                        .Select(k => new CollectionUpdateNotification(new ItemIdHolder
                        {
                            AddedAt = k.AddedAt,
                            Id = k.Id
                        }, true)).ToArray()));
                    foreach (var valueTuple in res)
                    {
                        CollectionUpdated?.Invoke(this, valueTuple);
                    }
                    break;
                case ServiceType.Apple:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            IsInitializing = false;
        }


        public bool IsSaved(ItemId id)
        {
            if (_lock.ContainsKey(id.Type))
                _lock[id.Type].Wait();
            else return false;
            return _collection.Contains(new ItemIdHolder
            {
                Id = id
            });
        }

        public async ValueTask<int> LibraryCount(EntityType type)
        {
            await Task.Run(async() => await _lock[type].WaitAsync());
            return _collection.Count(a => a.Id.Type == type);
        }

        public int TotalLibraryCount => _collection.Count;
        public bool IsInitializing { get; private set; }
        public event EventHandler<(EntityType Type, IReadOnlyList<CollectionUpdateNotification> Ids)>? CollectionUpdated;

        public void Deconstruct()
        {
            throw new NotImplementedException();
        }

        public void SaveItem(ItemId id)
        {
            switch (id.Service)
            {
                case ServiceType.Local:
                    break;
                case ServiceType.Spotify:
                    var now = DateTimeOffset.UtcNow;
                    // var base64 =
                    //     "Chk3dWNnaGRncXVmNmJ5cXVzcWtsaWx0d2MyEgpjb2xsZWN0aW9uGigKJHNwb3RpZnk6dHJhY2s6NUZWYnZ0dGpFdlE4cjJCZ1VjSmdOZxgBIhBmYWYxYjZmOWE5ZGU0NjVm";
                    // var r = WriteRequest.Parser.ParseFrom(ByteString.FromBase64(base64));
                    var req = new WriteRequest
                    {
                        Username = _spotifyClient.AuthenticatedUser.Username,
                        Set = id.Type switch
                        {
                            EntityType.Album => "collection",
                            EntityType.Track => "collection",
                            EntityType.Artist => "artist",
                            _ => throw new NotImplementedException()
                        },
                        Items =
                        {
                            new CollectionItem
                            {
                                IsRemoved = false,
                                AddedAt = (int)now.ToUnixTimeSeconds(),
                                Uri = id.Uri
                            }
                        }
                    };
                    //Content-Type: application/vnd.collection-v2.spotify.proto
                    //https://spclient.wg.spotify.com/collection/v2/write
                    _collection.Add(new ItemIdHolder
                    {
                        Id = id,
                        AddedAt = now
                    });
                    Task.Run(async () =>
                    {
                        using var by = new ByteArrayContent(req.ToByteArray());
                        using var _ = await "https://spclient.wg.spotify.com/collection/v2/write"
                            .WithOAuthBearerToken(await _spotifyClient.BearerClient.GetBearerTokenAsync())
                            .WithHeader("Content-Type", "application/vnd.collection-v2.spotify.proto")
                            .PostAsync(by);
                    });
                    break;
                case ServiceType.Apple:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UnsaveItem(ItemId id)
        {
            switch (id.Service)
            {
                case ServiceType.Local:
                    break;
                case ServiceType.Spotify:
                    var req = new WriteRequest
                    {
                        Username = _spotifyClient.AuthenticatedUser.Username,
                        Set = id.Type switch
                        {
                            EntityType.Album => "collection",
                            EntityType.Track => "collection",
                            EntityType.Artist => "artist",
                            _=> throw new NotImplementedException()
                        },
                        Items =
                        {
                            new CollectionItem
                            {
                                IsRemoved = true,
                                Uri = id.Uri
                            }
                        }
                    };
                    //Content-Type: application/vnd.collection-v2.spotify.proto
                    //https://spclient.wg.spotify.com/collection/v2/write
                    _collection.Remove(new ItemIdHolder
                    {
                        Id = id
                    });
                    Task.Run(async () =>
                    {
                        using var by = new ByteArrayContent(req.ToByteArray());
                        using var _ = await "https://spclient.wg.spotify.com/collection/v2/write"
                            .WithOAuthBearerToken(await _spotifyClient.BearerClient.GetBearerTokenAsync())
                            .WithHeader("Content-Type", "application/vnd.collection-v2.spotify.proto")
                            .PostAsync(by);
                    });
                    break;
                case ServiceType.Apple:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task<IEnumerable<ItemIdHolder>> FetchTracks(ServiceType serviceType, CancellationToken ct = default)
        {
            try
            {
                switch (serviceType)
                {
                    case ServiceType.Spotify:
                        var mercuryResponse = await _spotifyClient.MercuryClient
                            .SendAndReceiveResponseAsync(
                                new RawMercuryRequest($"hm://collection/collection/{_user.Id.Id}?format=json&allowonlytracks=false", "GET"),
                                MercuryRequestType.Get, ct);
                        var tracks =
                            JsonSerializer.Deserialize<MercuryCollectionResponse>(
                                mercuryResponse.Payload.Span, SystemTextJsonSerializationOptions.Default)?.Items ?? Enumerable.Empty<MercuryCollectionItem>();
                        return tracks.Select(a => new ItemIdHolder
                        {
                            Id = new ItemId(a.Id.Uri), 
                            AddedAt = a.AddedAtDate
                        });
                    case ServiceType.Local:
                        return Enumerable.Empty<ItemIdHolder>();
                    case ServiceType.Apple:
                        return Enumerable.Empty<ItemIdHolder>();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null);
                }
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                return Enumerable.Empty<ItemIdHolder>();
            }
            finally
            {
                _lock[EntityType.Track].Set();
            }
        }
        private async Task<IEnumerable<ItemIdHolder>> FetchAlbums(ServiceType spotify, CancellationToken ct)
        {
            try
            {
                switch (spotify)
                {
                    case ServiceType.Local:
                        break;
                    case ServiceType.Spotify:
                        await _lock[EntityType.Track].WaitAsync(ct);
                        break;
                    case ServiceType.Apple:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(spotify), spotify, null);
                }
                return Enumerable.Empty<ItemIdHolder>();
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                return Enumerable.Empty<ItemIdHolder>();
            }
            finally
            {
                _lock[EntityType.Album].Set();
            }
        }
        private async Task<IEnumerable<ItemIdHolder>> FetchArtists(ServiceType spotify, CancellationToken ct)
        {
            try
            {
                switch (spotify)
                {
                    case ServiceType.Spotify:
                        //FetchJsonArtists
                        var mercuryResponse = await _spotifyClient.MercuryClient
                            .SendAndReceiveResponseAsync(
                                new RawMercuryRequest($"hm://collection/artist/{_user.Id.Id}?allowonlytracks=false&format=json", "GET"),
                                MercuryRequestType.Get, ct);
                        var artists =
                            JsonSerializer.Deserialize<MercuryCollectionResponse>(
                                mercuryResponse.Payload.Span, SystemTextJsonSerializationOptions.Default)?.Items ?? Enumerable.Empty<MercuryCollectionItem>();
                        return artists.Select(a => new ItemIdHolder
                        {
                            Id = new ItemId(a.Id.Uri),
                            AddedAt = a.AddedAtDate
                        });
                }
                return Enumerable.Empty<ItemIdHolder>();
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                return Enumerable.Empty<ItemIdHolder>();
            }
            finally
            {
                _lock[EntityType.Artist].Set();
            }
        }
        private async Task<IEnumerable<ItemIdHolder>> FetchPodcasts(ServiceType spotify, CancellationToken ct)
        {
            try
            {
                _lock[EntityType.Show].Set();
                return Enumerable.Empty<ItemIdHolder>();
            }
            catch (Exception x)
            {
                S_Log.Instance.LogError(x);
                return Enumerable.Empty<ItemIdHolder>();
            }
            finally
            {
                _lock[EntityType.Show].Set();
            }
        }
    }
}
