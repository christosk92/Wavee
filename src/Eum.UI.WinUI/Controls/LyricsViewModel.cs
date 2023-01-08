using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Logging;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.ViewModels.Playback;
using Microsoft.UI.Dispatching;
using Timer = System.Timers.Timer;

namespace Eum.UI.WinUI.Controls
{
    [INotifyPropertyChanged]
    public partial class LyricsLineViewModel
    {
        [ObservableProperty] private bool _isActive;
        public string Words { get; init; }
        public double StartsAt { get; init; }
        public double ToFontSize(bool o, double s, double s1)
        {
            if (o) return s;
            return s1;
        }
    }
    [INotifyPropertyChanged]
    public partial class LyricsViewModel
    {
        public bool CompositeNoLyricsAndNotLoading => !HasLyrics && !Loading;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CompositeNoLyricsAndNotLoading))]
        private bool _hasLyrics;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CompositeNoLyricsAndNotLoading))] 
        private bool _loading;
        [ObservableProperty] private List<LyricsLineViewModel> _lyrics;
        [ObservableProperty] private LyricsLineViewModel? _activeLyricsLine;
        private readonly ILyricsProvider _lyricsProvider;
        private PlaybackViewModel _playbackViewModel;
        private Timer _timer;
        private double _ms;
        private readonly DispatcherQueue _dispatcherQueue;
        public LyricsViewModel(ILyricsProvider lyricsProvider, PlaybackViewModel playbackViewModel)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _lyricsProvider = lyricsProvider;
            _playbackViewModel = playbackViewModel;
            _timer = new Timer(TimeSpan.FromMilliseconds(20));
            _timer.Elapsed += TimerOnElapsed;
            
            _playbackViewModel.Seeked += PlaybackViewModelOnSeeked;
            _playbackViewModel.Paused += PlaybackViewModelOnPaused;
            _playbackViewModel.PlayingItemChanged += PlaybackViewModelOnPlayingItemChanged;
        }

        private void PlaybackViewModelOnSeeked(object sender, double e)
        {
            _ms = e;
            //TODO: invoke to go to accurate lyrics line
            var closestLyrics = FindClosestLyricsLine(e);
            if (closestLyrics != null)
            {
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () => { SeekToLyrics(closestLyrics); });
            }
        }

        private void PlaybackViewModelOnPaused(object sender, bool e)
        {
            if (e)
            {
                _timer.Stop();
            }
            else
            {
                _timer.Start();
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _ms += 20;
            if (ShouldChangeLyricsLine(_ms))
            {
                var closestLyrics = FindClosestLyricsLine(_ms);
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
                {
                    SeekToLyrics(closestLyrics);
                });
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
            if (_lyrics != null)
            {
                var closest = _lyrics.OrderBy(x => Math.Abs(x.StartsAt - ms)).First();
                return closest;
            }

            return null;
        }

        private void SeekToLyrics(LyricsLineViewModel l)
        {
            foreach (var lr in Lyrics)
            {
                lr.IsActive = l == lr;
            }

            ActiveLyricsLine = l;
        }
        private void PlaybackViewModelOnPlayingItemChanged(object sender, ItemId e)
        {
            _timer.Stop();
            _ms = 0;
            _timer.Start();
        }

        public async Task TryFetchLyrics(ItemId actualId, CancellationToken ct = default)
        {
            _timer.Stop();
            Loading = true;
            try
            {
                var lines = await _lyricsProvider.GetLyrics(actualId, ct);
                if (lines != null)
                {
                 
                    Lyrics = lines.Select((a, i) => new LyricsLineViewModel
                    {
                        Words = a.Words,
                        IsActive = i == 0,
                        StartsAt = a.StartTimeMs,
                    }).ToList();
                    HasLyrics = true;
                    _ms = _playbackViewModel.Timestamp;
                    if (!_playbackViewModel.IsPaused)
                    {
                        _timer.Start();
                    }
                }
                else
                {
                    HasLyrics = false;
                }
            }
            catch (Exception x)
            {
                HasLyrics = false;
                S_Log.Instance.LogError(x);
            }
            finally
            {
                Loading = false;
            }
        }
    }
}
