using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Wavee.Player.NAudio;
using Wavee.UI.AudioImport;
using Wavee.UI.AudioImport.Database;
using Wavee.UI.ViewModels.AudioItems;
using Wavee.UI.ViewModels.Playback.PlayerEvents;

namespace Wavee.UI.ViewModels.Playback.Impl;

public interface ILocalContext : IPlayContext
{
    LocalAudioFile? GetTrack(int index);
    int Length
    {
        get;
    }
}

public interface IPlayContext
{
}

internal class LocalPlayerHandler : PlayerViewHandlerInternal
{
    private ILocalContext? _context;

    private readonly ILogger<LocalFilePlayer> _logger;
    private readonly LinkedList<LocalAudioFile?> _trackQueue;
    private readonly LocalFilePlayer _localFilePlayer;

    private LocalAudioFile? _currentQueueFile;
    private int _currentTrackIndex;
    private bool _isPlayingQueue;
    public LocalPlayerHandler(LocalFilePlayer localFilePlayer, ILogger<LocalFilePlayer> logger) : base()
    {
        _trackQueue = new LinkedList<LocalAudioFile?>();
        _localFilePlayer = localFilePlayer;
        _logger = logger;

        _localFilePlayer.TrackEnded += LocalFilePlayerOnTrackEnded;
        _localFilePlayer.TrackStarted += LocalFilePlayerOnTrackStarted;
        _localFilePlayer.PauseChanged += LocalFilePlayerOnPauseChanged;
        _localFilePlayer.TimeChanged += LocalFilePlayerOnTimeChanged;
    }

    public override TimeSpan Position => _localFilePlayer.Position;
    public override Task LoadTrackList(IPlayContext context)
    {
        var wasEmpty = _trackQueue.Count == 0 || _context is null || _context.Length == 0;
        if (context is not ILocalContext localContext) return Task.CompletedTask;

        _context = localContext;
        _currentTrackIndex = -1;
        GoNext();
        return Task.CompletedTask;
    }

    public override void Seek(double position)
    {
        _localFilePlayer.Seek(TimeSpan.FromMilliseconds(position));
    }

    private void LocalFilePlayerOnTrackStarted(object? sender, string e)
    {
        var track = _isPlayingQueue ? _currentQueueFile! : _context!.GetTrack(_currentTrackIndex);
        EventsWriter.TryWrite(new TrackChangedEvent(PlayingTrackView.From(track)));
    }
    private void LocalFilePlayerOnPauseChanged(object? sender, bool e)
    {
        if (e)
        {
            EventsWriter.TryWrite(new PausedEvent());
        }
        else
        {
            EventsWriter.TryWrite(new ResumedEvent());
        }
    }

    private void LocalFilePlayerOnTrackEnded(object? sender, EventArgs e)
    {
        GoNext();
    }
    private void LocalFilePlayerOnTimeChanged(object? sender, TimeSpan e)
    {
        EventsWriter.TryWrite(new SeekedEvent((ulong)e.TotalMilliseconds));
    }
    private int GetNextTrackIndex(TrackIndexDirection direction)
    {
        switch (direction)
        {
            case TrackIndexDirection.Forward:
                if (_trackQueue.Count > 0) return -1;

                if (_currentTrackIndex < _context!.Length - 1)
                {
                    var r = _currentTrackIndex + 1;
                    return r;
                }
                else
                {
                    return 0;
                }

            case TrackIndexDirection.Backward:
                if (_currentTrackIndex > 0)
                {
                    var r = _currentTrackIndex - 1;
                    return r;
                }
                else
                {
                    return _context!.Length - 1;
                }
            default:
                return (int)TrackIndexDirection.Error;
        }
    }
    private void GoNext()
    {
        try
        {
            _currentTrackIndex = GetNextTrackIndex(TrackIndexDirection.Forward);
            LocalAudioFile? track;
            if (_currentTrackIndex == -1)
            {
                _isPlayingQueue = true;
                track = _trackQueue.First!.Value;
                _trackQueue.Remove(track);
            }
            else
            {
                _isPlayingQueue = false;
                _currentQueueFile = null;
                track = _context!.GetTrack(_currentTrackIndex);
            }

            _localFilePlayer.LoadTrack(track.Path);
            _localFilePlayer.Resume();
        }
        catch (Exception x)
        {
            _logger?.LogError(x, "An error occurred while trying to go to the next track.");
        }
    }
}