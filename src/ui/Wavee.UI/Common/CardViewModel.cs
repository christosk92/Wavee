using Wavee.Id;
using Wavee.Metadata.Common;
using Wavee.Metadata.Home;

namespace Wavee.UI.Common;

public interface ICardViewModel
{
    string Id { get; }
    string Title { get; }
    string? Image { get; }
    bool ImageIsIcon { get; }
    string? Subtitle { get; }
    bool HasSubtitle { get; }
    string? Caption { get; }
}
public sealed class CardViewModel : ICardViewModel
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Subtitle { get; init; }
    public string? Caption { get; init; }
    public string? Image { get; init; }
    public bool ImageIsIcon { get; init; }
    public bool IsArtist { get; init; }
    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);
    public AudioItemType Type { get; init; }

    public static ICardViewModel? From(ISpotifyHomeItem spotifyHomeItem)
    {
        static string GetMediumImage(CoverImage[] imgs)
        {
            //usually around ~300 pixels
            //so get image where difference between width and 300 is smallest
            const int targetWidth = 300;
            var best = imgs
                .OrderBy(x => Math.Abs(x.Width.IfNone(0) - targetWidth))
                .HeadOrNone()
                .Map(x => x.Url)
                .IfNone(string.Empty);
            return best;
        }
        return spotifyHomeItem switch
        {
            SpotifyCollectionItem collectionItem => new CardViewModel
            {
                Id = collectionItem.Id.ToString(),
                Title = "Saved songs",
                Image = "\uEB52",
                ImageIsIcon = true,
                Subtitle = "You can also find this in the sidebar.",
                Type = AudioItemType.UserCollection
            } as ICardViewModel,
            SpotifyPlaylistHomeItem playlistItem => new CardViewModel
            {
                Id = playlistItem.Id.ToString(),
                Title = playlistItem.Name,
                Subtitle = playlistItem.Description.Map(f => EscapeHtml(f))
                    .IfNone($"Playlist by {playlistItem.OwnerName}"),
                Image = GetMediumImage(playlistItem.Images),
                ImageIsIcon = false,
                Type = AudioItemType.Playlist
            },
            SpotifyAlbumHomeItem albumItem => new CardViewModel
            {
                Id = albumItem.Id.ToString(),
                Title = albumItem.Name,
                Subtitle = albumItem.Artists.HeadOrNone().Map(x => x.Name).IfNone(string.Empty),
                Image = GetMediumImage(albumItem.Images),
                Caption = "ALBUM",
                ImageIsIcon = false,
                Type = AudioItemType.Album,
            },
            SpotifyArtistHomeItem artistItem => new CardViewModel
            {
                Id = artistItem.Id.ToString(),
                Title = artistItem.Name,
                Subtitle = "Artist",
                IsArtist = true,
                Image = GetMediumImage(artistItem.Images),
                ImageIsIcon = false,
                Type = AudioItemType.Artist
            },
            SpotifyPodcastEpisodeHomeItem podcastEpisode => new PodcastEpisodeCardViewModel
            {
                Id = podcastEpisode.Id.ToString(),
                Title = podcastEpisode.Name,
                Image = GetMediumImage(podcastEpisode.Images),
                Duration = podcastEpisode.Duration,
                Progress = podcastEpisode.Position,
                PodcastDescription = podcastEpisode.Description.IfNone(string.Empty),
                Show = podcastEpisode.PodcastName,
                Started = podcastEpisode.Started,
                ReleaseDate = podcastEpisode.ReleaseDate,
            },
            _ => null
        };
    }

    private static string EscapeHtml(string s)
    {
        //remove all html tags and get the inner text
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(s);

        return doc.DocumentNode.InnerText;
    }
}

public sealed class PodcastEpisodeCardViewModel : ICardViewModel
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string? Image { get; init; }
    public bool ImageIsIcon => false;
    public string? Subtitle => Show;
    public bool HasSubtitle => true;
    public string? Caption { get; }
    public bool Started { get; init; }
    public TimeSpan Duration { get; init; }
    public TimeSpan Progress { get; init; }
    public string Show { get; init; }
    public string? PodcastDescription { get; init; }
    public DateTimeOffset ReleaseDate { get; init; }
}