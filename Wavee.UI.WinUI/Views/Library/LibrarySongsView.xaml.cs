using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.UI.ViewModels.Libray;
using CommunityToolkit.WinUI;
using TagLib.Mpeg;
using Wavee.UI.Models.TrackSources;
using Wavee.UI.ViewModels.Track;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;
using ReactiveUI;
using Wavee.Interfaces.Models;
using Wavee.UI.ViewModels.Shell;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Library
{
    public sealed partial class LibrarySongsView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel),
                typeof(LibrarySongsViewModel), typeof(LibrarySongsView),
                new PropertyMetadata(default(LibrarySongsViewModel), PropertyChangedCallback));

        private IncrementalLoadingCollection<AbsTrackSource<TrackViewModel>, TrackViewModel> _tracks;


        public LibrarySongsView()
        {
            this.InitializeComponent();
        }

        public LibrarySongsViewModel ViewModel
        {
            get => (LibrarySongsViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public IncrementalLoadingCollection<AbsTrackSource<TrackViewModel>, TrackViewModel> Tracks
        {
            get => _tracks;
            set => SetField(ref _tracks, value);
        }

        private void AscendingTapped(object sender, TappedRoutedEventArgs e)
        {
            if (AscendingBox.IsChecked == true)
            {
                ViewModel.SortAscending = true;
            }
            else
            {
                ViewModel.SortAscending = false;
            }
        }

        private void ExtendSortLv_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var firstItem = e.AddedItems.Cast<SortOption>().First();
                ViewModel.SortBy = firstItem;
            }
        }
        private async void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ViewModel.Tracks))
            {
                Tracks = new IncrementalLoadingCollection<AbsTrackSource<TrackViewModel>, TrackViewModel>(
                    source: ViewModel.Tracks
                );
            }
            if (e.PropertyName is nameof(ViewModel.SortAscending) or nameof(ViewModel.SortBy) or nameof(ViewModel.HeartedFilter))
            {
                await Tracks.RefreshAsync();
            }
        }


        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (LibrarySongsView)d;
            if (e.OldValue is LibrarySongsViewModel lold)
            {
                p.UnregisterViewModel(lold);
            }
            if (e.NewValue is LibrarySongsViewModel l)
            {
                p.RegisterViewModel(l);
            }
        }

        private void UnregisterViewModel(LibrarySongsViewModel lold)
        {
            lold.PropertyChanged -= ViewModelOnPropertyChanged;
            ShellViewModel.Instance.PlaybackViewModel.PlayingItemChanged -= PlaybackViewModelOnPlayingItemChanged;
        }

        private void RegisterViewModel(LibrarySongsViewModel lnew)
        {
            lnew.PropertyChanged += ViewModelOnPropertyChanged;
            if (lnew.Tracks != null)
            {
                ViewModelOnPropertyChanged(null, new PropertyChangedEventArgs(nameof(LibrarySongsViewModel.Tracks)));
            }
            ShellViewModel.Instance.PlaybackViewModel.PlayingItemChanged += PlaybackViewModelOnPlayingItemChanged;
        }

        private void LibrarySongsView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnregisterViewModel(ViewModel);
        }

        private void PlaybackViewModelOnPlayingItemChanged(object? sender, IPlayableItem? e)
        {
            //if a track was changed, we need to update the list IF we are sorting on playcount or lastplayed
            RxApp.MainThreadScheduler.Schedule(async () =>
            {
                if (ViewModel.SortBy is SortOption.Playcount or SortOption.LastPlayed)
                {
                    await Tracks.RefreshAsync();
                }
            });
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
