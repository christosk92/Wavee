using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wavee.Enums;
using Wavee.Playback.Models;
using Wavee.UI.Interfaces.Playback;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Playback.Player;

namespace Wavee.UI.Playback.PlayerHandlers;

internal class LocalPlayerHandler : PlayerViewHandlerInternal
{
    private IPlayContext? _context;

    private readonly ILogger<LocalPlayerHandler>? _logger;
    private readonly LinkedList<LocalTrack?> _trackQueue;
    private readonly IMusicPlayer _musicPlayer;

    private Stack<int> _shuffleStack;
    private LocalTrack? _currentQueueFile;
    private int _currentTrackIndex;
    private bool _isPlayingQueue;
    private bool _isShuffling;
    private RepeatState _repeatState;

    public LocalPlayerHandler(IMusicPlayer musicPlayer, ILogger<LocalPlayerHandler>? logger = null) : base()
    {
        _repeatState = RepeatState.None;
        _shuffleStack = new Stack<int>();
        _trackQueue = new LinkedList<LocalTrack?>();
        _musicPlayer = musicPlayer;
        _logger = logger;

        _musicPlayer.TrackEnded += MusicPlayerOnTrackEnded;
        _musicPlayer.TrackStarted += MusicPlayerOnTrackStarted;
        _musicPlayer.PauseChanged += MusicPlayerOnPauseChanged;
        _musicPlayer.TimeChanged += MusicPlayerOnTimeChanged;
        _musicPlayer.VolumeChanged += MusicPlayerOnVolumeChanged;
    }


    public override TimeSpan Position => _musicPlayer.Position;
    public override bool Paused => _musicPlayer.Paused;
    public override double Volume => _musicPlayer.Volume;

    public async override Task LoadTrackListButDoNotPlay(IPlayContext context, int index)
    {
        EventsWriter.TryWrite(new ContextChangedEvent(context));
        _shuffleStack.Clear();
        _context = context;
        _currentTrackIndex = index;

        var track = (LocalTrack?)_context!.GetTrack(_currentTrackIndex);

        await _musicPlayer.LoadTrack(track.Value.Id);
        _musicPlayer.Pause();
    }
    public async override Task LoadTrackList(IPlayContext context, int index)
    {
        EventsWriter.TryWrite(new ContextChangedEvent(context));
        _shuffleStack.Clear();
        _context = context;
        _currentTrackIndex = index;

        var track = (LocalTrack?)_context!.GetTrack(_currentTrackIndex);

        await _musicPlayer.LoadTrack(track.Value.Id);

        _musicPlayer.Resume();
    }

    public override ValueTask Seek(double position)
    {
        _musicPlayer.Seek(TimeSpan.FromMilliseconds(position));
        return ValueTask.CompletedTask;
    }

    public override ValueTask Resume()
    {
        _musicPlayer.Resume();
        return ValueTask.CompletedTask;
    }

    public override ValueTask Pause()
    {
        _musicPlayer.Pause();
        return ValueTask.CompletedTask;
    }

    public async override ValueTask SkipNext()
    {
        await Go(TrackIndexDirection.Forward);
    }

    public async override ValueTask SkipPrevious()
    {
        await Go(TrackIndexDirection.Backward);
    }

    public override ValueTask ToggleShuffle()
    {
        if (_isShuffling)
        {
            _shuffleStack.Clear();
            _isShuffling = false;
        }
        else
        {
            _shuffleStack.Clear();
            _isShuffling = true;
        }
        return EventsWriter.WriteAsync(new ShuffleToggledEvent(_isShuffling));
    }

    public override ValueTask GoShuffle(bool shuffle)
    {
        if (_isShuffling == shuffle) return ValueTask.CompletedTask;
        _isShuffling = shuffle;
        return EventsWriter.WriteAsync(new ShuffleToggledEvent(_isShuffling));
    }

    public override ValueTask GoNextRepeatState()
    {
        if (_repeatState == RepeatState.None)
        {
            //to context
            _repeatState = RepeatState.Context;
        }
        else if (_repeatState == RepeatState.Context)
        {
            //to track
            _repeatState = RepeatState.Track;
        }
        else if (_repeatState == RepeatState.Track)
        {
            //to none
            _repeatState = RepeatState.None;
        }

        return EventsWriter.WriteAsync(new RepeatStateChangedEvent(_repeatState));
    }

