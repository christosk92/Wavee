using CommunityToolkit.Mvvm.ComponentModel;
using Eum.Spotify.playlist4;
using LanguageExt;

namespace Wavee.UI.ViewModel.Playlist.Headers;

public sealed class RegularPlaylistHeader : ObservableObject, IPlaylistHeader
{
    private bool _mozaicCreated;

    public RegularPlaylistHeader(SelectedListContent listContent, TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>> futureTracks)
    {
        FutureTracks = futureTracks;
        ImageUrl = listContent.Attributes.PictureSize.SingleOrDefault(x => x.TargetName is "large")?.Url
                   ?? listContent.Attributes.PictureSize.SingleOrDefault(x => x.TargetName is "default")?.Url;
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
    public TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>> FutureTracks { get; }

    public bool MozaicCreated
    {
        get => _mozaicCreated;
        set => SetProperty(ref _mozaicCreated, value);
    }

    public bool Negate(bool b)
    {
        return !b;
    }

    public string HtmlEscape(string s)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml(s);

        return doc.DocumentNode.InnerText;
    }
}