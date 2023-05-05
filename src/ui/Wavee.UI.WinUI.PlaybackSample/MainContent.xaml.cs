using System;
using System.Reactive.Disposables;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Org.BouncyCastle.Asn1.Cms;
using Image = Spotify.Metadata.Image;

namespace Wavee.UI.WinUI.PlaybackSample
{
    public sealed partial class MainContent : UserControl, IViewFor<MainViewModel>

    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty
            .Register(nameof(ViewModel), typeof(MainViewModel), typeof(MainContent), new PropertyMetadata(null));

        public MainContent(Option<MainViewModel> existingVm)
        {
            ViewModel = existingVm.IfNone(new MainViewModel());
            this.InitializeComponent();

            // Setup the bindings.
            // Note: We have to use WhenActivated here, since we need to dispose the
            // bindings on XAML-based platforms, or else the bindings leak memory.
            this.WhenActivated(disposable =>
            {
                this.OneWayBind(ViewModel,
                        x => x.ErrorMessage,
                        x => x.ErrorMessageText.Visibility,
                        selector: b => string.IsNullOrEmpty(b) ? Visibility.Collapsed : Visibility.Visible)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.Username,
                        x => x.UsernameText.Text)
                    .DisposeWith(disposable);



                this.OneWayBind(ViewModel,
                        viewModel => viewModel.SearchResults,
                        view => view.searchTextBox.ItemsSource)
                    .DisposeWith(disposable);

                // We should avoid this binding, since it will cause the searchTextBox to lose focus
                // when the user types in it.

                // this.Bind(ViewModel,
                //         viewModel => viewModel.SearchTerm,
                //         view => view.searchTextBox.Text)
                //     .DisposeWith(disposable);


                this.OneWayBind(ViewModel,
                        x => x.CountryCode,
                        x => x.CountryCodeText.Text)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.CurrentItem,
                        x => x.AlbumCover.Source,
                        metadata => metadata is not null && metadata.LargeImage.IsSome ? new BitmapImage(new Uri(metadata.LargeImage.ValueUnsafe())) : null)
                    .DisposeWith(disposable);

                //BackgroundImage
                this.OneWayBind(ViewModel,
                        x => x.CurrentItem,
                        x => x.BackgroundImage.Source,
                        metadata => metadata is not null && metadata.LargeImage.IsSome ? new BitmapImage(new Uri(metadata.LargeImage.ValueUnsafe())) : null)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.CurrentItem,
                        x => x.PlayingTitle.Content,
                        metadata => metadata?.Title)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.CurrentItem,
                        x => x.PlayingArtist.Content,
                        metadata => metadata?.Descriptions.First().Name)
                    .DisposeWith(disposable);



                this.OneWayBind(ViewModel,
                        x => x.CurrentItem,
                        x => x.DurationBlock.Text,
                        metadata => metadata is not null ? TimestampToString(metadata.Duration) : "00:00")
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.CurrentItem,
                        x => x.PlaybackSlider.Maximum,
                        metadata => metadata?.Duration ?? 0)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.PositionMs,
                        x => x.CurrentPositionBlock.Text,
                        TimestampToString)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.PositionMs,
                        x => x.PlaybackSlider.Value,
                        pos => pos)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.ErrorMessage,
                        x => x.ErrorMessageText.Text)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.IsSignedIn, x => x.SignInPanel.Visibility,
                        selector: b => b ? Visibility.Collapsed : Visibility.Visible)
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                        x => x.IsSignedIn, x => x.SignedInPanel.Visibility,
                        selector: b => b ? Visibility.Visible : Visibility.Collapsed)
                    .DisposeWith(disposable);


                this.Bind(ViewModel,
                    vm => vm.Username,
                    view => view.SignInBox.Text)
                    .DisposeWith(disposable);

                this.Bind(ViewModel,
                        vm => vm.Password,
                        view => view.SignInPasswordBox.Password)
                    .DisposeWith(disposable);

                this.BindCommand(ViewModel, x => x.SignInCommand, x => x.SignInButton, withParameter: vm => vm.Password)
                    .DisposeWith(disposable);
            });
        }
        public MainContent() : this(Option<MainViewModel>.None)
        {
        }

        private static string TimestampToString(int valueDuration)
        {
            //for example 1000 milliseconds should be 00:01
            var seconds = valueDuration / 1000;
            var minutes = seconds / 60;
            seconds %= 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainViewModel)value;
        }

        private void SearchTextBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.SearchTerm = sender.Text;
            }
        }

        private void SearchTextBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {

        }

        private async void SearchTextBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is TrackViewModel track)
            {
                await ViewModel.SelectedItem(track);
            }
        }

        private void RefreshViewTapped(object sender, TappedRoutedEventArgs e)
        {
            MainWindow.Instance.Refresh();
        }

        public void Cleanup()
        {
            ViewModel.Cleanup();
            ViewModel = null;
        }
    }
}
