using Eum.Spotify.playlist4;

namespace Wavee.UI.ViewModel.Playlist.Headers;

public class PlaylistBigHeader : IPlaylistHeader
{
    public PlaylistBigHeader(SelectedListContent listContent)
    {
        Name = listContent.Attributes.Name;
        Description = listContent.Attributes.Description;
        IsCollab = listContent.Attributes.Collaborative;
        var shouldShowMadeFor = listContent.Attributes.FormatAttributes
                                    .Any(x => x.Key is "madeFor.displayed")
                                && listContent.Attributes.FormatAttributes.Single(x => x.Key is "madeFor.displayed")
                                    .Value is "1";

        var madeForUsername = shouldShowMadeFor
            ? listContent.Attributes.FormatAttributes.Single(x => x.Key is "madeFor.username").Value
            : null;

        ShouldShowMadeFor = shouldShowMadeFor;
        MadeForUsername = madeForUsername;
        HeaderImage = listContent.Attributes.FormatAttributes.Single(x => x.Key is "header_image_url_desktop").Value;

        Owner = listContent.OwnerUsername;
    }
    public string Owner { get;  }
    public string Name { get; }
    public string Description { get; }

    public string? MadeForUsername { get; }
    public bool ShouldShowMadeFor { get; }
    public string HeaderImage { get; }
    public bool IsCollab { get; }
}