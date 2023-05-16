using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Playlist
{
    public sealed partial class CreatePlaylistView : UserControl, INavigablePage
    {
        private bool _created;
        public CreatePlaylistView()
        {
            this.InitializeComponent();
        }

        public bool ShouldKeepInCache(int depth)
        {
            //if created, return false
            if (_created) return false;
            return depth == 1;
        }

        public Option<INavigableViewModel> ViewModel => Option<INavigableViewModel>.None;
    }
}
