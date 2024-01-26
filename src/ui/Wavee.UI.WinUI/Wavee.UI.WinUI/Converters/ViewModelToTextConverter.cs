using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.ViewModels.Feed;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.NowPlaying;

namespace Wavee.UI.WinUI.Converters;
internal sealed class ViewModelToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double? fontSize = null;
        var valueAsStr = parameter?.ToString();
        if (!string.IsNullOrEmpty(valueAsStr) && double.TryParse(valueAsStr, out var x))
        {
            fontSize = x;
        }

        var icon = value switch
        {
            FeedViewModel => "Feed",
            LibraryRootViewModel => "Library",
            NowPlayingViewModel => "Now Playing",
            LibraryTracksViewModel => "Tracks",
            LibraryAlbumsViewModel => "Albums",
            LibraryArtistsViewModel => "Artists",
            _ => null
        };

        return icon;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}