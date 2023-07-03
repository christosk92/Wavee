using Eum.Spotify.playlist4;

namespace Wavee.UI.ViewModel.Playlist.Headers;

public sealed class RegularPlaylistHeader : IPlaylistHeader
{
    public RegularPlaylistHeader(SelectedListContent listContent)
    {
        ImageUrl = listContent.Attributes.FormatAttributes.SingleOrDefault(x => x.Key is "image_url")?.Value;
        ShouldShowMozaic = string.IsNullOrEmpty(ImageUrl);

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

        Owner = listContent.OwnerUsername;
    }
    public string Owner { get; }
    public string Name { get; }
    public string Description { get; }

    public string? ImageUrl { get; }
    public bool ShouldShowMozaic { get; }

    public string? MadeForUsername { get; }
    public bool ShouldShowMadeFor { get; }

    public bool IsCollab { get; }
}