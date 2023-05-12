using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using LanguageExt;
using Microsoft.UI.Xaml;
using ReactiveUI;
using Wavee.UI.Helpers;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Views.Playlists;
using Unit = System.Reactive.Unit;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView : UserControl, IViewFor<MainViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty
            .Register(nameof(ViewModel), typeof(MainViewModel), typeof(ShellView), new PropertyMetadata(null));

        public ShellView()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
            SidebarControl.SetSidebarWidth(Services.UiConfig.SidebarWidth ?? Constants.DefaultSidebarWidth);
            SidebarControl.PlaylistFilters = new Seq<PlaylistSourceFilter>(new[]
            {
                PlaylistSourceFilter.Yours, PlaylistSourceFilter.Others,
                PlaylistSourceFilter.Spotify, PlaylistSourceFilter.Local
            });
            SidebarControl.PlaylistSort = ViewModel.PlaylistSorts;

            this.WhenActivated(disposable =>
            {
                this.SidebarControl.SidebarWidthChanged
                    .Throttle(TimeSpan.FromMilliseconds(500))
                    .Skip(1) // Won't save on UiConfig creation.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x =>
                    {
                        Services.UiConfig.SidebarWidth = x;
                        return Unit.Default;
                    })
                    .Subscribe()
                    .DisposeWith(disposable);


                this.SidebarControl.WhenAnyValue(x => x.PlaylistSort)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x =>
                    {
                        ViewModel.PlaylistSorts = x;
                        return Unit.Default;
                    })
                    .Subscribe()
                    .DisposeWith(disposable);

                this.SidebarControl.WhenAnyValue(x => x.PlaylistFilters)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x =>
                    {
                        ViewModel.PlaylistSourceFilters = x;
                        return Unit.Default;
                    })
                    .Subscribe()
                    .DisposeWith(disposable);


                this.OneWayBind(ViewModel,
                    vmProperty: viewModel => viewModel.SidebarItems,
                    viewProperty: view => view.SidebarControl.SidebarItems);

                ViewModel.HasPlaylists
                    .Select(c => c ? Visibility.Collapsed : Visibility.Visible)
                    .BindTo(this, x => x.SidebarControl.NoPlaylistsView.Visibility);
            });
        }

        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainViewModel)value;
        }

        private void SidebarControl_OnOnCreatePlaylistRequested(object sender, EventArgs e)
        {
            NavigationFrame.Content = new CreatePlaylistView();
        }
    }
}
