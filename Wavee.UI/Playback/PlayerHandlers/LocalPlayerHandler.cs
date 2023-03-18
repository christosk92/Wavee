using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Local;
using Wavee.UI.Playback.Player;

namespace Wavee.UI.Playback.PlayerHandlers;
internal class LocalPlayerHandler : PlayerViewHandlerInternal
{
    private IPlayContext? _context;

    private readonly ILogger<LocalPlayerHandler>? _logger;
    private readonly LinkedList<LocalTrack?> _trackQueue;
    private readonly ILocalFilePlayer _localFilePlayer;

    private LocalTrack? _currentQueueFile;
    private int _currentTrackIndex;
    private bool _isPlayingQueue;
    public LocalPlayerHandler(ILocalFilePlayer localFilePlayer, ILogger<LocalPlayerHandler>? logger = null) : base()
    {
        _trackQueue = new LinkedList<LocalTrack?>();
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

        _context = context;
        _currentTrackIndex = -1;
        GoNext();
        return Task.CompletedTask;
    }

    public override ValueTask Seek(double position)
    {
        _localFilePlayer.Seek(TimeSpan.FromMilliseconds(position));
        return ValueTask.CompletedTask;
    }

    public override ValueTask Resume()
    {
        _localFilePlayer.Resume();
        return ValueTask.CompletedTask;
    }

    public override ValueTask Pause()
    {
        _localFilePlayer.Pause();
        return ValueTask.CompletedTask;
    }

    private async void LocalFilePlayerOnTrackStarted(object? sender, string e)
    {
        var track = _isPlayingQueue ? _currentQueueFile! : _context!.GetTrack(_currentTrackIndex);

        if (track != null)
        {
            //increment playcount
            var db = Ioc.Default.GetRequiredService<IPlaycountService>();
            await db.IncrementPlayCount(track.Id);
        }

        EventsWriter.TryWrite(new TrackChangedEvent(track));
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
            LocalTrack? track;
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
                track = (LocalTrack?)_context!.GetTrack(_currentTrackIndex);
            }

            _localFilePlayer.LoadTrack(track.Value.Id);
            _localFilePlayer.Resume();
        }
        catch (Exception x)
        {
            _logger?.LogError(x, "An error occurred while trying to go to the next track.");
        }
    }
}
//"The call is ambiguous between the following methods or properties: 'System.Text.Json.JsonSerializer.Deserialize<string[]>(string, System.Text.Json.JsonSerializerOptions)' and 'System.Text.Json.JsonSerializer.Deserialize<string[]>(System.Text.Json.JsonDocument, System.Text.Json.JsonSerializerOptions)'"