using Eum.UI.Items;
using Eum.UI.Users;
using Eum.UI.ViewModels.Users;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Eum.UWP.Views.Login
{
    public sealed partial class ProfilesView : UserControl
    {

        public static readonly DependencyProperty LoginViewModelProperty = DependencyProperty.Register(nameof(LoginViewModel), typeof(LoginViewModel), typeof(ProfilesView), new PropertyMetadata(default(LoginViewModel)));

        public ProfilesView()
        {
            this.InitializeComponent();
        }


        public LoginViewModel LoginViewModel
        {
            get => (LoginViewModel)GetValue(LoginViewModelProperty);
            set => SetValue(LoginViewModelProperty, value);
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            LoginViewModel.AddUserCommand.Execute(ServiceType.Spotify);
        }

        private async void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as EumUserViewModel;
            await LoginViewModel.Login(item, CancellationToken.None);
        }
    }
}
