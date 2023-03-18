using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.ViewModels.Shell.Sidebar;
using Wavee.UI.ViewModels.User;
using Wavee.UI.WinUI.Interfaces.Services;

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class SidebarControl : NavigationView
    {

        public static readonly DependencyProperty NavigationViewServiceProperty =
            DependencyProperty.Register(nameof(NavigationViewService),
                typeof(INavigationViewService),
                typeof(SidebarControl),
                new PropertyMetadata(default(INavigationViewService), NavigationViewServiceInitialized));

        private readonly INavigationService _navigationService;
        public SidebarControl()
        {
            _navigationService = Ioc.Default.GetRequiredService<INavigationService>();
            this.InitializeComponent();
        }

        public INavigationViewService NavigationViewService
        {
            get => (INavigationViewService)GetValue(NavigationViewServiceProperty);
            set => SetValue(NavigationViewServiceProperty, value);
        }

        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public ObservableCollection<ISidebarItem> SidebarItems
        {
            get => (ObservableCollection<ISidebarItem>)GetValue(SidebarItemsProperty);
            set => SetValue(SidebarItemsProperty, value);
        }


        private void Initialize(INavigationViewService navViewService)
        {
            _navigationService.Navigated += OnNavigated;
        }


        public bool ShouldShowHeader(object? o)
        {
            return false;
        }

        private bool _wasExpandedImage;
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(UserViewModel), typeof(SidebarControl), new PropertyMetadata(default(UserViewModel)));
        public static readonly DependencyProperty SidebarItemsProperty = DependencyProperty.Register(nameof(SidebarItems), typeof(ObservableCollection<ISidebarItem>), typeof(SidebarControl), new PropertyMetadata(default(ObservableCollection<ISidebarItem>)));

        private void ViewPanel_OnPaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
        {
            this.FindDescendant<UserProfileCard>()!.Visibility = Visibility.Collapsed;
            _wasExpandedImage = User.SidebarExpanded;
            if (_wasExpandedImage)
            {
                User.SidebarExpanded = false;
            }
        }

        private void ViewPanel_OnPaneOpening(NavigationView sender, object args)
        {
            this.FindDescendant<UserProfileCard>()!.Visibility = Visibility.Visible;
            User.SidebarExpanded = _wasExpandedImage;
        }
        private void OnNavigated(object sender, SharedNavigationEventArgs e)
        {
            IsBackEnabled = _navigationService.CanGoBack;
            var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType, e.Parameter);
            if (selectedItem != null)
            {
                SelectedItem = selectedItem;
            }
        }
        private static void NavigationViewServiceInitialized(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sidebarControl = (SidebarControl)d;

            if (e.NewValue is not null)
            {
                sidebarControl.Initialize(e.NewValue as INavigationViewService);
            }
        }

        private void AddPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
        }
    }
}