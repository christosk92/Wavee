using CommunityToolkit.Mvvm.ComponentModel;
using Eum.Spotify.playlist4;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Wavee.UI.Client.Lyrics;
using Wavee.UI.Client.Playback;
using Wavee.UI.ViewModel.Playback;
using Timer = System.Threading.Timer;
using ReactiveUI;
using System.Reactive.Linq;
using System.Security.Cryptography;

namespace Wavee.UI.ViewModel.Shell.Lyrics;

public partial class LyricsViewModel : ObservableObject
{
    private const int Interval = 20;
    [ObservableProperty] private bool _hasLyrics;
    [ObservableProperty] private bool _loading;
    [ObservableProperty] private List<LyricsLineViewModel> _lyrics;
    [ObservableProperty] private LyricsLineViewModel? _activeLyricsLine;
    private PlaybackViewModel _playbackViewModel;
    private readonly IWaveeUILyricsClient _lyricsClient;
    private readonly Action<Action> _invokeOnUiThread;
    private string? _currentTrackId;
    private Timer _timer;
    private double _ms;
    private IDisposable _subscription;
    private readonly PlaybackViewModel playbackViewModel;
    public LyricsViewModel(IWaveeUILyricsClient lyricsProvider, PlaybackViewModel playbackViewModel,
        Action<Action> _invokeOnUiThread)
    {
        _lyricsClient = lyricsProvider;
        this.playbackViewModel = playbackViewModel;
        _playbackViewModel = playbackViewModel;
        this._invokeOnUiThread = _invokeOnUiThread;
        _timer = new Timer(Callback);
        //TimeSpan.FromMilliseconds(20)
    }

    public void Create()
    {
        Destroy();
        _subscription = playbackViewModel
            .CreateListener()
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(async t => await OnPlaybackEvent(t))
            .Subscribe();
    }

    public void Destroy()
    {
        _subscription?.Dispose();
        //clear lyrics
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        Lyrics = new List<LyricsLineViewModel>(0);
    }
    private void Callback(object? state)
    {
        _ms += 20;
        if (ShouldChangeLyricsLine(_ms))
        {
            var closestLyrics = FindClosestLyricsLine(_ms);
            _invokeOnUiThread(() =>
            {
                SeekToLyrics(closestLyrics);
            });
        }
    }

    public async Task<Unit> OnPlaybackEvent(WaveeUIPlaybackState state)
    {
        if (state.Metadata.IsNone || state.PlaybackState is WaveeUIPlayerState.NotPlayingAnything)
        {
            //clear
            return Unit.Default;
        }
        var metadata = state.Metadata.ValueUnsafe();
        if (!string.Equals(metadata.Id, _currentTrackId))
        {
            _currentTrackId = metadata.Id;
            var result = await TryFetchLyrics(metadata.Id, CancellationToken.None);
            if (result is null or false)
            {
                return Unit.Default;
            }
        }

        if (state.PlaybackState is WaveeUIPlayerState.Paused)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        else if (state.PlaybackState is WaveeUIPlayerState.Playing)
        {
            _timer.Change(0, Interval);
        }

        _ms = state.Position.TotalMilliseconds;
        //TODO: invoke to go to accurate lyrics line
        var closestLyrics = FindClosestLyricsLine(state.Position.TotalMilliseconds);

        _invokeOnUiThread(() =>
        {
            SeekToLyrics(closestLyrics);
        });
        return Unit.Default;
    }


    public async Task<bool?> TryFetchLyrics(string actualId, CancellationToken ct = default)
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        Loading = true;
        try
        {
            var lines = await _lyricsClient.GetLyrics(actualId, ct);
            if (lines.Length > 0)
            {
                Lyrics = lines.Select((a, i) => new LyricsLineViewModel
                {
                    Words = a.Words,
                    IsActive = i == 0,
                    StartsAt = a.StartTimeMs,
                }).ToList();
                HasLyrics = true;
                _ms = _playbackViewModel.GetPosition();
                if (!_playbackViewModel.Paused)
                {
                    _timer.Change(0, Interval);
                    // _timer.Start();
                }

                return true;
            }
            else
            {
                HasLyrics = false;
                return false;
            }
        }
        catch (Exception x)
        {
            HasLyrics = false;
            Log.Error(x, "An error occurred while fetching lyrics.");
            return null;
        }
        finally
        {
            Loading = false;
        }
    }

    private bool ShouldChangeLyricsLine(double ms)
    {
        //check if the current line is the last one
        if (_lyrics == null || _activeLyricsLine == _lyrics[^1])
        {
            return false;
        }

        //We advance to the next lyrics line if the current one is over with a small margin of 10ms.
        //get the next line and compare the start time with the current time
        var nextLine = _lyrics[_lyrics.IndexOf(_activeLyricsLine) + 1];
        //if the small margin exceeds the difference, just say yes (but only if > 0)
        if (nextLine.StartsAt - ms < 10)
        {
            return true;
        }
        return false;
    }

    private LyricsLineViewModel FindClosestLyricsLine(double ms)
    {
        //with a margin of 10ms
        var closest = _lyrics.OrderBy(x => Math.Abs(x.StartsAt - ms)).First();
        return closest;
    }

    private void SeekToLyrics(LyricsLineViewModel l)
    {
        foreach (var lr in Lyrics)
        {
            lr.IsActive = l == lr;
        }

        ActiveLyricsLine = l;
    }
}