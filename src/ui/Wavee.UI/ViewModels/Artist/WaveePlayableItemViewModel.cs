using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;

namespace Wavee.UI.ViewModels.Artist;

public abstract class WaveePlayableItemViewModel : ObservableObject
{
    private WaveeUITrackPlaybackStateType _playbackState;
    private bool _isHovered;

    public WaveePlayableItemViewModel(ComposedKey id, ICommand playCommand)
    {
        PlayCommand = playCommand;
        Id = id;
    }
    public ComposedKey Id { get; }
    public WaveeUITrackPlaybackStateType PlaybackState
    {
        get => _playbackState;
        set => this.SetProperty(ref _playbackState, value);
    }
    public bool IsHovered
    {
        get => _isHovered;
        set => this.SetProperty(ref _isHovered, value);
    }
    public ICommand PlayCommand { get; }
    public abstract string Name { get; }

    public abstract bool Is(IWaveePlayableItem x, Option<string> uid, string contextId);
}