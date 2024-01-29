using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using LanguageExt.Pretty;
using Wavee.UI.Providers;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.ViewModels.NowPlaying;

public sealed class LyricsViewModel : RightSidebarComponentViewModel
{
    private readonly NowPlayingViewModel _nowPlayingViewModel;
    private readonly IDispatcher _dispatcher;
    private Guid? _callback;
    private LinkedList<LyricsLineViewModel>? _linesLinked;
    private LinkedListNode<LyricsLineViewModel>? _activeLine;
    private List<LyricsLineViewModel>? _lines;
    private readonly SemaphoreSlim _linesLock = new SemaphoreSlim(1, 1);
    public LyricsViewModel(NowPlayingViewModel nowPlayingViewModel, IDispatcher dispatcher)
    {
        _nowPlayingViewModel = nowPlayingViewModel;
        _dispatcher = dispatcher;
    }

    public List<LyricsLineViewModel>? Lines
    {
        get => _lines;
        set
        {
            if (SetProperty(ref _lines, value))
            {
                this.OnPropertyChanged(nameof(HasLyrics));
                if (value is null)
                {
                    _linesLinked = null;
                    _activeLine = null;
                }
                else
                {
                    _linesLinked = new LinkedList<LyricsLineViewModel>(value);
                }
            }
        }
    }

    public LinkedListNode<LyricsLineViewModel>? ActiveLine
    {
        get => _activeLine;
        set
        {
            if (SetProperty(ref _activeLine, value)) ;
        }
    }
    public bool HasLyrics => _lines?.Count > 0;

    public int ActiveLineIndex => ActiveLine?.Value?.Index ?? -1;

    public override async ValueTask Opened()
    {
        await LoadLyricsForTrack(_nowPlayingViewModel.CurrentTrack.Item, _nowPlayingViewModel.CurrentTrack.Profile);
        _nowPlayingViewModel.PropertyChanged += NowPlayingViewModelOnPropertyChanged;
        _callback = _nowPlayingViewModel.RegisterTimerCallback(PositionCallback);
    }

    public override ValueTask Closed()
    {
        _nowPlayingViewModel.PropertyChanged -= NowPlayingViewModelOnPropertyChanged;
        if (_callback is not null)
        {
            _nowPlayingViewModel.ClearPositionCallback(_callback.Value);
            _callback = null;
        }

        return ValueTask.CompletedTask;
    }

    private async void NowPlayingViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NowPlayingViewModel.CurrentTrack))
        {
            await LoadLyricsForTrack(_nowPlayingViewModel.CurrentTrack.Item, _nowPlayingViewModel.CurrentTrack.Profile);
        }
    }
    private void PositionCallback()
    {
        _linesLock.Wait();
        try
        {
            //this callback is called every 10ms
            //we can use this to determine the next lyrical line..
            if (!HasLyrics) return;

            var position = _nowPlayingViewModel?.Position.TotalMilliseconds ?? 0;
            var nextLine = _lines?.FirstOrDefault(line => line.Position.TotalMilliseconds > position);
            var newActiveLine = nextLine != null ? _linesLinked?.Find(nextLine)?.Previous : _linesLinked?.Last;
            if (newActiveLine is null && ActiveLine is not null)
            {
                ActiveLine = null;
                this.OnPropertyChanged(nameof(ActiveLineIndex));
                return;
            }

            if (newActiveLine != null && newActiveLine != _activeLine)
            {
                _dispatcher.Dispatch(() =>
                {
                    if (ActiveLine != null)
                    {
                        ActiveLine.Value.IsActive = false;
                    }

                    ActiveLine = newActiveLine;
                    ActiveLine!.Value.IsActive = true;
                    this.OnPropertyChanged(nameof(ActiveLineIndex));
                    foreach (var line in _lines)
                    {
                        line.NotifyChangedThis();
                    }

                    Debug.WriteLine(ActiveLine.Value.Text);
                }, true);
            }
        }
        catch (Exception x)
        {

        }
        finally
        {
            _linesLock.Release();
        }
    }
    private async Task LoadLyricsForTrack(IWaveePlayableItem? currentTrackItem, IWaveeUIAuthenticatedProfile? profile)
    {
        try
        {
            await _linesLock.WaitAsync();
            if (currentTrackItem is null)
            {
                Lines = null;
                ActiveLine = null;
                this.OnPropertyChanged(nameof(ActiveLineIndex));
                return;
            }

            try
            {
                var lyrics = await Task.Run(async () => await profile.GetLyricsFor(currentTrackItem.Id, currentTrackItem.Images.Head.Url))
                    .ConfigureAwait(false);

                _dispatcher.Dispatch(() =>
                {
                    Lines = lyrics.OrderBy(x => x.Position)
                        .Select((x, i) => new LyricsLineViewModel(x.Text, x.Position, i)).ToList();
                }, true);
            }
            catch (Exception x)
            {
                Lines = null;
                ActiveLine = null;
                this.OnPropertyChanged(nameof(ActiveLineIndex));
                return;
            }
        }
        catch (Exception x)
        {

        }
        finally
        {
            _linesLock.Release();
        }
    }
}

public sealed class LyricsLineViewModel : ObservableObject
{
    private bool _isActive;

    public LyricsLineViewModel(string text, TimeSpan position, int index)
    {
        Text = text;
        Position = position;
        Index = index;
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public TimeSpan Position { get; }
    public string Text { get; }
    public int Index { get; }
    public LyricsLineViewModel This => this;

    public void NotifyChangedThis()
    {
        this.OnPropertyChanged(nameof(This));
    }
}