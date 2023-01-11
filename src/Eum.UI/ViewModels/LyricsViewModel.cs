using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using Eum.Logging;
using Eum.UI.Items;
using Eum.UI.Services;
using Eum.UI.ViewModels.Playback;
using Timer = System.Timers.Timer;

namespace Eum.UI.ViewModels
{
    [INotifyPropertyChanged]
    public partial class LyricsLineViewModel
    {
        [ObservableProperty] private bool _isActive;
        public string Words { get; init; }
        public double StartsAt { get; init; }
        public int Index { get; init; }
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
        private readonly IDispatcherHelper _dispatcherHelper;
        public LyricsViewModel(ILyricsProvider lyricsProvider,
            PlaybackViewModel playbackViewModel,
            IDispatcherHelper dispatcherHelper)
        {
            _lyricsProvider = lyricsProvider;
            _playbackViewModel = playbackViewModel;
            _dispatcherHelper = dispatcherHelper;
            _timer = new Timer(TimeSpan.FromMilliseconds(20));
            _timer.Elapsed += TimerOnElapsed;

            _playbackViewModel.Seeked += PlaybackViewModelOnSeeked;
            _playbackViewModel.Paused += PlaybackViewModelOnPaused;
            _playbackViewModel.PlayingItemChanged += PlaybackViewModelOnPlayingItemChanged;
            PlaybackViewModelOnPlayingItemChanged(_playbackViewModel, _playbackViewModel.Item.Id);
        }

        public void Deconstruct()
        {
            _timer.Stop();
            _timer.Dispose();

            _playbackViewModel.Seeked -= PlaybackViewModelOnSeeked;
            _playbackViewModel.Paused -= PlaybackViewModelOnPaused;
            _playbackViewModel.PlayingItemChanged -= PlaybackViewModelOnPlayingItemChanged;
        }
        private void PlaybackViewModelOnSeeked(object sender, double e)
        {
            _ms = e;
            //TODO: invoke to go to accurate lyrics line
            var closestLyrics = FindClosestLyricsLine(e);
            if (closestLyrics != null)
            {
                _dispatcherHelper.TryEnqueue(QueuePriority.High, () => { SeekToLyrics(closestLyrics); });
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
                _dispatcherHelper.TryEnqueue(QueuePriority.High, () => { SeekToLyrics(closestLyrics); });
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

        private ItemId _previousLyricsId;
        private async void PlaybackViewModelOnPlayingItemChanged(object sender, ItemId e)
        {
            if (e != _previousLyricsId)
            {
                _previousLyricsId = e;
                await TryFetchLyrics(e);
                _timer.Stop();
                _ms = (sender as PlaybackViewModel).Timestamp;
                _timer.Start();
            }
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
                        Index = i,
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

    public interface IDispatcherHelper
    {
        bool TryEnqueue(QueuePriority priority, Action callback);
    }

    public enum QueuePriority
    {
        /// <summary>**Low** priority work will be scheduled when there isn't any other work to process. Work at **Low** priority can be preempted by new incoming **High** and **Normal** priority tasks.</summary>
        Low = -10, // 0xFFFFFFF6

        /// <summary>Work will be dispatched once all **High** priority tasks are dispatched. If a new **High** priority work is scheduled, all new **High** priority tasks are processed before resuming **Normal** tasks. This is the default priority.</summary>
        Normal = 0,

        /// <summary>Work scheduled at **High** priority will be dispatched first, along with other **High** priority System tasks, before processing **Normal** or **Low** priority work.</summary>
        High = 10, // 0x0000000A
    }
}