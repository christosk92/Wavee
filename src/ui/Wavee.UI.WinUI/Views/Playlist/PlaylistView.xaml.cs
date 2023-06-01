using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;
using Wavee.UI.ViewModels.Playlists;

namespace Wavee.UI.WinUI.Views.Playlist
{
    public sealed partial class PlaylistView : UserControl, INavigablePage
    {
        public PlaylistView()
        {
            ViewModel = new PlaylistViewModel();
            this.InitializeComponent();
        }

        public PlaylistViewModel ViewModel { get; }

        public bool ShouldKeepInCache(int depth)
        {
            //only if depth leq 3
            return depth <= 3;
        }

        Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

        public async void NavigatedTo(object parameter)
        {
            //ok, so what?
        }

        public void RemovedFromCache()
        {
            //cleanup
        }
    }
}
