using System.Reactive.Disposables;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.UI.Xaml;

namespace Wavee.UI.WinUI.PlaybackSample
{
    public sealed partial class MainContent : UserControl, IViewFor<MainViewModel>

    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty
            .Register(nameof(ViewModel), typeof(MainViewModel), typeof(MainContent), new PropertyMetadata(null));

        public MainContent()
        {
            ViewModel = new MainViewModel();
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
    }
}
