using Wavee.UI.AudioImport;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Models;
using Wavee.UI.ViewModels.Artist;

namespace Wavee.UI.ViewModels.Playback;

public record PlayingTrackView(
    string Name,
    IDescriptionaryItem[] Artists,
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
            Artists: track.Artists
                .Select(a => new ArtistDescriptionaryItem(a, a))
                .Cast<IDescriptionaryItem>()
                .ToArray(),
            Album: track.Album,
            Duration: track.Duration,
            Image: track.ImagePath,
            Provider: ServiceType.Local
        );
    }
}
