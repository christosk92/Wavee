using System;
using Windows.Foundation;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.FakeService;
using Wavee.UI.Spotify;
using Wavee.UI.ViewModels;
using Wavee.UI.WinUI.Views.Account;
using Wavee.UI.WinUI.Views.Home;
using Wavee.UI.WinUI.Views.Search;
using HomeViewModel = Wavee.UI.ViewModels.Home.HomeViewModel;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using SizeChangedEventArgs = Microsoft.UI.Xaml.SizeChangedEventArgs;

namespace Wavee.UI.WinUI.Views;

public sealed partial class ShellView : UserControl
{
    public ShellView()
    {
        ViewModel = new ShellViewModel(new ViewFactory(), new SpotifyClientFactory(new SpotifyConfig());
        this.InitializeComponent();

        AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
        AppTitleBar.Loaded += AppTitleBar_Loaded;
        MainWindow.Instance.SetTitleBar(AppTitleBar);

        MainWindow.Instance.ExtendsContentIntoTitleBar = true;
        if (MainWindow.Instance.ExtendsContentIntoTitleBar)
        {
            MainWindow.Instance.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }
    }

    public ShellViewModel ViewModel { get; }

    private void SearchBox_OnGotFocus(object sender, RoutedEventArgs e)
    {
        //TODO
    }

    private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (MainWindow.Instance.ExtendsContentIntoTitleBar == true)
        {
            // Set the initial interactive regions.
            SetRegionsForCustomTitleBar();
        }
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MainWindow.Instance.ExtendsContentIntoTitleBar == true)
        {
            // Update interactive regions if the size of the window changes.
            SetRegionsForCustomTitleBar();
        }
    }

    private void SetRegionsForCustomTitleBar()
    {
        // Specify the interactive regions of the title bar.

        double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(MainWindow.Instance.AppWindow.TitleBar.RightInset / scaleAdjustment);
        LeftPaddingColumn.Width = new GridLength(MainWindow.Instance.AppWindow.TitleBar.LeftInset / scaleAdjustment);

        // Get the rectangle around the MyLibraryButtonOne control.
        GeneralTransform transform = MyLibraryButtonOne.TransformToVisual(null);
        Rect bounds = transform.TransformBounds(new Rect(0, 0,
            MyLibraryButtonOne.ActualWidth,
            MyLibraryButtonOne.ActualHeight));
        Windows.Graphics.RectInt32 MyLibraryButtonOneRect = GetRect(bounds, scaleAdjustment);


        // Get the rectangle around the MyLibraryButtonTwo control.
        transform = MyLibraryButtonTwo.TransformToVisual(null);
        bounds = transform.TransformBounds(new Rect(0, 0,
            MyLibraryButtonTwo.ActualWidth,
            MyLibraryButtonTwo.ActualHeight));
        Windows.Graphics.RectInt32 MyLibraryButtonTwoRect = GetRect(bounds, scaleAdjustment);

        // Get the rectangle around the TabViewCtrl control.
        transform = TabViewCtrl.TransformToVisual(null);
        bounds = transform.TransformBounds(new Rect(0, 0,
            TabViewCtrl.ActualWidth,
            TabViewCtrl.ActualHeight));
        Windows.Graphics.RectInt32 TabViewCtrlRect = GetRect(bounds, scaleAdjustment);


        var rectArray = new Windows.Graphics.RectInt32[] { MyLibraryButtonOneRect, MyLibraryButtonTwoRect, TabViewCtrlRect };

        InputNonClientPointerSource nonClientInputSrc =
            InputNonClientPointerSource.GetForWindowId(MainWindow.Instance.AppWindow.Id);
        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
    }

    private Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
    {
        return new Windows.Graphics.RectInt32(
            _X: (int)Math.Round(bounds.X * scale),
            _Y: (int)Math.Round(bounds.Y * scale),
            _Width: (int)Math.Round(bounds.Width * scale),
            _Height: (int)Math.Round(bounds.Height * scale)
        );
    }
}

public sealed class ViewFactory : IViewFactory
{
    private readonly Lazy<HomeView> _homeView;
    private readonly Lazy<SearchView> _searchView;
    private readonly Lazy<SignInView> _signInView;

    public ViewFactory()
    {
        _homeView = new Lazy<HomeView>(() => new HomeView());
        _searchView = new Lazy<SearchView>(() => new SearchView());
        _signInView = new Lazy<SignInView>(() => new SignInView());
    }

    public object CreateView(object viewModel)
    {
        switch (viewModel)
        {
            case HomeViewModel home:
                var homeView = _homeView.Value;
                homeView.DataContext = home;
                return homeView;
            case SearchViewModel search:
                var searchView = _searchView.Value;
                searchView.DataContext = search;
                return searchView;
            case AccountViewModel signIn when !signIn.IsSignedIn:
                var signInView = _signInView.Value;
                signInView.DataContext = signIn;
                return signInView;
        }

        return null;
    }
}