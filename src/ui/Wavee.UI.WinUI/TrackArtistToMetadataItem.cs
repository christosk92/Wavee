using System;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Wavee.Id;
using Wavee.Metadata.Artist;
using Wavee.UI.Client.Playback;
using Wavee.UI.ViewModel.Playlist;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.View.Album;
using Wavee.UI.WinUI.View.Artist;
using Wavee.UI.WinUI.View.Shell;

namespace Wavee.UI.WinUI;

public class TrackArtistToMetadataItem : IValueConverter
{
    public static ICommand NavigateTo = new RelayCommand<ItemWithId>(id =>
    {
        switch (id.Type)
        {
            case AudioItemType.Album:
                NavigationService.Instance.Navigate(typeof(AlbumView), id.Id);
                break;
            case AudioItemType.Artist:
                NavigationService.Instance.Navigate(typeof(ArtistView), id.Id);
                break;
        }
    });
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value switch
        {
            ITrackArtist[] trackArtists => trackArtists.Select(x => new MetadataItem
            {
                Label = x.Name,
                Command = NavigateTo,
                CommandParameter = new ItemWithId(Id: x.Id.ToString(), Type: x.Id.Type, Title: x.Name)
            }),
            ITrackAlbum album => new[]
            {
                new MetadataItem
                {
                    Label = album.Name,
                    Command = NavigateTo,
                    CommandParameter = new ItemWithId(Id: album.Id.ToString(), Type: AudioItemType.Album,
                        Title: album.Name)
                }
            },
            _ => Enumerable.Empty<MetadataItem>()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}