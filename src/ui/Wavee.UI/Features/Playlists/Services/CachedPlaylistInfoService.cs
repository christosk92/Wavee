using System.Collections.Immutable;
using System.Numerics;
using System.Text;
using Eum.Spotify.playlist4;
using Google.Protobuf.Collections;
using Nito.AsyncEx;
using Wavee.Domain.Library;
using Wavee.Spotify.Application.Library;
using Wavee.Spotify.Application.Playlist;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Library;
using Wavee.UI.Domain.Library;
using Wavee.UI.Domain.Playlist;
using Wavee.UI.Domain.Track;
using Wavee.UI.Extensions;
using Wavee.UI.Features.Library.Queries;
using Wavee.UI.Features.Library.QueryHandlers;
using Wavee.UI.Features.Playlists.QueryHandlers;

namespace Wavee.UI.Features.Playlists.Services;

internal sealed class CachedPlaylistInfoService : ICachedPlaylistInfoService
{
    private readonly record struct PlaylistTracksInfoKey(string Id, BigInteger Revision) :
        IComparable<PlaylistTracksInfoKey>,
        IComparable
    {
        public int CompareTo(PlaylistTracksInfoKey other)
        {
            var idComparison = string.Compare(Id, other.Id, StringComparison.Ordinal);
            if (idComparison != 0) return idComparison;
            return Revision.CompareTo(other.Revision);
        }

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is PlaylistTracksInfoKey other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(PlaylistTracksInfoKey)}");
        }
    }
    private readonly Dictionary<string, object> _changeListeners = new();
    private readonly SortedDictionary<PlaylistTracksInfoKey, IReadOnlyCollection<PlaylistTrackInfo>> _tracks = new();
    private readonly ISpotifyClient _spotifyClient;
    private readonly AsyncLock _lock = new AsyncLock();
    public CachedPlaylistInfoService(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public bool TryGetTracks(string playlistId,
        BigInteger? revision,
        out IReadOnlyCollection<PlaylistTrackInfo> tracks)
    {
        try
        {
            using (_lock.Lock())
            {
                if (_tracks.Count is 0)
                {
                    tracks = default;
                    return false;
                }

                if (revision is null)
                {
                    var filtered = _tracks
                        .Where(f => f.Key.Id == playlistId)
                        .ToImmutableArray();

                    if (filtered.Length > 0)
                    {
                        var maxRevision = filtered.MaxBy(f => f.Key.Revision);
                        tracks = maxRevision.Value;
                        return true;
                    }
                }

                var key = new PlaylistTracksInfoKey(playlistId, revision ?? BigInteger.Zero);
                if (_tracks.TryGetValue(key, out var ids))
                {
                    tracks = ids;
                    return true;
                }

                tracks = default;
                return false;
            }
        }
        catch (ArgumentException x)
        {
            tracks = default;
            return false;
        }
    }

    public void SetTracks(string playlistId,
        BigInteger revision,
        IReadOnlyCollection<PlaylistTrackInfo> tracks)
    {
        using (_lock.Lock())
        {
            if (!_changeListeners.TryGetValue(playlistId, out _))
            {
                if (SpotifyId.TryParse(playlistId, out var spotifyPlaylistId)
                    && spotifyPlaylistId.Type == SpotifyItemType.Playlist)
                {
                    // Register as playlist
                    var changeListener = _spotifyClient.Playlists.ChangeListener(spotifyPlaylistId);
                    changeListener.ItemsChanged += OnItemsChanged;
                    _changeListeners[playlistId] = changeListener;
                }
                else if (playlistId.StartsWith("spotify:collection:"))
                {
                    var last = playlistId.Split(":").Last();
                    var collectionType = Enum.Parse<SpotifyLibaryType>(last, true);
                    var changeListener = _spotifyClient.Library.ChangeListener(collectionType);
                    changeListener.ItemsChanged += OnLibraryItemsChanged;
                    _changeListeners[playlistId] = changeListener;
                }
            }

            var key = new PlaylistTracksInfoKey(playlistId, revision);
            _tracks[key] = tracks;
        }
    }

    public void Clear(string playlistId)
    {
        if (_changeListeners.Remove(playlistId, out var changeListener))
        {
            if (changeListener is SpotifyPlaylistChangeListener playlistChangeListener)
                playlistChangeListener.ItemsChanged -= OnItemsChanged;
            else if (changeListener is SpotifyLibraryChangeListener libraryChangeListener)
                libraryChangeListener.ItemsChanged -= OnLibraryItemsChanged;
            else
                throw new ArgumentOutOfRangeException();
        }

        foreach (var apropriatekey in _tracks.Where(f => f.Key.Id == playlistId))
        {
            _tracks.Remove(apropriatekey.Key);
        }
    }

    public event EventHandler<string>? PlaylistChanged;
    public event EventHandler<WaveeLibraryType>? LibraryChanged;

    private async void OnLibraryItemsChanged(object? sender, LibraryModificationInfo libraryModification)
    {
        // So because we are using an old version of the library, we need to refetch the entire list.
        var userId = _spotifyClient.User.Result.CanonicalUsername;
        var uri = libraryModification.Type switch
        {
            SpotifyLibaryType.Artist => $"spotify:collection:{userId}:artist",
            SpotifyLibaryType.Album => $"spotify:collection:{userId}:album",
            SpotifyLibaryType.Songs => $"spotify:collection:{userId}:songs",
            _ => throw new ArgumentOutOfRangeException()
        };

        var items = await _spotifyClient.Library.GetTracks(CancellationToken.None);

        var newKey = new PlaylistTracksInfoKey(uri, BigInteger.Zero);
        _tracks[newKey] = GetLibrarySongsQueryHandler.ToPlaylistItems(new LibraryItems<SimpleTrackEntity>
        {
            Items = items.Items.Select(f => new LibraryItem<SimpleTrackEntity>
            {
                Item = f.Item.ToSimpleTrack(),
                AddedAt = f.AddedAt,
                LastPlayedAt = f.LastPlayedAt
            }).ToImmutableArray(),
            Total = items.Items.Count
        });
        LibraryChanged?.Invoke(this, libraryModification.Type switch
        {
            SpotifyLibaryType.Artist => WaveeLibraryType.Artist,
            SpotifyLibaryType.Album => WaveeLibraryType.Album,
            SpotifyLibaryType.Songs => WaveeLibraryType.Songs,
            _ => throw new ArgumentOutOfRangeException()
        });
    }
    private async void OnItemsChanged(object? sender, PlaylistModificationInfo playlistModificationInfo)
    {
        var newRevisionId = playlistModificationInfo.NewRevision.ToBigInteger();
        var fromRevisionId = playlistModificationInfo.ParentRevision.ToBigInteger();
        // Check if we have this revision.
        var uri = Encoding.UTF8.GetString(playlistModificationInfo.Uri.Span);
        var fromRevision = new PlaylistTracksInfoKey(uri, fromRevisionId);
        if (!_tracks.TryGetValue(fromRevision, out var fromRevisionTracks))
        {
            // We are out of sync, its easier to refetch the entire list
            var (selectedList, tracks) =
                await _spotifyClient.Playlists.GetPlaylistWithTracks(SpotifyId.FromUri(uri), CancellationToken.None);

            var adapted = tracks.ToDictionary(f => f.Id.ToString(), f => f.MapToSimpleEntity());
            var newItems = GetPlaylistTracksIdsQueryHandler.ParseItems(selectedList.Contents.Items, adapted);
            var newKey = new PlaylistTracksInfoKey(uri, newRevisionId);
            _tracks[newKey] = newItems;
            PlaylistChanged?.Invoke(this, uri);
            return;
        }

        using (_lock.Lock())
        {
            // Create a new list
            var newList = await DiffList(playlistModificationInfo.Ops, fromRevisionTracks);
            var newKey = new PlaylistTracksInfoKey(uri, newRevisionId);
            _tracks[newKey] = newList;
        }

        PlaylistChanged?.Invoke(this, uri);
    }

    private async Task<IReadOnlyCollection<PlaylistTrackInfo>> DiffList(RepeatedField<Op> ops, IReadOnlyCollection<PlaylistTrackInfo> fromRevisionTracks)
    {
        var newList = fromRevisionTracks.ToList();
        foreach (var op in ops)
        {
            switch (op.Kind)
            {
                case Op.Types.Kind.Unknown:
                    break;
                case Op.Types.Kind.Add:
                    {
                        var add = op.Add;
                        if (add.HasFromIndex)
                        {
                            newList.InsertRange(add.FromIndex, GetPlaylistTracksIdsQueryHandler.ParseItems(add.Items, null));
                        }
                        else
                        {

                        }
                        break;
                    }
                case Op.Types.Kind.Rem:
                    {
                        var rem = op.Rem;
                        if (rem.HasFromIndex)
                        {
                            var fromIndex = rem.FromIndex;
                            var len = rem.Length;
                            newList.RemoveRange(fromIndex, len);
                        }
                        break;
                    }
                case Op.Types.Kind.Mov:
                    {
                        var mov = op.Mov;
                        var from = mov.FromIndex;
                        var to = mov.ToIndex;
                        var length = mov.Length;
                        // Extract items to be moved
                        var itemsToMove = newList.GetRange(from, length);

                        // Remove items from original position
                        newList.RemoveRange(from, length);

                        // Adjust ToIndex if necessary
                        if (to > from)
                        {
                            to -= length;
                        }

                        // Insert items at new position
                        newList.InsertRange(to, itemsToMove);
                        break;
                    }
                case Op.Types.Kind.UpdateItemAttributes:
                    break;
                case Op.Types.Kind.UpdateListAttributes:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return newList;
    }
}

public interface ICachedPlaylistInfoService
{
    bool TryGetTracks(string playlistId,
        BigInteger? revision,
        out IReadOnlyCollection<PlaylistTrackInfo> tracks);

    void SetTracks(string playlistId,
        BigInteger revision,
        IReadOnlyCollection<PlaylistTrackInfo> tracks);

    void Clear(string playlistId);
    event EventHandler<string> PlaylistChanged;
    event EventHandler<WaveeLibraryType> LibraryChanged;
}