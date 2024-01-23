using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Contexting;
using Wavee.Spfy.Remote;

namespace Wavee.Spfy.Playback.Contexts;

internal abstract class SpotifyRealContext : ISpotifyContext
{
    protected readonly Guid _connectionId;
    private readonly Func<SpotifyId, CancellationToken, Task<WaveeStream>> _streamFactory;

    private ActiveSpotifyContextPage? _activePage;
    
    private readonly LinkedList<SpotifyContextPage> _pagesCache = new();

    protected SpotifyRealContext(Guid connectionId,
        string contextUri,
        string contextUrl,
        Func<SpotifyId, CancellationToken, Task<WaveeStream>> streamFactory)
    {
        _connectionId = connectionId;
        _streamFactory = streamFactory;
        ContextUri = contextUri;
        ContextUrl = contextUrl;
    }

    public string ContextUri { get; }
    public string ContextUrl { get; }
    public abstract HashMap<string, string> ContextMetadata { get; }
    protected abstract ValueTask<Option<SpotifyContextPage>> NextPage();
    public async ValueTask<Option<WaveeContextStream>> GetNextStream()
    {
        var nextTrack = await GetNextTrack();
         if (nextTrack.IsNone)
        {
            return Option<WaveeContextStream>.None;
        }
        
        var track = nextTrack.ValueUnsafe();
        var stream = await _streamFactory(track.Gid, CancellationToken.None);
        var ctxStream = new WaveeContextStream(stream,
            Common.ConstructComposedKeyForCurrentTrack(track, track.Gid));
        CurrentStream = Option<WaveeContextStream>.Some(ctxStream);
        return CurrentStream;
    }

    private async ValueTask<Option<SpotifyContextTrack>> GetNextTrack()
    {
        var activePage = _activePage;
        if (activePage is null)
        {
            // try to fetch the next page
            var nextPage = await NextPage();
            if (nextPage.IsNone)
            {
                return Option<SpotifyContextTrack>.None;
            }
            
            var newPage=  _pagesCache.AddLast(nextPage.ValueUnsafe());
            _activePage = new ActiveSpotifyContextPage(newPage);
            activePage = _activePage;
        }
        
        if(!activePage.TryMoveNext(out var track))
        {
            // try to fetch the next page
            var nextPage = await NextPage();
            if (nextPage.IsNone)
            {
                return Option<SpotifyContextTrack>.None;
            }
            
            var newPage=  _pagesCache.AddLast(nextPage.ValueUnsafe());
            _activePage = new ActiveSpotifyContextPage(newPage);
            return await GetNextTrack();
        }
        
        return track.Value;
    }
    public ValueTask<Option<WaveeContextStream>> GetPreviousStream()
    {
        throw new NotImplementedException();
    }

    public Option<WaveeContextStream> CurrentStream { get; private set; }

    public async ValueTask<bool> TrySkip(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var nextTrack = await GetNextTrack();
            if (nextTrack.IsNone)
            {
                return false;
            }
        }

        return true;
    }
    
    private sealed class ActiveSpotifyContextPage
    {
        public ActiveSpotifyContextPage(LinkedListNode<SpotifyContextPage> currentPage)
        {
            CurrentPage = currentPage;
            CurrentTrack = currentPage.Value.Tracks.First;
        }

        public bool IsDone { get; private set; }
        public LinkedListNode<SpotifyContextPage> CurrentPage { get; }
        public LinkedListNode<SpotifyContextTrack>? CurrentTrack { get; private set; }

        public bool TryMoveNext(out SpotifyContextTrack? track)
        {
            if (CurrentTrack is null)
            {
                track = null;
                IsDone = true;
                return false;
            }

            track = CurrentTrack.Value;
            CurrentTrack = CurrentTrack.Next;
            return true;
        }
    }
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