using Eum.Spotify.context;
using Wavee.Core;
using Wavee.Core.Exceptions;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Playback.Contexting;

internal sealed class SpotifyPlayContext : IWaveePlayContext
{
    private readonly string _contextUri;
    private readonly string _contextUrl;
    private LinkedList<ContextPage>? _pages;
    private readonly ISpotifyClient _spotifyClient;

    public SpotifyPlayContext(string contextUri,
        string contextUrl,
        IReadOnlyList<ContextPage>? pages,
        ISpotifyClient spotifyClient)
    {
        _contextUri = contextUri;
        _contextUrl = contextUrl;
        _spotifyClient = spotifyClient;
        if (pages is not null)
        {
            _pages = new LinkedList<ContextPage>(pages);
        }
    }

    public async ValueTask<WaveeMediaSource?> GetAt(int index, CancellationToken cancellationToken = default)
    {
        if (_pages is null || _pages.Count == 0)
        {
            await InitializePages(cancellationToken);
        }

        int cumulativeIndex = 0;
        var currentNode = _pages?.First;
        while (currentNode != null)
        {
            var page = await EnsurePageLoadedAsync(currentNode, cancellationToken);
            if (page == null || page.Tracks == null)
            {
                currentNode = currentNode.Next;
                continue;
            }

            int nextPageCumulativeIndex = cumulativeIndex + page.Tracks.Count;
            if (index < nextPageCumulativeIndex)
            {
                int trackIndexInPage = index - cumulativeIndex;
                if (trackIndexInPage < page.Tracks.Count)
                {
                    var track = page.Tracks[trackIndexInPage];
                    return await CreateMediaSource(track, cancellationToken);
                }

                break;
            }

            cumulativeIndex = nextPageCumulativeIndex;
            currentNode = currentNode.Next;
        }

        return null;
    }

    public async Task<(int AbsoluteIndex, int IndexInPage, int PageIndex)> FindAsync(
        int? pageIndex = null,
        int? trackIndex = null,
        string? trackUid = null,
        SpotifyId? trackId = null,
        CancellationToken cancellationToken = default)
    {
        if (_pages is null || _pages.Count == 0)
        {
            await InitializePages(cancellationToken);
        }

        int absoluteIndex = 0;
        var currentNode = _pages?.First;
        int pageIndexCounter = 0;
        while (currentNode != null)
        {
            var page = await EnsurePageLoadedAsync(currentNode, cancellationToken);
            if (page == null || page.Tracks == null)
            {
                currentNode = currentNode.Next;
                pageIndexCounter++;
                continue;
            }

            // Priority 1: Direct index access if within bounds
            if (pageIndex.HasValue && pageIndex.Value == pageIndexCounter)
            {
                if (trackIndex.HasValue && trackIndex.Value < page.Tracks.Count)
                {
                    return (absoluteIndex + trackIndex.Value, trackIndex.Value, pageIndexCounter);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Track index is out of bounds.");
                }
            }

            // Priority 2 and 3: Search by trackUid or trackId
            for (int j = 0; j < page.Tracks.Count; j++)
            {
                var track = page.Tracks[j];
                if (trackUid != null && track.Uid == trackUid ||
                    trackId != null && (track.Uri == trackId.Value.ToString() ||
                                        track.Gid.Span.SequenceEqual(trackId.Value.Id.ToByteArray(true, true))))
                {
                    return (absoluteIndex + j, j, pageIndexCounter);
                }
            }

            absoluteIndex += page.Tracks.Count;
            currentNode = currentNode.Next;
            pageIndexCounter++;
        }

        // Priority 4: Default case, return the first track if available
        if (_pages.First.Value.Tracks == null || !_pages.First.Value.Tracks.Any())
        {
            throw new InvalidOperationException("No tracks available in the context.");
        }

        return (0, 0, 0);
    }

