using Wavee.Core.Ids;

namespace Wavee.UI.ViewModels.Artist;

public class ArtistView
{
    public string Name { get; }
    public string HeaderImage { get; }
    public ulong MonthlyListeners { get; }
    public List<ArtistTopTrackView> TopTracks { get; set; }
    public List<ArtistDiscographyGroupView> Discography { get; set; }
    public string ProfilePicture { get; }
    public AudioId Id { get; }

    public ArtistView(string name, string headerImage, ulong monthlyListeners, List<ArtistTopTrackView>
        topTracks, List<ArtistDiscographyGroupView> discography, string profilePicture, AudioId id)
    {
        Name = name;
        HeaderImage = headerImage;
        MonthlyListeners = monthlyListeners;
        TopTracks = topTracks;
        Discography = discography;
        ProfilePicture = profilePicture;
        Id = id;
    }

    public void Clear()
    {
        foreach (var artistDiscographyGroupView in Discography)
        {
            foreach (var artistDiscographyView in artistDiscographyGroupView.Views)
            {
                foreach (var track in artistDiscographyView.Tracks.Tracks)
                {
                    track.PlayCommand = null;
                }
                artistDiscographyView.Tracks.Tracks.Clear();
            }
            artistDiscographyGroupView.Views.Clear();
        }

        foreach (var topTrack in TopTracks)
        {
            topTrack.PlayCommand = null;
        }
        TopTracks.Clear();

        Discography.Clear();
        Discography = null;
        TopTracks = null;
    }
}