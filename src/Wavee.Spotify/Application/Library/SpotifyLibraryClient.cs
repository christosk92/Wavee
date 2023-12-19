using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using Mediator;
using System.Text.Json;
using NeoSmart.AsyncLock;
using Wavee.Spotify.Application.Common.Queries;
using Wavee.Spotify.Application.GraphQL.Queries;
using Wavee.Spotify.Application.Library.Query;
using Wavee.Spotify.Application.Metadata.Query;
using Wavee.Spotify.Application.Playlist;
using Wavee.Spotify.Application.Remote;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Library;
using Wavee.Spotify.Infrastructure.LegacyAuth;
using Eum.Spotify.playlist4;
using Spotify.Metadata;
using Wavee.Spotify.Domain.Tracks;

namespace Wavee.Spotify.Application.Library;

internal class SpotifyLibraryClient : ISpotifyLibraryClient
{
    private readonly record struct LibraryKeyComposite(string User, SpotifyItemType Type);
    private readonly IMediator _mediator;
    private readonly SpotifyTcpHolder _tcpHolder;
    private readonly List<SpotifyLibraryChangeListener> _changeListeners = new();

    public SpotifyLibraryClient(IMediator mediator,
        SpotifyTcpHolder tcpHolder,
        SpotifyRemoteHolder remoteHolder)
    {
        _mediator = mediator;
        _tcpHolder = tcpHolder;

        remoteHolder.ItemAdded += (sender, item) =>
        {
            foreach (var group in item.GroupBy(x => x.Item.Type switch
                     {
                         SpotifyItemType.Track => SpotifyLibaryType.Songs,
                         SpotifyItemType.Album => SpotifyLibaryType.Album,
                         SpotifyItemType.Artist => SpotifyLibaryType.Artist,
                         _ => throw new ArgumentOutOfRangeException()
                     }))
            {
                var notification = new LibraryModificationInfo
                {
                    Added = group.Select(f => f)
                        .ToImmutableArray(),
                    Removed = null,
                    IsAdded = true,
                    Type = group.Key
                };
                foreach (var listener in _changeListeners)
                {
                    if (listener.Type == group.Key)
                    {
                        listener.Incoming(notification);
                    }
                }
            }
        };
        remoteHolder.ItemRemoved += (sender, e) =>
        {
            foreach (var group in e.GroupBy(x => x.Type switch
                     {
                         SpotifyItemType.Track => SpotifyLibaryType.Songs,
                         SpotifyItemType.Album => SpotifyLibaryType.Album,
                         SpotifyItemType.Artist => SpotifyLibaryType.Artist,
                         _ => throw new ArgumentOutOfRangeException()
                     }))
            {
                var notification = new LibraryModificationInfo
                {
                    Added = null,
                    Removed = group.Select(f => f)
                        .ToImmutableArray(),
                    IsAdded = false,
                    Type = group.Key
                };
                foreach (var listener in _changeListeners)
                {
                    if (listener.Type == group.Key)
                    {
                        listener.Incoming(notification);
                    }
                }
            }
        };
    }

    public async Task<SpotifyArtistsLibrary> GetArtists(bool orderOnRecentlyPlayed,CancellationToken cancellationToken = default)
    {
        var user = _tcpHolder.WelcomeMessage.Result.CanonicalUsername;
        var recentlyPlayedTask = Task.Run(async () =>
        {
            if (orderOnRecentlyPlayed is true)
            {
                return await _mediator.Send(new FetchRecentlyPlayedQuery
                {
                    User = user,
                    Limit = 50,
                    Filter = "default,track,collection-new-episodes"
                }, cancellationToken);
            }

            return null;
        }, cancellationToken);
        //Get 
        var items = await _mediator.Send(new FetchArtistCollectionQuery
        {
            User = user,
        }, cancellationToken);


        var uris = items.ToDictionary(x => x.Item.ToString(), x => x);
        var metadataRaw = await _mediator.Send(new FetchBatchedMetadataQuery
        {
            AllowCache = true,
            Uris = uris.Keys,
            Country = _tcpHolder.Country,
            ItemsType = SpotifyItemType.Artist
        }, cancellationToken);

        var metadata = metadataRaw
            .ToDictionary(x => x.Key,
                x => global::Spotify.Metadata.Artist.Parser.ParseFrom(x.Value));

        var recentlyPlayed = await recentlyPlayedTask;
        var finalList = metadata.Select(x =>
        {
            var libraryItem = uris[x.Key];
            var recentlyPlayedItem = recentlyPlayed?
                .Where(f => f.Uri == x.Key)?
                .OrderByDescending(x=> x.PlayedAt)?
                .FirstOrDefault()?
                .PlayedAt;
            return new SpotifyLibraryItem<global::Spotify.Metadata.Artist>
            {
                Item = x.Value,
                AddedAt = libraryItem.AddedAt,
                LastPlayedAt = recentlyPlayedItem
            };
        });


        return new SpotifyArtistsLibrary
        {
            Items = finalList.ToImmutableArray()
        };
    }

