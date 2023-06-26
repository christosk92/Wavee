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
    public sealed partial class CardView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string),
            typeof(CardView), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title),
            typeof(string), typeof(CardView), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle),
            typeof(string), typeof(CardView), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image),
            typeof(string), typeof(CardView), new PropertyMetadata(default(string)));

        public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(nameof(Caption),
            typeof(string), typeof(CardView), new PropertyMetadata(default(string?)));

        public static readonly DependencyProperty ImageIsIconProperty = DependencyProperty.Register(nameof(ImageIsIcon),
            typeof(bool), typeof(CardView), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsArtistProperty = DependencyProperty.Register(nameof(IsArtist),
            typeof(bool), typeof(CardView), new PropertyMetadata(default(bool)));

        public CardView()
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

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public string Image
        {
            get => (string)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public string? Caption
        {
            get => (string?)GetValue(CaptionProperty);
            set
            {
                SetValue(CaptionProperty, value);
                OnPropertyChanged(nameof(HasCaption));
            }
        }

        public bool ImageIsIcon
        {
            get => (bool)GetValue(ImageIsIconProperty);
            set => SetValue(ImageIsIconProperty, value);
        }

        public bool IsArtist
        {
            get => (bool)GetValue(IsArtistProperty);
            set => SetValue(IsArtistProperty, value);
        }

        public bool HasCaption => !string.IsNullOrEmpty(Caption);
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

        public string CalculateInitials(string s) => string.Concat(s
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length >= 1 && char.IsLetter(x[0]))
            .Select(x => char.ToUpper(x[0])));
    }
}
