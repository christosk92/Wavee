using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NeoSmart.AsyncLock;
using Wavee.UI.Features.Library.ViewModels.Artist;


namespace Wavee.UI.WinUI.Views.Libraries.Components
{
    public sealed partial class SelectedLibraryArtistComponent : UserControl
    {
        private static AsyncLock _lock = new();
        private static CancellationTokenSource _cts;

        public static readonly DependencyProperty SelectedArtistProperty = DependencyProperty.Register(nameof(SelectedArtist), typeof(LibraryArtistViewModel), typeof(SelectedLibraryArtistComponent), new PropertyMetadata(default(LibraryArtistViewModel), PropertyChangedCallback));

        private static async void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                _cts?.Cancel();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                try
                {

                    _cts?.Dispose();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            CancellationToken token = default;
            try
            {
                using (await _lock.LockAsync())
                {
                    try
                    {
                        token = _cts?.Token ?? CancellationToken.None;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    _cts = new CancellationTokenSource();
                    token = _cts.Token;


                    var x = d as SelectedLibraryArtistComponent;
                    try
                    {
                        await x.OnSelectedArtistChanged(e.NewValue as LibraryArtistViewModel, token);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        public LibraryArtistsViewModel ViewModel => DataContext is LibraryArtistsViewModel vm ? vm : null;
        public SelectedLibraryArtistComponent()
        {
            this.InitializeComponent();
        }

        public LibraryArtistViewModel SelectedArtist
        {
            get => (LibraryArtistViewModel)GetValue(SelectedArtistProperty);
            set => SetValue(SelectedArtistProperty, value);
        }

        private async Task OnSelectedArtistChanged(LibraryArtistViewModel artist, CancellationToken cancellationToken)
        {
            await ViewModel.FetchArtistAlbums(artist,
                offset: 0,
                limit: limit,
                cancellationToken);
        }

        const int limit = 10;

        private async void MainScroller_OnViewChanged(ScrollView sender, object args)
        {
            try
            {
                using (await _lock.LockAsync(_cts.Token))
                {
                    // Fetch more albums if we are at the bottom of the scrollviewer
                    if (sender.VerticalOffset >= sender.ScrollableHeight - 200)
                    {
                        if (SelectedArtist.Albums.Count >= SelectedArtist.TotalAlbums)
                        {
                            return;
                        }

                        await ViewModel.FetchArtistAlbums(SelectedArtist,
                            offset: SelectedArtist.Albums.Count,
                            limit: limit,
                            _cts.Token);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
