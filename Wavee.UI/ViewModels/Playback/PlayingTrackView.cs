using Wavee.UI.AudioImport;
using Wavee.UI.Identity.Users.Contracts;

namespace Wavee.UI.ViewModels.Playback;

public record PlayingTrackView(
    string Name,
    string[] Artists,
    string Album,
    double Duration,
    string? Image,
    ServiceType Provider
    )
{
    public static PlayingTrackView From(LocalAudioFile? track)
    {
        return new PlayingTrackView(
            Name: track.Name,
            Artists: track.Artists,
            Album: track.Album,
            Duration: track.Duration,
            Image: track.ImagePath,
            Provider: ServiceType.Local
        );
    }
}
