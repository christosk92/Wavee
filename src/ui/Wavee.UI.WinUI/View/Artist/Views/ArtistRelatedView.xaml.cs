using System.Linq;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Client.Artist;
using Wavee.UI.Common;

namespace Wavee.UI.WinUI.View.Artist.Views;

public sealed partial class ArtistRelatedView : UserControl
{
    public ArtistRelatedView(WaveeUIArtistView waveeUiArtistView)
    {
        this.InitializeComponent();

        RelatedArtists = waveeUiArtistView.RelatedArtists
            .Where(x => x.IsSome)
            .Select(f => f.ValueUnsafe())
            .ToArray();
        HasMoreRelatedArtists = waveeUiArtistView.RelatedArtists.Any(f => f.IsNone);

        DiscoveredOn = waveeUiArtistView.DiscoveredOn
            .Where(x => x.IsSome)
            .Select(f => f.ValueUnsafe())
            .ToArray();
        HasMoreDiscoveredOn = waveeUiArtistView.DiscoveredOn.Any(f => f.IsNone);

        AppearsOn = waveeUiArtistView.AppearsOn
            .Where(x => x.IsSome)
            .Select(f => f.ValueUnsafe())
            .ToArray();
        HasMoreAppearsOn = waveeUiArtistView.AppearsOn.Any(f => f.IsNone);
    }

    public ICardViewModel[] RelatedArtists { get; }
    public bool HasMoreRelatedArtists { get; }

    public ICardViewModel[] DiscoveredOn { get; }
    public bool HasMoreDiscoveredOn { get; }

    public ICardViewModel[] AppearsOn { get; }
    public bool HasMoreAppearsOn { get; }
}