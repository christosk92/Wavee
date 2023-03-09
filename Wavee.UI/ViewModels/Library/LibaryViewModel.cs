using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wavee.Spotify.Id;
using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.ViewModels.Library;

public class LibaryViewModel
{
}

public class LibraryViewModelFactory : SidebarItemViewModel
{
    public override string Title => Type switch
    {
        AudioItemType.Track => "Songs",
        AudioItemType.Album => "Albums",
        AudioItemType.Artist => "Artists",
        AudioItemType.Show => "Podcasts"
    };

    public override string Icon => Type switch
    {
        AudioItemType.Track => "\uEB52",
        AudioItemType.Album => "\uE93C",
        AudioItemType.Artist => "\uEBDA",
        AudioItemType.Show => "\uEB44"
    };

    public override string GlyphFontFamily => "Segoe Fluent Icons";
    public override string Id => Type.ToString().ToLowerInvariant();
    public int Count { get; }
    public AudioItemType Type { get; init; }
    public override void NavigateTo()
    {

    }
}