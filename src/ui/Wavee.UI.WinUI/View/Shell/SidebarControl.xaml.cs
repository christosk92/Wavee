using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using System.Windows.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.User;
using Wavee.UI.ViewModel.Shell.Sidebar;
using Wavee.UI.WinUI.Helpers;
using Windows.System;
using Windows.UI.Core;
using CommunityToolkit.WinUI.UI.Controls;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Helpers;
using Wavee.UI.WinUI.Navigation;
using Microsoft.VisualBasic.ApplicationServices;
using ReactiveUI;
using Wavee.UI.Client.Playback;

namespace Wavee.UI.WinUI.View.Shell;

public sealed partial class SidebarControl : NavigationView, INotifyPropertyChanged
{
    /// <summary>
    /// true if the user is currently resizing the sidebar
    /// </summary>
    private bool dragging;

    private double originalSize = 0;

    private bool lockFlag = false;
    public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(UserViewModel), typeof(SidebarControl), new PropertyMetadata(default(UserViewModel), UserChanged));


    public SidebarControl()
    {
        this.InitializeComponent();
    }

    public UserViewModel User
    {
        get => (UserViewModel)GetValue(UserProperty);
        set => SetValue(UserProperty, value);
    }

    private ISidebarItem selectedSidebarItem;
    public static readonly DependencyProperty SidebarItemsProperty = DependencyProperty.Register(nameof(SidebarItems), typeof(BulkConcurrentObservableCollection<ISidebarItem>), typeof(SidebarControl), new PropertyMetadata(default(BulkConcurrentObservableCollection<ISidebarItem>)));

    public ISidebarItem SelectedSidebarItem
    {
        get => selectedSidebarItem;
        set => SetField(ref selectedSidebarItem, value);
    }

    public BulkConcurrentObservableCollection<ISidebarItem> SidebarItems
    {
        get => (BulkConcurrentObservableCollection<ISidebarItem>)GetValue(SidebarItemsProperty);
        set => SetValue(SidebarItemsProperty, value);
    }

    public event EventHandler<Option<double>>? Resized;


    public event PropertyChangedEventHandler PropertyChanged;
    private static async void UserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = d as SidebarControl;
        await Task.Delay(50);
        if (e.NewValue is UserViewModel user)
        {
            if (user.Settings.ImageExpanded)
            {
                x.IsPaneOpen = true;
                var box = x.FindDescendant<ConstrainedBox>(x => x.Name is "SidebarImageBox");
                if (box != null)
                    box.Visibility = Visibility.Visible;
            }

            if (user.Client.Playback.CurrentPlayback.IsSome)
            {
                SetImageFromPlaybackEvent(x, user.Client.Playback.CurrentPlayback.ValueUnsafe());
            }

            user.Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(user.Settings.ImageExpanded))
                {
                    if (user.Settings.ImageExpanded)
                        x.IsPaneOpen = true;
                    x.FindDescendant<ConstrainedBox>(x => x.Name is "SidebarImageBox").Visibility = user.Settings.ImageExpanded ? Visibility.Visible : Visibility.Collapsed;
                }
            };

            user.Client.Playback.PlaybackEvents
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(y =>
                {
                    SetImageFromPlaybackEvent(x, y);
                });
        }
    }

    private static string _previousId;
    private static void SetImageFromPlaybackEvent(SidebarControl x, WaveeUIPlaybackState y)
    {
        if (y.Metadata.IsSome)
        {
            if (_previousId != y.Metadata.ValueUnsafe().Id)
            {
                _previousId = y.Metadata.ValueUnsafe().Id;
                var image = x.FindDescendant<Image>(z => z.Name is "SidebarImage");
                if (image is not null)
                {
                    var bmp = new BitmapImage();
                    image.Source = bmp;
                    bmp.UriSource = new Uri(y.Metadata.ValueUnsafe().LargeImageUrl);
                }
            }
        }
    }

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

    private void NavigationView_Collapsed(NavigationView sender, NavigationViewItemCollapsedEventArgs args)
    {
        User.Settings.SidebarExpanded = false;
    }

    private void SidebarControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {

    }

    private void NavigationView_Expanding(NavigationView sender, NavigationViewItemExpandingEventArgs args)
    {
        User.Settings.SidebarExpanded = true;
    }
    private void NavigationView_PaneClosed(NavigationView sender, object args)
    {
        User.Settings.SidebarExpanded = false;
    }
    private void NavigationView_PaneOpened(NavigationView sender, object args)
    {
        User.Settings.SidebarExpanded = true;
    }
    private void Sidebar_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            var data = this.MenuItemFromContainer(item);


            Type? vm = null;
            object? parameter = null;
            if (data is RegularSidebarItem regularSidebarItem)
            {
                vm = regularSidebarItem.ViewModelType;
                parameter = regularSidebarItem.Parameter;
            }
            else if (data is CountedSidebarItem ctd)
            {
                vm = ctd.Inner.ViewModelType;
                parameter = ctd.Inner.Parameter;
            }

            if (vm is not null)
            {
                var page = ViewFactory.GetTypeFromViewModel(vm);
                NavigationService.Instance.Navigate(page, parameter);
            }
        }
    }

    private void SidebarNavView_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void PaneRoot_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        var step = ctrl.HasFlag(CoreVirtualKeyStates.Down) ? 5 : 1;
        originalSize = IsPaneOpen ? User.Settings.SidebarWidth : CompactPaneLength;

        if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
        {
            IsPaneOpen = !IsPaneOpen;
            return;
        }

        if (IsPaneOpen)
        {
            if (e.Key == VirtualKey.Left)
            {
                SetSize(-step, true);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Right)
            {
                SetSize(step, true);
                e.Handled = true;
            }
        }
        else if (e.Key == VirtualKey.Right)
        {
            IsPaneOpen = !IsPaneOpen;
            return;
        }

        User.Settings.SidebarWidth = OpenPaneLength;
    }

    private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (DisplayMode == NavigationViewDisplayMode.Expanded)
            SetSize(e.Cumulative.Translation.X);
    }

    private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (dragging)
            return; // keep showing pressed event if currently resizing the sidebar

        var border = (Border)sender;
        border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerNormal", true);
    }

    private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (DisplayMode != NavigationViewDisplayMode.Expanded)
            return;

        var border = (Border)sender;
        border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
        VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPointerOver", true);
    }

    private void SetSize(double val, bool closeImmediatelyOnOversize = false)
    {
        if (IsPaneOpen)
        {
            var newSize = originalSize + val;
            var isNewSizeGreaterThanMinimum = newSize >= UserSettings.MinimumSidebarWidth;
            if (newSize <= UserSettings.MaximumSidebarWidth && isNewSizeGreaterThanMinimum)
                OpenPaneLength = newSize; // passing a negative value will cause an exception

            // if the new size is below the minimum, check whether to toggle the pane collapse the sidebar
            IsPaneOpen = !(!isNewSizeGreaterThanMinimum && (UserSettings.MinimumSidebarWidth + val <= CompactPaneLength || closeImmediatelyOnOversize));
            if (IsPaneOpen)
            {
                Resized?.Invoke(this, newSize);
            }
            else
            {
                Resized?.Invoke(this, Option<double>.None);
            }
        }
        else
        {
            if (val < UserSettings.MinimumSidebarWidth - CompactPaneLength &&
                !closeImmediatelyOnOversize)
                return;

            OpenPaneLength = val + CompactPaneLength; // set open sidebar length to minimum value to keep it smooth
            IsPaneOpen = true;

            Resized?.Invoke(this, val + CompactPaneLength);
        }
    }

    private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        var border = (Border)sender;
        border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerNormal", true);
        User.Settings.SidebarWidth = OpenPaneLength;
        dragging = false;
    }

    private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        IsPaneOpen = !IsPaneOpen;
    }

    private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        if (DisplayMode != NavigationViewDisplayMode.Expanded)
            return;

        originalSize = IsPaneOpen ? User.Settings.SidebarWidth : CompactPaneLength;
        var border = (Border)sender;
        border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
        VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPressed", true);
        dragging = true;
    }

    public static GridLength GetSidebarCompactSize()
    {
        return App.Current.Resources.TryGetValue("NavigationViewCompactPaneLength", out object paneLength) && paneLength is double paneLengthDouble
            ? new GridLength(paneLengthDouble)
            : new GridLength(200);
    }

    private void DownsizeImageButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        User.Settings.ImageExpanded = !User.Settings.ImageExpanded;
    }

    private void SidebarImageBox_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (User?.Settings?.SidebarExpanded is true)
        {
            var box = sender as ConstrainedBox;
            if (box != null)
                box.Visibility = User.Settings.SidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SidebarImageBox_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        //DownsizeImageButton
        var box = sender as ConstrainedBox;
        var button = box.FindDescendant<Button>(x => x.Name is "DownsizeImageButton") as Button;
        if (button != null)
            button.Visibility = Visibility.Visible;
    }

    private void SidebarImageBox_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var box = sender as ConstrainedBox;
        var button = box.FindDescendant<Button>(x => x.Name is "DownsizeImageButton") as Button;
        if (button != null)
            button.Visibility = Visibility.Collapsed;
    }

    private void SidebarImage_OnLoaded(object sender, RoutedEventArgs e)
    {
        var plb = User?.Client?.Playback?.CurrentPlayback;
        if (plb is not null && plb.Value.IsSome)
        {
            var val = plb.Value.ValueUnsafe();
            SetImageFromPlaybackEvent(this, val);
        }
    }
}