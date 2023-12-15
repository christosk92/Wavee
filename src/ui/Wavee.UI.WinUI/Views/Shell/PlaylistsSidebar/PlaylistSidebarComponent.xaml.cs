using System;
using System.Linq;
using System.Windows.Forms;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.Features.Playlists.ViewModel;
using Wavee.UI.Features.Shell.ViewModels;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views.Shell.PlaylistsSidebar;

public sealed partial class PlaylistSidebarComponent : UserControl
{
    public PlaylistSidebarComponent()
    {
        this.InitializeComponent();
    }
    public ShellViewModel ViewModel => (ShellViewModel)DataContext;
    public event EventHandler<PlaylistSidebarItemViewModel>? PlaylistSelected;


    private void TreeViewItemTapped(object sender, TappedRoutedEventArgs e)
    {
        var item = (TreeViewItem)sender;
        if (item.Tag is PlaylistSidebarItemViewModel playlist)
        {
            ViewModel.Playlists.SelectedPlaylist = playlist;

            var mediator = ViewModel.Mediator;
            var uiDispatcher = ViewModel.Dispatcher;
            ViewModel.Navigation.Navigate(Option<object>.None, new PlaylistViewModel(
                playlist,
                mediator, uiDispatcher));
        }
    }

    private bool _initialized = false;

    private void PlaylistSidebarComponent_OnDataContextChanged(FrameworkElement sender,
        DataContextChangedEventArgs args)
    {
        if (_initialized)
            return;

        if (ViewModel is not null)
        {
            ViewModel.Navigation.NavigatedTo += (o, e) =>
            {
                if (e is PlaylistViewModel pl)
                {
                    ViewModel.Playlists.SelectedPlaylist =
                        ViewModel.Playlists.Playlists.PlaylistViewModels.FirstOrDefault(x => x.Id == pl.Id)
                            as PlaylistSidebarItemViewModel;
                    this.Bindings.Update();
                }
                else
                {
                    ViewModel.Playlists.SelectedPlaylist = null;
                    this.Bindings.Update();
                }
            };
            _initialized = true;
        }
    }
}