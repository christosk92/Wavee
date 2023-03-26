using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.Interfaces.Models;

namespace Wavee.UI.ViewModels.Album
{
    [INotifyPropertyChanged]
    public partial class AlbumViewModel
    {
        public AlbumViewModel(IAlbum album)
        {
            Album = album;
        }

        public IAlbum Album
        {
            get;
        }


        public bool IsNull(object? o, bool ifNull)
        {
            return o is null ? ifNull : !ifNull;
        }
    }
}
