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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.UI.Animations;
using LanguageExt;
using Wavee.Id;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.ContextFlyout;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.View.Album;
using Microsoft.UI.Xaml.Media.Animation;
using Wavee.UI.WinUI.View.Artist;
using Wavee.UI.WinUI.View.Playlist;

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

        private bool _buttonsPanelLoaded;
        private string _id;
        public static readonly DependencyProperty AudioTypeProperty = DependencyProperty.Register(nameof(AudioType), typeof(AudioItemType), typeof(CardView), new PropertyMetadata(default(AudioItemType)));

        public CardView()
        {
            this.InitializeComponent();
        }

        public string Id
        {
            get => (string)GetValue(IdProperty);
            set
            {
                SetValue(IdProperty, value);
                _id = value;

            }
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

        public bool ButtonsPanelLoaded
        {
            get => _buttonsPanelLoaded;
            set => SetField(ref _buttonsPanelLoaded, value);
        }

        public bool HasCaption => !string.IsNullOrEmpty(Caption);

        public AudioItemType AudioType
        {
            get => (AudioItemType)GetValue(AudioTypeProperty);
            set => SetValue(AudioTypeProperty, value);
        }

        public event EventHandler OnNavigated;

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

        private void CardView_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ButtonsPanelLoaded = true;
        }

        private void CardView_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (ButtonsPanel is not null)
            {
                _ = AnimationBuilder.Create()
                    .Translation(new Vector2(0, 20), duration: TimeSpan.FromMilliseconds(200))
                    .Opacity(to: 0, from: 1, duration: TimeSpan.FromMilliseconds(100))
                    .StartAsync((ButtonsPanel as UIElement)!);
                ButtonsPanel.Visibility = Visibility.Collapsed;
            }
            ButtonsPanelLoaded = false;

            /*
            

                        <animations:Implicit.HideAnimations>
                            <animations:OpacityAnimation Duration="0:0:0.1"
                                                         To="0.0" />
                            <animations:TranslationAnimation Duration="0:0:0.2"
                                                             From="0, 0, 0"
                                                             To="0, 20,0" />
                        </animations:Implicit.HideAnimations>
             */
        }

        private void ButtonsPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ButtonsPanel is not null)
            {
                ButtonsPanel.Visibility = Visibility.Visible;

                /*
                 * *   <animations:Implicit.ShowAnimations>
                            <animations:TranslationAnimation Duration="0:0:0.2"
                                                             From="0, 30, 0" 
                                                             To="0" />
                            <animations:OpacityAnimation Duration="0:0:0.2"
                                                         From="0"
                                                         To="1.0" />
                        </animations:Implicit.ShowAnimations>
                 */
                _ = AnimationBuilder.Create()
                    .Translation(new Vector2(0), from: new Vector2(0, 30), duration: TimeSpan.FromMilliseconds(200))
                    .Opacity(to: 1, from: 0, duration: TimeSpan.FromMilliseconds(200))
                    .StartAsync((ButtonsPanel as UIElement)!);
            }
        }

        private void CardView_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            Point point = new Point(0, 0);
            var contextFlyout = ContextFlyouts.BuildFlyout(Id);
            if (args.TryGetPosition(sender, out point))
            {
                contextFlyout.ShowAt(sender, point);
            }
            else
            {
                contextFlyout.ShowAt((FrameworkElement)sender);
            }
        }

        private void CardView_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Navigate();
        }

        public void Navigate()
        {
            switch (AudioType)
            {
                case AudioItemType.Album:
                    NavigationService.Instance.Navigate(typeof(AlbumView), new NavigatingWithImage(
                        Id: this.Id,
                        Image: this.NormalImageImage.Source
                    ));
                    ButtonsPanelLoaded = false;
                    OnNavigated?.Invoke(this.NormalImageBox, EventArgs.Empty);
                    break;
                case AudioItemType.Artist:
                    NavigationService.Instance.Navigate(typeof(ArtistView), this.Id);
                    ButtonsPanelLoaded = false;
                    OnNavigated?.Invoke(null, EventArgs.Empty);
                    break;
                case AudioItemType.Playlist:
                    NavigationService.Instance.Navigate(typeof(PlaylistView), this.Id);
                    ButtonsPanelLoaded = false;
                    OnNavigated?.Invoke(null, EventArgs.Empty);
                    break;
            }
        }
        public async Task<Option<string>> GetPreviewStreamsAsync(CancellationToken ct)
        {
            var user = ShellViewModel.Instance.User;
            var previewStreams = await user.Client.Previews.GetPreviewStreamsForContext(_id, ct);
            return previewStreams;
        }
    }
}
