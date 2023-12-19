
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;
using Wavee.Domain.Playback;

namespace Wavee.UI.Features.Album.ViewModels;

public sealed class AlbumTrackViewModel : ObservableObject
{
    private WaveeTrackPlaybackState _playbackState;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required TimeSpan Duration { get; init; }
    public required int Number { get; init; }
    public required ulong? Playcount { get; init; }
    public string DurationString
    {
        get
        {
            var totalHours = (int)Duration.TotalHours;
            var totalMinutes = (int)Duration.TotalMinutes;
            var totalSeconds = (int)Duration.TotalSeconds;
            if (totalHours > 0)
            {
                //=> Duration.ToString(@"mm\:ss");
                return Duration.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return Duration.ToString(@"mm\:ss");
            }
        }
    }

    public string PlaycountString
    {
        get
        {
            if (Playcount is null)
            {
                return "";
            }
            else
            {
                return Playcount.Value.ToString("N0");
            }
        }
    }

    public required ICommand PlayCommand { get; init; }
    public object This => this;
    public required AlbumViewModel Album { get; init; }

    public WaveeTrackPlaybackState PlaybackState
    {
        get => _playbackState;
        set => SetProperty(ref _playbackState, value);
    }
}