    public async Task<SpotifySongsLibrary> GetTracks(CancellationToken cancellationToken)
    {
        var user = _tcpHolder.WelcomeMessage.Result.CanonicalUsername;
        //Get 
        var items = await _mediator.Send(new FetchTracksCollectionQuery
        {
            User = user,
            WithAlbums = false
        }, cancellationToken);

        var uris = items.ToDictionary(x => x.Item.ToString(), x => x);
        var metadataRaw = await _mediator.Send(new FetchBatchedMetadataQuery
        {
            AllowCache = true,
            Uris = uris.Keys,
            Country = _tcpHolder.Country,
            ItemsType = SpotifyItemType.Artist
        }, cancellationToken);

        var metadata = metadataRaw
            .ToDictionary(x => x.Key,
                x => global::Spotify.Metadata.Track.Parser.ParseFrom(x.Value));

        var finalList = metadata.Select(x =>
        {
            var libraryItem = uris[x.Key];
            return new SpotifyLibraryItem<Track>
            {
                Item = x.Value,
                AddedAt = libraryItem.AddedAt,
                LastPlayedAt = null
            };
        });

        return new SpotifySongsLibrary
        {
            Items = finalList.ToImmutableArray()
        };
    }

    public SpotifyLibraryChangeListener ChangeListener(SpotifyLibaryType library)
    {
        var listener = new SpotifyLibraryChangeListener(library);
        _changeListeners.Add(listener);
        return listener;
    }

    private SpotifyLibraryItem<SpotifySimpleArtist>[] FilterSortLimit(
        IEnumerable<SpotifyLibraryItem<SpotifySimpleArtist>> finalList,
        string query,
        SpotifyArtistLibrarySortField order,
        int offset,
        int limit)
    {
        if (!string.IsNullOrEmpty(query))
        {
            finalList = finalList
                .Where(x => x.Item.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        finalList = order switch
        {
            SpotifyArtistLibrarySortField.Recents => finalList
                .OrderByDescending(x => x.LastPlayedAt)
                .ThenByDescending(x => x.AddedAt),
            SpotifyArtistLibrarySortField.RecentlyAdded => finalList.OrderByDescending(x => x.AddedAt),
            SpotifyArtistLibrarySortField.Alphabetical => finalList.OrderBy(f => f.Item.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(order), order, null)
        };

        return finalList
            .Skip(offset)
            .Take(limit)
            .ToArray();
    }

    private IReadOnlyCollection<SpotifyImage> BuildImages(global::Spotify.Metadata.Artist artist)
    {
        var portraitrGroup = artist.PortraitGroup.Image;
        const string url = "https://i.scdn.co/image/";
        return portraitrGroup.Select(c =>
        {
            var id = SpotifyId.FromRaw(c.FileId.Span, SpotifyItemType.Unknown);
            var hex = id.ToBase16();
            var uri = $"{url}{hex}";
            return new SpotifyImage(
                Url: uri,
                Width: (ushort?)c.Width,
                Height: (ushort?)c.Height);
        }).ToImmutableArray();
    }

    private static SpotifyImage[] ParseVisuals(JsonElement getProperty)
    {
        var sources = getProperty.GetProperty("sources");
        using var enumerator = sources.EnumerateArray();
        var output = new SpotifyImage[sources.GetArrayLength()];
        var i = 0;
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            var url = curr.GetProperty("url").GetString();
            var width = curr.GetProperty("width").TryGetUInt16(out var w) ? w : (ushort?)null;
            var height = curr.GetProperty("height").TryGetUInt16(out var h) ? h : (ushort?)null;
            output[i++] = new SpotifyImage(url, width, height);
        }
        return output;
    }
}

public interface ISpotifyLibraryClient
{
    Task<SpotifyArtistsLibrary> GetArtists(bool orderOnRecentlyPlayed, CancellationToken cancellationToken = default);

    Task<SpotifySongsLibrary> GetTracks(CancellationToken cancellationToken);

    SpotifyLibraryChangeListener ChangeListener(SpotifyLibaryType library);
}

public sealed class SpotifyLibraryChangeListener
{
    internal SpotifyLibraryChangeListener(SpotifyLibaryType type)
    {
        Type = type;
    }
    public SpotifyLibaryType Type { get; }

    public event EventHandler<LibraryModificationInfo>? ItemsChanged;

    public void Incoming(LibraryModificationInfo libarryLibraryModificationInfo)
    {
        ItemsChanged?.Invoke(this, libarryLibraryModificationInfo);
    }
}
