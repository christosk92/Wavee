using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using LanguageExt;
using Microsoft.VisualBasic.Logging;
using Wavee.UI.ViewModel.Playlist;
using Log = Serilog.Log;
using Microsoft.UI.Xaml.Hosting;
using System.ComponentModel;
using System.Net.Http;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Imaging;
using Microsoft.UI.Xaml.Media.Imaging;
using Spotify.Metadata;
using Wavee.Metadata.Common;
using Image = Microsoft.UI.Xaml.Controls.Image;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Controls
{
    public sealed partial class MozaicImageControl : UserControl
    {
        public static readonly DependencyProperty FutureTracksProperty = DependencyProperty.Register(nameof(FutureTracks), typeof(TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>>), typeof(MozaicImageControl), new PropertyMetadata(default(TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>>), TracksChanged));

        private static async void TracksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (MozaicImageControl)d;
            await x.TracksChanged(e.NewValue);
        }

        public static readonly DependencyProperty ImageLoadedProperty = DependencyProperty.Register(nameof(ImageLoaded), typeof(bool), typeof(MozaicImageControl), new PropertyMetadata(default(bool)));

        public MozaicImageControl()
        {
            this.InitializeComponent();
        }

        public TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>> FutureTracks
        {
            get => (TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>>)GetValue(FutureTracksProperty);
            set
            {
                SetValue(FutureTracksProperty, value);
                _ = TracksChanged(value);
            }
        }

        public bool ImageLoaded
        {
            get => (bool)GetValue(ImageLoadedProperty);
            set => SetValue(ImageLoadedProperty, value);
        }

        private async Task TracksChanged(object eNewValue)
        {
            ImageLoaded = false;
            if (eNewValue is not TaskCompletionSource<Seq<Either<WaveeUIEpisode, WaveeUITrack>>> tcs)
                return;
            try
            {
                var tracks = await tcs.Task;
                //Mozaic is created by either a grid of 4 tracks or more or 1 track
                //nothing in between
                var firstFourTracks = tracks.Take(4);
                var hasMoreThanFourTracks = tracks.Count() >= 4;
                if (hasMoreThanFourTracks)
                {
                    await ConstructGridMozaic(firstFourTracks);
                }
                else
                {
                    ConstructSingleMozaic(tracks);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error loading tracks");
            }
        }

        private void ConstructSingleMozaic(Seq<Either<WaveeUIEpisode, WaveeUITrack>> tracks)
        {

        }

        private async Task ConstructGridMozaic(Seq<Either<WaveeUIEpisode, WaveeUITrack>> firstFourTracks)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            var firstTrack = firstFourTracks.First();
            var secondTrack = firstFourTracks.Skip(1).First();
            var thirdTrack = firstFourTracks.Skip(2).First();
            var fourthTrack = firstFourTracks.Skip(3).First();

            var firstTrackImage = GetImage(firstTrack);
            var secondTrackImage = GetImage(secondTrack);
            var thirdTrackImage = GetImage(thirdTrack);
            var fourthTrackImage = GetImage(fourthTrack);

            var firstImageLoaded = new TaskCompletionSource<bool>();
            var secondImageLoaded = new TaskCompletionSource<bool>();
            var thirdImageLoaded = new TaskCompletionSource<bool>();
            var fourthImageLoaded = new TaskCompletionSource<bool>();

            var firstTrackImageControl = new Image
            {
                Source = firstTrackImage,
                Stretch = Stretch.UniformToFill,
            };
            firstTrackImageControl.ImageOpened += (sender, args) => firstImageLoaded.SetResult(true);


            var secondTrackImageControl = new Image
            {
                Source = secondTrackImage,
                Stretch = Stretch.UniformToFill
            };
            secondTrackImageControl.ImageOpened += (sender, args) => secondImageLoaded.SetResult(true);

            var thirdTrackImageControl = new Image
            {
                Source = thirdTrackImage,
                Stretch = Stretch.UniformToFill
            };
            thirdTrackImageControl.ImageOpened += (sender, args) => thirdImageLoaded.SetResult(true);

            var fourthTrackImageControl = new Image
            {
                Source = fourthTrackImage,
                Stretch = Stretch.UniformToFill
            };
            fourthTrackImageControl.ImageOpened += (sender, args) => fourthImageLoaded.SetResult(true);

            grid.Children.Add(firstTrackImageControl);
            grid.Children.Add(secondTrackImageControl);
            grid.Children.Add(thirdTrackImageControl);
            grid.Children.Add(fourthTrackImageControl);

            Grid.SetColumn(firstTrackImageControl, 0);
            Grid.SetRow(firstTrackImageControl, 0);
            Grid.SetColumn(secondTrackImageControl, 1);
            Grid.SetRow(secondTrackImageControl, 0);
            Grid.SetColumn(thirdTrackImageControl, 0);
            Grid.SetRow(thirdTrackImageControl, 1);
            Grid.SetColumn(fourthTrackImageControl, 1);
            Grid.SetRow(fourthTrackImageControl, 1);

            MainControl.Child = grid;
            await Task.WhenAll(firstImageLoaded.Task, secondImageLoaded.Task, thirdImageLoaded.Task, fourthImageLoaded.Task);
            ImageLoaded = true;
        }

        private static BitmapImage GetImage(Either<WaveeUIEpisode, WaveeUITrack> firstTrack)
        {

            static string GetImageUrl(Either<WaveeUIEpisode, WaveeUITrack> item)
            {
                return item.Match(
                    Right: track => track.Covers,
                    Left: episode => episode.Covers)
                    .OrderByDescending(x => x.Height.IfNone(0))
                    .HeadOrNone()
                    .Map(x => x.Url)
                    .IfNone("");
            }

            var imageUrl = GetImageUrl(firstTrack);
            var image = new BitmapImage(new Uri(imageUrl));
            image.DecodePixelHeight = 200;
            image.DecodePixelWidth = 200;
            return image;
        }
    }
}