    public override ValueTask GoToRepeatState(RepeatState repeatState)
    {
        if (_repeatState == repeatState)
            return ValueTask.CompletedTask;

        _repeatState = repeatState;
        return EventsWriter.WriteAsync(new RepeatStateChangedEvent(_repeatState));
    }

    public override ValueTask SetVolume(double d)
    {
        _musicPlayer.SetVolume(d);
        return ValueTask.CompletedTask;
    }

    private async void MusicPlayerOnTrackStarted(object? sender, PreviousTrackData? e)
    {
        if (e != null)
        {
            var db = Ioc.Default.GetRequiredService<IPlaycountService>();
            await db.IncrementPlayCount(e.Value.Id, e.Value.StartedAt, e.Value.RealDuration, CancellationToken.None);
        }

        var track = _isPlayingQueue ? _currentQueueFile! : _context!.GetTrack(_currentTrackIndex);

        EventsWriter.TryWrite(new TrackChangedEvent(track, _currentTrackIndex));
    }

    private void MusicPlayerOnPauseChanged(object? sender, bool e)
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
    private void MusicPlayerOnVolumeChanged(object? sender, double e)
    {
        EventsWriter.TryWrite(new VolumeChangedEvent(e));
    }
    private async void MusicPlayerOnTrackEnded(object? sender, EventArgs e)
    {
        if (_repeatState == RepeatState.Track)
        {
            //repeat track
            if (_context!.GetTrack(_currentTrackIndex) is LocalTrack track)
                await _musicPlayer.LoadTrack(track.Id);
        }
        else
        {
            //go to next track
            await SkipNext();
        }
    }

    private void MusicPlayerOnTimeChanged(object? sender, TimeSpan e)
    {
        EventsWriter.TryWrite(new SeekedEvent((ulong)e.TotalMilliseconds));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="direction"></param>
    /// <returns> larger than or 0 = track, -1 is queue, -2 is stop</returns>
    private int GetNextTrackIndex(TrackIndexDirection direction)
    {
        switch (direction)
        {
            case TrackIndexDirection.Forward:
                if (_trackQueue.Count > 0) return -1;

                if (_isShuffling)
                {
                    //random track
                    //add current track to stack if not -1
                    if (_currentTrackIndex != -1)
                    {
                        _shuffleStack.Push(_currentTrackIndex);
                    }

                    var random = new Random().Next(0, _context.Length - 1);
                    return random;
                }

                if (_currentTrackIndex < _context!.Length - 1)
                {

                    var r = _currentTrackIndex + 1;
                    return r;
                }
                else
                {
                    if (_repeatState == RepeatState.Context)
                        return 0;
                    return -2;
                }

            case TrackIndexDirection.Backward:
                if (_currentTrackIndex > 0 || _isShuffling)
                {
                    if (_isShuffling)
                    {
                        if (_shuffleStack.Count > 0)
                        {
                            return _shuffleStack.Pop();
                        }
                        else
                        {
                            return _currentTrackIndex;
                        }
                    }

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

    private async Task Go(TrackIndexDirection position)
    {
        try
        {
            _currentTrackIndex = _currentTrackIndex == -1 ? 0 : GetNextTrackIndex(position);
            LocalTrack? track;
            if (_currentTrackIndex == -1)
            {
                _isPlayingQueue = true;
                track = _trackQueue.First!.Value;
                _trackQueue.Remove(track);
            }
            else if (_currentTrackIndex == -2)
            {
                //reset to 0 but do not paly
                _currentTrackIndex = 0;
                _currentQueueFile = null;
                track = (LocalTrack?)_context!.GetTrack(_currentTrackIndex);
                await _musicPlayer.LoadTrack(track.Value.Id);
                _musicPlayer.Pause();
            }
            else
            {
                _isPlayingQueue = false;
                _currentQueueFile = null;
                track = (LocalTrack?)_context!.GetTrack(_currentTrackIndex);
                await _musicPlayer.LoadTrack(track.Value.Id);
                _musicPlayer.Resume();
            }
        }
        catch (Exception x)
        {
            _logger?.LogError(x, "An error occurred while trying to go to the next track.");
        }
    }
}