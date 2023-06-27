using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using Wavee.UI.Client.Playback;
using Wavee.UI.User;
using ReactiveUI;
using System.Reactive;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.UI.ViewModel.Playback;

public sealed class PlaybackViewModel : ObservableObject
{
    private readonly IDisposable _subscription;
    private bool _hasPlayback;
    private string? _title;
    private string? _largeImageUrl;
    private string? _smallImageUrl;

    public PlaybackViewModel(UserViewModel user)
    {
        _subscription = user.Client.Playback
            .PlaybackEvents
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(OnPlaybackEvent)
            .Subscribe();
    }

    public bool HasPlayback
    {
        get => _hasPlayback;
        set => SetProperty(ref _hasPlayback, value);
    }

    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string? LargeImageUrl
    {
        get => _largeImageUrl;
        set => SetProperty(ref _largeImageUrl, value);
    }

    public string? SmallImageUrl
    {
        get => _smallImageUrl;
        set => SetProperty(ref _smallImageUrl, value);
    }

    private Unit OnPlaybackEvent(WaveeUIPlaybackState state)
    {
        HasPlayback = state.PlaybackState > 0;
        if (state.Metadata.IsSome)
        {
            var metadata = state.Metadata.ValueUnsafe();
            Title = metadata.Title;
            LargeImageUrl = metadata.LargeImageUrl;
            SmallImageUrl = metadata.SmallImageUrl;
        }
        return Unit.Default;
    }
}