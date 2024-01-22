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


    internal sealed class SingularTrackContext : IWaveePlayerContext
    {
        private Func<Task<WaveeStream>> _trackStreamFactory;

        public SingularTrackContext(Func<Task<WaveeStream>> trackStreamFactory)
        {
            _trackStreamFactory = trackStreamFactory;
        }

        public ValueTask<Option<WaveeContextStream>> GetNextStream()
        {
            if (_trackStreamFactory is null)
            {
                return new ValueTask<Option<WaveeContextStream>>(Option<WaveeContextStream>.None);
            }

            var stream = _trackStreamFactory();
            _trackStreamFactory = null;
            return new ValueTask<Option<WaveeContextStream>>(AwaitRes(stream));
        }


        public ValueTask<Option<WaveeContextStream>> GetPreviousStream()
        {
            return new ValueTask<Option<WaveeContextStream>>(Option<WaveeContextStream>.None);
        }

        public Option<WaveeContextStream> CurrentStream { get; private set; }

        public ValueTask<bool> TrySkip(int count)
        {
            // Cant skip
            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> TryPeek(int count)
        {
            // Cant peek
            return new ValueTask<bool>(false);
        }

        public ValueTask<bool> MoveTo(int index)
        {
            throw new NotImplementedException();
        }

        private async Task<Option<WaveeContextStream>> AwaitRes(Task<WaveeStream> stream)
        {
            var res = await stream;
            CurrentStream =
                Option<WaveeContextStream>.Some(new WaveeContextStream(res, new ComposedKey(res.Metadata.Id!)));
            return CurrentStream;
        }
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

// internal sealed class SpotifyAlbumContext : SpotifyRealContext
// {
//     public SpotifyAlbumContext(Guid connectionId, SpotifyId itemId, Option<int> startIndex,
//         Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory) : base(connectionId, itemId, startIndex,
//         streamFactory)
//     {
//     }
//
//     protected override ValueTask<Option<SpotifyContextPage>> NextPage()
//     {
//         throw new NotImplementedException();
//     }
// }

internal sealed class SpotifyPlaylistOrAlbumContext : SpotifyRealContext
{
    //private Option<Option<ContextPage>> _currentPage;
    private Option<Queue<ContextPage>> _pages = new();
    private Option<ContextPage> _lastPage = Option<ContextPage>.None;
    private int _tracksBefore = 0;
    private int _pagesBefore = 0;
    private HashMap<string, string> _contextMetadata = Empty;

    public SpotifyPlaylistOrAlbumContext(Guid connectionId, SpotifyId itemId, Option<ValueTask<int>> startIndex,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory) : base(connectionId, itemId, startIndex,
        streamFactory)
    {
    }


    public override HashMap<string, string> ContextMetadata => _contextMetadata;

    protected override async ValueTask<Option<SpotifyContextPage>> NextPage()
    {
        if (_pages.IsNone)
        {
            // initialize !
            // 1) Context-Resolve
            // 2) Add pages
            if (!EntityManager.TryGetClient(_connectionId, out var client))
            {
                throw new InvalidOperationException("Client not found");
            }

            var contextResolveResponse = await client.Playback.ResolveContext(_itemId.ToString());
            var pages = new Queue<ContextPage>();
            foreach (var pageInContext in contextResolveResponse.Pages)
            {
                pages.Enqueue(pageInContext);
            }

            foreach (var metadatakey in contextResolveResponse.Metadata)
            {
                _contextMetadata = _contextMetadata.Add(metadatakey.Key, metadatakey.Value);
            }

            _pages = Some(pages);
        }

        var pagesValue = _pages.ValueUnsafe();
        if (!pagesValue.TryDequeue(out var page))
        {
            if (_lastPage.IsNone)
                return None;

            var lastPageValue = _lastPage.ValueUnsafe();
            // TODO: Try fetch next page
            return Option<SpotifyContextPage>.None;
        }

        var x = MapToSpotifyContextPage(page, true);

        _lastPage = Some(page);
        return Some(x);
    }

    protected override async ValueTask<Option<SpotifyContextPage>> PeekNextPage()
    {
        if (_pages.IsNone)
        {
            // initialize !
            // 1) Context-Resolve
            // 2) Add pages
            if (!EntityManager.TryGetClient(_connectionId, out var client))
            {
                throw new InvalidOperationException("Client not found");
            }

            var contextResolveResponse = await client.Playback.ResolveContext(_itemId.ToString());
            var pages = new Queue<ContextPage>();
            foreach (var pageInContext in contextResolveResponse.Pages)
            {
                pages.Enqueue(pageInContext);
            }

            foreach (var metadatakey in contextResolveResponse.Metadata)
            {
                _contextMetadata = _contextMetadata.Add(metadatakey.Key, metadatakey.Value);
            }

            _pages = Some(pages);
        }

        var pagesValue = _pages.ValueUnsafe();
        if (!pagesValue.TryPeek(out var page))
        {
            if (_lastPage.IsNone)
                return None;

            var lastPageValue = _lastPage.ValueUnsafe();
            // TODO: Try fetch next page
            return Option<SpotifyContextPage>.None;
        }

        var x = MapToSpotifyContextPage(page, false);

        _lastPage = Some(page);
        return Some(x);
    }

    private SpotifyContextPage MapToSpotifyContextPage(ContextPage result, bool mutate)
    {
        var tracks = new LinkedList<SpotifyContextTrack>();
        for (int i = 0; i < result.Tracks.Count; i++)
        {
            var track = result.Tracks[i];
            SpotifyId id = default;
            if (!string.IsNullOrEmpty(track.Uri))
            {
                id = SpotifyId.FromUri(track.Uri);
            }
            else if (track.Gid.Span.Length > 0)
            {
                id = SpotifyId.FromRaw(track.Gid.Span, AudioItemType.Track);
            }
            else
            {
                continue;
            }

            tracks.AddLast(new SpotifyContextTrack(id, track.Uid, _tracksBefore + i));
        }

        if (mutate)
        {
            _tracksBefore += result.Tracks.Count;
            _pagesBefore++;
        }

        {
        }

        return new SpotifyContextPage(tracks, (uint)(_pagesBefore - 1));
    }
}

internal sealed class SpotifyArtistContext : SpotifyRealContext
{
    public SpotifyArtistContext(Guid connectionId, SpotifyId itemId, Option<ValueTask<int>> startIndex,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory) : base(connectionId, itemId, startIndex,
        streamFactory)
    {
    }

    public override HashMap<string, string> ContextMetadata { get; }

    protected override ValueTask<Option<SpotifyContextPage>> NextPage()
    {
        throw new NotImplementedException();
    }

    protected override ValueTask<Option<SpotifyContextPage>> PeekNextPage()
    {
        throw new NotImplementedException();
    }
}

public interface ISpotifyContext : IWaveePlayerContext
{
    string ContextUri { get; }
    string ContextUrl { get; }
    HashMap<string, string> ContextMetadata { get; }
}

internal abstract class SpotifyRealContext : ISpotifyContext
{
    protected readonly Guid _connectionId;
    protected readonly SpotifyId _itemId;
    protected Option<ValueTask<int>> _startIndex;
    private readonly Func<SpotifyId, CancellationToken, Task<WaveeStream>> _streamFactory;
    protected readonly LinkedList<SpotifyContextPage> _pages = new();
    private Option<LinkedListNode<SpotifyContextPage>> _currentPage = Option<LinkedListNode<SpotifyContextPage>>.None;

    private Option<LinkedListNode<SpotifyContextTrack>>
        _currentTrack = Option<LinkedListNode<SpotifyContextTrack>>.None;

    protected SpotifyRealContext(Guid connectionId,
        SpotifyId itemId,
        Option<ValueTask<int>> startIndex,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory)
    {
        _connectionId = connectionId;
        _itemId = itemId;
        _startIndex = startIndex;
        _streamFactory = streamFactory;

        ContextUri = itemId.ToString();
        ContextUrl = $"context://{ContextUri}";
    }

    public string ContextUri { get; }
    public string ContextUrl { get; }
    public abstract HashMap<string, string> ContextMetadata { get; }
    public Option<SpotifyContextPage> CurrentPage => _currentPage.Map(x => x.Value);

    protected abstract ValueTask<Option<SpotifyContextPage>> NextPage();
    protected abstract ValueTask<Option<SpotifyContextPage>> PeekNextPage();

    public async ValueTask<Option<WaveeContextStream>> GetNextStream()
    {
        var currentPage = _currentPage;
        var currentTrack = _currentTrack;
        bool skipped = false;
        if (_startIndex.IsSome)
        {
            var startIndexVal = await _startIndex.ValueUnsafe();
            _startIndex = None;
            if (!(await TrySkip(startIndexVal)))
            {
                return None;
            }

            skipped = true;
            currentPage = _currentPage;
            currentTrack = _currentTrack;
        }

        if (currentPage == None || currentTrack == None)
        {
            // No current page or current track, fetch the first page
            var firstPage = await NextPage();
            if (firstPage.IsSome)
            {
                currentPage = _pages.AddLast(firstPage.ValueUnsafe());
                _currentPage = currentPage;
                currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                _currentTrack = currentTrack;
            }
            else
            {
                // No more pages, no more tracks
                CurrentStream = Option<WaveeContextStream>.None;
                return Option<WaveeContextStream>.None;
            }
        }
        else
        {
            // Move to the next track in the current page
            //currentTrack = currentTrack.ValueUnsafe().Next;
            _currentTrack = currentTrack.Bind(x =>
            {
                if (skipped)
                    return x;
                var next = x.Next;
                if (next == null)
                {
                    return None;
                }

                return Some(next);
            });

            if (currentTrack == null)
            {
                // Reached the end of the current page, fetch the next page
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks
                    CurrentStream = Option<WaveeContextStream>.None;
                    return Option<WaveeContextStream>.None;
                }
            }
        }

        var waveeStream = await _streamFactory(_currentTrack.ValueUnsafe().Value.Gid, CancellationToken.None);
        var keys = new List<object>(5);
        if (_currentTrack.ValueUnsafe().Value.Uid.IsSome)
        {
            keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Uid,
                _currentTrack.ValueUnsafe().Value.Uid.ValueUnsafe()));
        }

        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Provider, "context"));
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Id, waveeStream.Metadata.Id));
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.Index,
            _currentTrack.ValueUnsafe().Value.Index.ToString()));
        //page index
        keys.Add(new SpotifyContextTrackKey(SpotifyContextTrackKeyType.PageIndex,
            _currentPage.ValueUnsafe().Value.Index.ToString()));
        var waveeContextStream = new WaveeContextStream(waveeStream, ComposedKey.FromKeys(keys));
        CurrentStream = Option<WaveeContextStream>.Some(waveeContextStream);
        return Option<WaveeContextStream>.Some(waveeContextStream);
    }

    public ValueTask<Option<WaveeContextStream>> GetPreviousStream()
    {
        throw new NotImplementedException();
    }

    public Option<WaveeContextStream> CurrentStream { get; private set; }

    public async ValueTask<bool> TrySkip(int count)
    {
        if (count is 0)
            return true;
        if (count < 0)
        {
            return false; // Invalid count, cannot skip non-positive number of tracks
        }

        if (_startIndex.IsSome)
        {
            var startIndexVal = await _startIndex.ValueUnsafe();
            count += startIndexVal;
        }

        var currentPage = _currentPage;
        var currentTrack = _currentTrack;
        // Iterate through pages and tracks to skip the specified count
        while (count > 0)
        {
            if (currentPage == None || currentTrack == None)
            {
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }

            var remainingTracksInPage = currentPage.ValueUnsafe().Value.Tracks.Count -
                                        (currentTrack.ValueUnsafe().Value.Index + 1);
            if (count <= remainingTracksInPage)
            {
                // We can skip the remaining count within the current page
                for (int i = 0; i < count; i++)
                {
                    currentTrack = currentTrack.ValueUnsafe().Next;
                    if (currentTrack == null)
                    {
                        // Reached the end of the current page, fetch the next page
                        var nextPage = await NextPage();
                        if (nextPage.IsSome)
                        {
                            currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                            _currentPage = currentPage;
                            currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                            _currentTrack = currentTrack;
                        }
                        else
                        {
                            // No more pages, no more tracks to skip
                            return false;
                        }
                    }
                }

                count = 0; // We have successfully skipped the required tracks
            }
            else
            {
                // Skip the remaining tracks in the current page
                count -= remainingTracksInPage;

                // Move to the next page
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }
        }

        // Set the updated current track
        _currentTrack = currentTrack;

        return true;
    }

    public async ValueTask<bool> TryPeek(int count)
    {
        if (count <= 0)
        {
            return false; // Invalid count, cannot skip non-positive number of tracks
        }

        var currentPage = _currentPage;
        var currentTrack = _currentTrack;
        var pagesCopy = new LinkedList<SpotifyContextPage>(_pages);

        if (_startIndex.IsSome)
        {
            var startIndexVal = await _startIndex.ValueUnsafe();
            count += startIndexVal;
        }

        // Iterate through pages and tracks to skip the specified count
        while (count > 0)
        {
            if (currentPage == None || currentTrack == None)
            {
                var nextPage = await PeekNextPage();
                if (nextPage.IsSome)
                {
                    currentPage = pagesCopy.AddLast(nextPage.ValueUnsafe());
                    //_currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    //_currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }

            var remainingTracksInPage = currentPage.ValueUnsafe().Value.Tracks.Count -
                                        (currentTrack.ValueUnsafe().Value.Index + 1);
            if (count <= remainingTracksInPage)
            {
                // We can skip the remaining count within the current page
                for (int i = 0; i < count; i++)
                {
                    currentTrack = currentTrack.ValueUnsafe().Next;
                    if (currentTrack == null)
                    {
                        // Reached the end of the current page, fetch the next page
                        var nextPage = await PeekNextPage();
                        if (nextPage.IsSome)
                        {
                            currentPage = pagesCopy.AddLast(nextPage.ValueUnsafe());
                            //_currentPage = currentPage;
                            currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                            //   _currentTrack = currentTrack;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                count = 0; // We have successfully skipped the required tracks
            }
            else
            {
                // Skip the remaining tracks in the current page
                count -= remainingTracksInPage;

                // Move to the next page
                var nextPage = await PeekNextPage();
                if (nextPage.IsSome)
                {
                    currentPage = pagesCopy.AddLast(nextPage.ValueUnsafe());
                    // _currentPage = currentPage;
                    currentTrack = currentPage.ValueUnsafe().Value.Tracks.First;
                    // _currentTrack = currentTrack;
                }
                else
                {
                    return false;
                }
            }
        }

        // Set the updated current track
        //   _currentTrack = currentTrack;

        return true;
    }

    public async ValueTask<bool> MoveTo(int index)
    {
        if (index < 0)
        {
            return false;
        }

        //we will always move one too much because FetchNextStream will move to the next track
        if (index is 0)
        {
            _currentPage = _pages.First;
            _currentTrack = null;
            return true;
        }
        else
        {
            index -= 1;
        }

        //Keep in mind, index is an absolute index, not relative to the current page
        //meanaing we might have 20 pages, and index 100, which means we need to fetch 5 pages
        LinkedListNode<SpotifyContextPage>? currentPage = _pages.First;
        LinkedListNode<SpotifyContextTrack>? currentTrack = currentPage?.Value.Tracks?.First;
        while (index > 0)
        {
            if (currentPage == null || currentTrack == null)
            {
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }

            var remainingTracksInPage = currentPage.Value.Tracks.Count -
                                        (currentTrack.Value.Index + 1);
            if (index <= remainingTracksInPage)
            {
                // We can skip the remaining count within the current page
                for (int i = 0; i < index; i++)
                {
                    currentTrack = currentTrack.Next;
                    if (currentTrack == null)
                    {
                        // Reached the end of the current page, fetch the next page
                        var nextPage = await NextPage();
                        if (nextPage.IsSome)
                        {
                            currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                            _currentPage = currentPage;
                            currentTrack = currentPage.Value.Tracks.First;
                            _currentTrack = currentTrack;
                        }
                        else
                        {
                            // No more pages, no more tracks to skip
                            return false;
                        }
                    }
                }

                index = 0; // We have successfully skipped the required tracks
            }
            else
            {
                // Skip the remaining tracks in the current page
                index -= remainingTracksInPage;

                // Move to the next page
                var nextPage = await NextPage();
                if (nextPage.IsSome)
                {
                    currentPage = _pages.AddLast(nextPage.ValueUnsafe());
                    _currentPage = currentPage;
                    currentTrack = currentPage.Value.Tracks.First;
                    _currentTrack = currentTrack;
                }
                else
                {
                    // No more pages, no more tracks to skip
                    return false;
                }
            }
        }

        _currentTrack = currentTrack;
        return true;
    }
}

internal readonly record struct SpotifyContextPage(LinkedList<SpotifyContextTrack> Tracks, uint Index);

internal readonly record struct SpotifyContextTrack(SpotifyId Gid, Option<string> Uid, int Index);