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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Components
{
    public sealed partial class PodcastEpisodeCardView : UserControl
    {
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string),
           typeof(PodcastEpisodeCardView), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title),
            typeof(string), typeof(PodcastEpisodeCardView), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image),
            typeof(string), typeof(PodcastEpisodeCardView), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty StartedProperty = DependencyProperty.Register(nameof(Started), typeof(bool), typeof(PodcastEpisodeCardView), new PropertyMetadata(default(bool)));
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(nameof(Duration), typeof(TimeSpan), typeof(PodcastEpisodeCardView), new PropertyMetadata(default(TimeSpan)));
        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(nameof(Progress), typeof(TimeSpan), typeof(PodcastEpisodeCardView), new PropertyMetadata(default(TimeSpan)));
        public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(nameof(ShowTitle), typeof(string), typeof(PodcastEpisodeCardView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(PodcastEpisodeCardView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ReleaseDateProperty = DependencyProperty.Register(nameof(ReleaseDate), typeof(DateTimeOffset), typeof(PodcastEpisodeCardView), new PropertyMetadata(default(DateTimeOffset)));


        public PodcastEpisodeCardView()
        {
            this.InitializeComponent();
        }

        public string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Image
        {
            get => (string)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public bool Started
        {
            get => (bool)GetValue(StartedProperty);
            set => SetValue(StartedProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public TimeSpan Progress
        {
            get => (TimeSpan)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public string ShowTitle
        {
            get => (string)GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set
            {
                SetValue(DescriptionProperty, value);
                OnPropertyChanged(nameof(HasDescription));
            }
        }
        public bool HasDescription => !string.IsNullOrEmpty(Description);

        public DateTimeOffset ReleaseDate
        {
            get => (DateTimeOffset)GetValue(ReleaseDateProperty);
            set => SetValue(ReleaseDateProperty, value);
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

        public bool Negate(bool b)
        {
            return !b;
        }

        public string FormatReleaseDate(DateTimeOffset dateTimeOffset)
        {
            //month name + day
            return dateTimeOffset.ToString("MMMM d");
        }

        public string FormatTimestamp(TimeSpan timeSpan)
        {
            var minutes = timeSpan.TotalMinutes;
            return $"{(int)minutes} min";
        }

        public double CalculateHeight(bool b, short s, short s1)
        {
            return b ? s : s1;
        }

        public string FormatTimestampString(TimeSpan timeSpan)
        {
            //mm:ss
            return timeSpan.ToString("mm\\:ss");
        }
    }
}
