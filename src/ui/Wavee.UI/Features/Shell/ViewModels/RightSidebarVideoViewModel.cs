using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.Features.Playback.ViewModels;
using Wavee.Youtube;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Wavee.UI.Features.Shell.ViewModels;

public sealed class RightSidebarVideoViewModel : RightSidebarItemViewModel
{
    private bool _hasSetup;
    private Stream? _activeVideoStream;
    private VideoSearchResult? _activeVideo;
    private bool _loading;
    private bool _hasVideo;
    private string _currentTrackId;
    private TimeSpan _currentTime;

    public RightSidebarVideoViewModel(PlaybackViewModel playback, IWaveeYoutubeClient youtubeClient)
    {
        Playback = playback;
        Setup = new VideoSetupViewModel
        {
            AlwaysFetch = true
        };
        HasSetup = true;
        FetchActiveVideoCommand = new AsyncRelayCommand(async (CancellationToken ct) =>
        {
            if (!IsActive) 
                return;

            if (_currentTrackId == playback.ActivePlayer.Id)
            {
                // Seek
                CurrentTime = playback.ActivePlayer.Position;
                return;
            }

            _currentTrackId = playback.ActivePlayer.Id;

            HasSetup = false;
            try
            {
                Loading = true;
                ActiveVideoStream?.Dispose();

                var title = playback.ActivePlayer.Title;
                var artist = playback.ActivePlayer.Artists[0].Item2;

                const string format = "{0} {1} official music video";
                var query = string.Format(format, title, artist);
                // Make it json safe
                query = query.Replace("\"", "\\\"");

                var videosAsync = youtubeClient.SearchAsync(query, ct);
                const int maxCounts = 4;
                int count = 0;

                await using var enumerator = videosAsync.GetAsyncEnumerator(ct);
                await enumerator.MoveNextAsync();
                var video = enumerator.Current;
                
                // await using var enumerator = videosAsync.GetAsyncEnumerator(ct);
                // if (!await enumerator.MoveNextAsync())
                // {
                //     return;
                // }
                var stream = await youtubeClient.GetStreamAsync(video.Id, ct);
                ActiveVideoStream = stream;
                ActiveVideo = video;
            }
            catch (Exception e)
            {

            }
            finally
            {
                Loading = false;
            }
        });

        playback.PlaybackStateChanged += PlaybackOnPlaybackStateChanged;
    }

    public TimeSpan CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    private async void PlaybackOnPlaybackStateChanged(object? sender, EventArgs e)
    {
        await FetchActiveVideoCommand.ExecuteAsync(null);
    }

    public VideoSearchResult? ActiveVideo
    {
        get => _activeVideo;
        set => SetProperty(ref _activeVideo, value);
    }

    public Stream? ActiveVideoStream
    {
        get => _activeVideoStream;
        set
        {
            SetProperty(ref _activeVideoStream, value);

            HasVideo = value is not null;
        }
    }

    public bool HasSetup
    {
        get => _hasSetup;
        set => SetProperty(ref _hasSetup, value);
    }

    public bool Loading
    {
        get => _loading;
        set => SetProperty(ref _loading, value);
    }
    public VideoSetupViewModel Setup { get; }

    public AsyncRelayCommand FetchActiveVideoCommand { get; }

    public bool HasVideo
    {
        get => _hasVideo;
        set => SetProperty(ref _hasVideo, value);
    }

    public PlaybackViewModel Playback { get; }
    public bool IsActive { get; set; }
}

public sealed class VideoSetupViewModel : ObservableObject
{
    private bool _alwaysFetch;

    public bool AlwaysFetch
    {
        get => _alwaysFetch;
        set => SetProperty(ref _alwaysFetch, value);
    }
}