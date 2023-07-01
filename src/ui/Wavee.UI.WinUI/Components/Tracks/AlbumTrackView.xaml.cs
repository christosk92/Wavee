using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Wavee.Metadata.Artist;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Components.Tracks
{
    public sealed partial class AlbumTrackView : UserControl
    {
        public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(nameof(Number), typeof(ushort), typeof(AlbumTrackView), new PropertyMetadata(default(ushort)));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(AlbumTrackView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ArtistsProperty = DependencyProperty.Register(nameof(Artists), typeof(ITrackArtist[]), typeof(AlbumTrackView), new PropertyMetadata(default(ITrackArtist[])));
        public static readonly DependencyProperty PlaycountProperty = DependencyProperty.Register(nameof(Playcount), typeof(Option<ulong>), typeof(AlbumTrackView), new PropertyMetadata(default(Option<ulong>)));
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(nameof(Duration), typeof(TimeSpan), typeof(AlbumTrackView), new PropertyMetadata(default(TimeSpan)));
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(AlbumTrackView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty UidProperty = DependencyProperty.Register(nameof(Uid), typeof(Option<string>), typeof(AlbumTrackView), new PropertyMetadata(default(Option<string>)));
        public static readonly DependencyProperty WithCheckboxProperty = DependencyProperty.Register(nameof(WithCheckbox), typeof(bool), typeof(AlbumTrackView), new PropertyMetadata(default(bool)));

        public AlbumTrackView()
        {
            this.InitializeComponent();
        }

        public ushort Number
        {
            get => (ushort)GetValue(NumberProperty);
            set => SetValue(NumberProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public ITrackArtist[] Artists
        {
            get => (ITrackArtist[])GetValue(ArtistsProperty);
            set => SetValue(ArtistsProperty, value);
        }

        public Option<ulong> Playcount
        {
            get => (Option<ulong>)GetValue(PlaycountProperty);
            set => SetValue(PlaycountProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public Option<string> Uid
        {
            get => (Option<string>)GetValue(UidProperty);
            set => SetValue(UidProperty, value);
        }

        public bool WithCheckbox
        {
            get => (bool)GetValue(WithCheckboxProperty);
            set => SetValue(WithCheckboxProperty, value);
        }


        public string FormatPlaycount(Option<ulong> ulongs)
        {
            if (ulongs.IsNone) return "< 1,000";
            var x = ulongs.IfNone(0);
            if (x < 1000) return x.ToString();

            //1 million -> 1,000,000
            //1 billion -> 1,000,000,000
            return x.ToString("N0");
        }

        public string FormatDuration(TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"mm\:ss");
        }

        public ushort MinusOne(ushort @ushort)
        {
            return Math.Max((ushort)0, (ushort)(@ushort - 1));
        }
    }
}
