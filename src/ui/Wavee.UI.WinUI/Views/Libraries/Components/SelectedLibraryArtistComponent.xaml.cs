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
    public sealed partial class SelectedLibraryArtistComponent : Page
    {

        public static readonly DependencyProperty SelectedArtistProperty = DependencyProperty.Register(nameof(SelectedArtist), typeof(LibraryArtistViewModel), typeof(SelectedLibraryArtistComponent), new PropertyMetadata(default(LibraryArtistViewModel)));
        private readonly AsyncLock _lock = new AsyncLock();

        public SelectedLibraryArtistComponent()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is LibraryArtistViewModel vm)
            {
                SelectedArtist = vm;
                await vm.FetchArtistAlbums(
                    offset: 0,
                    limit: limit,
                    CancellationToken.None);
            }
        }

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