    private async Task<ContextPage?> FetchPageByNextPageUrl(string pageNextPageUrl, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<Context?> FetchPageByPageUrl(string pagePageUrl, CancellationToken cancellationToken)
    {
        var finalPageurl = pagePageUrl.Replace("hm://", string.Empty);
        var ctx = await _spotifyClient.Context.ResolveContext(finalPageurl, cancellationToken);
        return ctx;
    }

    private async Task<WaveeMediaSource?> CreateMediaSource(ContextTrack ctxTrack, CancellationToken cancellationToken)
    {
        SpotifyId? trackId = null;
        if (ctxTrack.HasUri && !string.IsNullOrEmpty(ctxTrack.Uri))
        {
            if (SpotifyId.TryParse(ctxTrack.Uri, out var id))
            {
                trackId = id;
            }
        }
        else if (ctxTrack.HasGid && !ctxTrack.Gid.IsEmpty)
        {
            //TODO: Episode
            trackId = SpotifyId.FromRaw(ctxTrack.Gid.Span, AudioItemType.Track);
        }

        if (trackId == null)
        {
            throw new CannotPlayException(CannotPlayException.Reason.UnsupportedMediaType);
        }

        var track = await _spotifyClient.Tracks.Get(trackId.Value, cancellationToken);
        var files = track.AudioFiles;
        if (files.Count is 0)
        {
            throw new CannotPlayException(CannotPlayException.Reason.NoAudioFiles);
        }

        var requestedFormat = SpotifyAudioFileType.OGG_VORBIS_320;
        var file = files.FirstOrDefault(f => f.Type == requestedFormat);
        if (file.FileId is null)
        {
            file = files.FirstOrDefault();
        }

        var encryptedFile = await SpotifyEncryptedStream.Open(this._spotifyClient, track, file);

        var isCached = encryptedFile.IsCached;

        // Not all audio files are encrypted. If we can't get a key, try loading the track
        // without decryption. If the file was encrypted after all, the decoder will fail
        // parsing and bail out, so we should be safe from outputting ear-piercing noise.
        var audioKey =
            await Task.Run(
                async () => await _spotifyClient.AudioKey.GetAudioKey(trackId.Value, file.FileId, cancellationToken),
                cancellationToken);

        var decryptedFile = SpotifyClient.DecryptionFactory!(encryptedFile, audioKey);
        var isOgg = file.Type switch
        {
            SpotifyAudioFileType.OGG_VORBIS_320 => true,
            SpotifyAudioFileType.OGG_VORBIS_160 => true,
            SpotifyAudioFileType.OGG_VORBIS_96 => true,
            _ => false
        };
        var offSetAndMutData = isOgg
            ? (SpotifyOggHeaderEnd, NormalisationDataHelper.ParseFromOgg(decryptedFile))
            : (0, null);


        var mediaSource = new SpotifyMediaSource(
            stream: decryptedFile,
            item: track,
            offset: (int)offSetAndMutData.Item1,
            normalisationData: offSetAndMutData.Item2);

        //read first 30 bytes
        var buffer = new byte[30];
        mediaSource.Seek(0, SeekOrigin.Begin);
        var read = mediaSource.Read(buffer);

        return mediaSource;
    }

    public const uint SpotifyOggHeaderEnd = 0xa7;

    private static int BytesPerSecond(SpotifyAudioFileType fileType)
    {
        var kbps = fileType switch
        {
            SpotifyAudioFileType.OGG_VORBIS_320 => 12,
            SpotifyAudioFileType.OGG_VORBIS_160 => 20,
            SpotifyAudioFileType.OGG_VORBIS_96 => 40,
            _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, null)
        };
        return kbps * 1024;
    }

    private async Task InitializePages(CancellationToken cancellationToken)
    {
        var context = await _spotifyClient.Context.ResolveContext(_contextUri, cancellationToken);
        _pages = new LinkedList<ContextPage>(context.Pages);
    }

    private async Task<ContextPage?> EnsurePageLoadedAsync(LinkedListNode<ContextPage> currentNode,
        CancellationToken cancellationToken)
    {
        var page = currentNode.Value;
        if (page.Tracks.Count > 0) return page; // Page is already loaded

        ContextPage? updatedPage = null;
        if (!string.IsNullOrEmpty(page.PageUrl))
        {
            var newContext = await FetchPageByPageUrl(page.PageUrl, cancellationToken);
            if (newContext != null)
            {
                var firstPage = newContext.Pages.FirstOrDefault();
                if (firstPage != null)
                {
                    updatedPage = firstPage;
                    foreach (var p in newContext.Pages.Skip(1))
                    {
                        _pages.AddAfter(currentNode, p);
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(page.NextPageUrl))
        {
            updatedPage = await FetchPageByNextPageUrl(page.NextPageUrl, cancellationToken);
            if (updatedPage != null)
            {
                _pages.AddAfter(currentNode, updatedPage);
            }
        }

        if (updatedPage != null)
        {
            currentNode.Value = updatedPage;
            return updatedPage;
        }

        return null;
    }
}