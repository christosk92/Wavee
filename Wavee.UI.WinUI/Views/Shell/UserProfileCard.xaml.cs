using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Wavee.Enums;
using Wavee.UI.Models.Profiles;

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class UserProfileCard : UserControl
    {
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User),
            typeof(Profile), typeof(UserProfileCard), new PropertyMetadata(default(Profile), ProfileChanged));

        public UserProfileCard()
        {
            this.InitializeComponent();
        }

        public Profile User
        {
            get => (Profile)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        private static void ProfileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (UserProfileCard)d;
            if (e.NewValue is Profile p)
            {
                c.UpdateProfileData();
            }
        }

        private void UpdateProfileData()
        {
            Name.Text = User.DisplayName;
            if (!string.IsNullOrEmpty(User.Image))
                Prf.ProfilePicture = new BitmapImage(new Uri(User.Image));
            else
            {
                Prf.DisplayName = User.DisplayName;
            }

            Product.Text = User.ServiceType switch
            {
                ServiceType.Local => "OFFLINE",
                ServiceType.Spotify => User.Properties["product"].ToUpper(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
