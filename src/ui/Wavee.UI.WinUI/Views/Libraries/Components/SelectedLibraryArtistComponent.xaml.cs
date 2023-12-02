using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NeoSmart.AsyncLock;
using Wavee.UI.Features.Library.ViewModels.Artist;


namespace Wavee.UI.WinUI.Views.Libraries.Components
{
    public sealed partial class SelectedLibraryArtistComponent : UserControl
    {

        public static readonly DependencyProperty SelectedArtistProperty = DependencyProperty.Register(nameof(SelectedArtist), typeof(LibraryArtistViewModel), typeof(SelectedLibraryArtistComponent), new PropertyMetadata(default(LibraryArtistViewModel), ArtistChanged));

        private static async void ArtistChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is LibraryArtistViewModel vm)
            {
                using (await _lock.LockAsync())
                {
                    if (vm.Albums.Count is not 0) return;
                    await vm.FetchArtistAlbums(
                        offset: 0,
                        limit: limit,
                        CancellationToken.None);
                }
            }
        }

        private static readonly AsyncLock _lock = new AsyncLock();

        public SelectedLibraryArtistComponent()
        {
            this.InitializeComponent();
        }

        // protected override async void OnNavigatedTo(NavigationEventArgs e)
        // {
        //     base.OnNavigatedTo(e);
        //     if (e.Parameter is LibraryArtistViewModel vm)
        //     {
        //         SelectedArtist = vm;
        //         await vm.FetchArtistAlbums(
        //             offset: 0,
        //             limit: limit,
        //             CancellationToken.None);
        //     }
        // }

        public LibraryArtistViewModel SelectedArtist
        {
            get => (LibraryArtistViewModel)GetValue(SelectedArtistProperty);
            set => SetValue(SelectedArtistProperty, value);
        }


        const int limit = 5;

        private async void MainScroller_OnViewChanged(ScrollView sender, object args)
        {
            try
            {
                using (await _lock.LockAsync())
                {
                    // Fetch more albums if we are at the bottom of the scrollviewer
                    if (sender.VerticalOffset >= sender.ScrollableHeight - 200)
                    {
                        if (SelectedArtist.Albums.Count >= SelectedArtist.TotalAlbums)
                        {
                            return;
                        }

                        await SelectedArtist.FetchArtistAlbums(
                            offset: SelectedArtist.Albums.Count,
                            limit: limit,
                            CancellationToken.None);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public Visibility NotNullThenVisible(uint? u)
        {
            return u.HasValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
