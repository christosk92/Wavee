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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.UI.Features.Album.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Libraries.Components
{
    public sealed partial class LibraryAlbumComponent : UserControl
    {
        public static readonly DependencyProperty PageWidthProperty = DependencyProperty.Register(nameof(PageWidth), typeof(double), typeof(LibraryAlbumComponent), new PropertyMetadata(default(double)));

        public LibraryAlbumComponent()
        {
            this.InitializeComponent();
        }

        public AlbumViewModel ViewModel => DataContext is AlbumViewModel v ? v : null;

        public double PageWidth
        {
            get => (double)GetValue(PageWidthProperty);
            set => SetValue(PageWidthProperty, value);
        }


        public string FormatToDurationString(TimeSpan timeSpan)
        {
            //3 min, 10 sec
            var totalHrs = timeSpan.Hours;
            var totalMins = timeSpan.Minutes;
            var totalSecs = timeSpan.Seconds;
            if (totalHrs > 0)
            {
                return $"{totalHrs} hr, {totalMins} min, {totalSecs} sec";
            }
            else if (totalMins > 0)
            {
                return $"{totalMins} min, {totalSecs} sec";
            }
            else
            {
                return $"{totalSecs} sec";
            }
        }

        private void LibraryAlbumComponent_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is AlbumViewModel v)
            {
                this.Bindings.Update();
            }
        }

        public bool SizeBetween(double pageWidth, double lowerBound, double upperBound)
        {
            double tolerance = 0.0001;
            if (Math.Abs(upperBound - (-1)) < tolerance)
            {
                return pageWidth >= lowerBound;
            }
            else
            {
                return pageWidth >= lowerBound && pageWidth < upperBound;
            }
        }

        private void LibraryAlbumComponent_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            PageWidth = e.NewSize.Width;
        }
    }
}
