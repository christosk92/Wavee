using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using DynamicData;
using Eum.UI.Helpers;
using Eum.UI.ViewModels.Artists;
using Eum.UI.ViewModels.Fullscreen;
using ReactiveUI;

namespace Eum.UI.ViewModels.Navigation;

// For more information on navigation between pages see
// https://github.com/microsoft/TemplateStudio/blob/main/docs/WinUI/navigation.md
[INotifyPropertyChanged]
public partial class NavigationService
{
    private object? _lastParameterUsed;
    [ObservableProperty]
    private INavigatable? _current;

    public event OpenNavigatedEventHandler? Navigated;
    // public event UnhandledNavigationEventHandler? UnhandledNavigation;


    public bool CanGoBack => Current != null && _navigationBackStack.Count > 0;

    public bool CanGoForward => Current != null & _navigationForwardStack.Count > 0;

    public NavigationService()
    {
        Instance = this;
    }
    public static NavigationService Instance { get; private set; }
    public event EventHandler<AbsFullscreenViewModel?> OnFullscreenNavigation;

    public bool GoBack()
    {
        throw new NotImplementedException();
        // if (CanGoBack)
        // {
        //     //we add the current page to the forward stack
        //     //and we remove the previous page from the backstack!
        //
        //     //Add this page to the forward for forward navigation! the as cast will default to null 
        //     if (_navigationForwardStack.LastOrDefault().PageType != _current.GetType())
        //         _navigationForwardStack.Add((_current.GetType(), _current,
        //             _lastParameterUsed));
        //
        //     var vmBeforeNavigation = _frame.GetPageViewModel();
        //     if (vmBeforeNavigation is INavigationAware navigationAware)
        //     {
        //         navigationAware.OnNavigatedFrom();
        //     }
        //     _current?.OnNavigatedFrom();
        //
        //     var previousPage = _navigationBackStack.Last();
        //     if (previousPage.Page is not null)
        //     {
        //         _frame.Content = previousPage.Page;
        //         Navigated?.Invoke(this, new OpenNavigationEventArgs
        //         {
        //             SourcePageType = previousPage.Page.GetType()
        //         });
        //     }
        //     else
        //     {
        //
        //         var navigateTo = GetService(previousPage.PageType);
        //         _frame.Content = navigateTo;
        //
        //
        //         //var navigated = _frame.Navigate(pageType, parameter);
        //         var vmTo = _frame.GetPageViewModel();
        //         if (vmTo is INavigationAware navigationAwareNext)
        //         {
        //             _lastParameterUsed = previousPage.Parameter;
        //             navigationAwareNext.OnNavigatedTo(previousPage.Parameter);
        //         }
        //         if (_frame.Content is INavigationAware navigationAwareNext_Page)
        //         {
        //             navigationAwareNext_Page.OnNavigatedFrom();
        //         }
        //         Navigated?.Invoke(this, new OpenNavigationEventArgs
        //         {
        //             SourcePageType = navigateTo.GetType()
        //         });
        //     }
        //     _navigationBackStack.RemoveAt(_navigationBackStack.Count - 1);
        //     CheckHistory();
        //     return true;
        // }

        return false;
    }

    public bool GoForward()
    {
        throw new NotImplementedException();
        // if (CanGoForward)
        // {
        //     //we add the current page to the backward stack
        //     //and we remove the previous page from the forward!
        //
        //     //Add this page to the forward for forward navigation! the as cast will default to null 
        //     if (_navigationBackStack.LastOrDefault().PageType != _frame.Content.GetType())
        //         _navigationBackStack.Add((_frame.Content.GetType(), _frame.Content as ICachedPage, _lastParameterUsed));
        //
        //     var vmBeforeNavigation = _frame.GetPageViewModel();
        //     if (vmBeforeNavigation is INavigationAware navigationAware)
        //     {
        //         navigationAware.OnNavigatedFrom();
        //     }
        //     if (_frame.Content is INavigationAware navigationAware_Page)
        //     {
        //         navigationAware_Page.OnNavigatedFrom();
        //     }
        //
        //     var nextPage = _navigationForwardStack.Last();
        //     if (nextPage.Page is not null)
        //     {
        //         _frame.Content = nextPage.Page;
        //         Navigated?.Invoke(this, new OpenNavigationEventArgs
        //         {
        //             SourcePageType = nextPage.Page.GetType()
        //         });
        //     }
        //     else
        //     {
        //
        //         var navigateTo = GetService(nextPage.PageType);
        //         _frame.Content = navigateTo;
        //
        //         //var navigated = _frame.Navigate(pageType, parameter);
        //         var vmTo = _frame.GetPageViewModel();
        //         Navigated?.Invoke(this, new OpenNavigationEventArgs
        //         {
        //             SourcePageType = nextPage.PageType
        //         });
        //         if (vmTo is INavigationAware navigationAwareNext)
        //         {
        //             _lastParameterUsed = nextPage.Parameter;
        //             navigationAwareNext.OnNavigatedTo(nextPage.Parameter);
        //         }
        //
        //         if (_frame.Content is INavigationAware navigationAwareNext_Page)
        //         {
        //             _lastParameterUsed = nextPage.Parameter;
        //             navigationAwareNext_Page.OnNavigatedTo(nextPage.Parameter);
        //         }
        //     }
        //     _navigationForwardStack.RemoveAt(_navigationForwardStack.Count - 1);
        //     CheckHistory();
        //     return true;
        // }
        // return false;
    }

    private readonly List<(Type PageType, INavigatable? Page, object Parameter)> _navigationBackStack = new();
    private readonly List<(Type PageType, INavigatable? Page, object? Parameter)> _navigationForwardStack = new();

    private CancellationTokenSource? _navigationToken;
    public bool To(INavigatable navigateTo, object? parameter = null, bool clearNavigation = false)
    {
        if (_current != navigateTo || (parameter != null && !parameter.Equals(_lastParameterUsed)))
        {
            if (navigateTo is AbsFullscreenViewModel f)
            {
                OnFullscreenNavigation?.Invoke(this, f);
            }
            else
            {
                OnFullscreenNavigation?.Invoke(this, null);
            }
            //_frame.Tag = clearNavigation;
            var vmBeforeNavigation = _current;
            if (_current != null)
            {
                //Add this page to the backstack for backward navigation! the as cast will default to null 
                _navigationBackStack.Add((_current.GetType(), _current, _lastParameterUsed));
            }

            try
            {
                _navigationToken?.Cancel();
                _navigationToken?.Dispose();
            }
            catch (Exception)
            {

            }

            if (navigateTo is IGlazeablePage glazeablePage)
            {
                if (glazeablePage.ShouldSetPageGlaze)
                {
                    _navigationToken = new CancellationTokenSource();
                    Task.Run(async () =>
                    {
                        var theme = Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.ThemeService.Theme;
                        var glazeColor = await glazeablePage.GetGlazeColor(theme, _navigationToken.Token);
                        if (!_navigationToken.IsCancellationRequested)
                        {
                            var f = glazeColor.ToColor();
                            var colorCodeHex = (Color.FromArgb(25, f.R, f.G, f.B)).ToHex();
                            RxApp.MainThreadScheduler.Schedule(() =>
                            {
                                Ioc.Default.GetRequiredService<MainViewModel>()
                                    .Glaze = colorCodeHex;
                            });
                        }

                    }, _navigationToken .Token);
                }
            }
            Current = navigateTo;
            Navigated?.Invoke(this, new OpenNavigationEventArgs
            {
                ToViewModelType = navigateTo.GetType()
            });
            
            //var navigated = _frame.Navigate(pageType, parameter);

            _lastParameterUsed = parameter;
            vmBeforeNavigation?.OnNavigatedFrom();

            navigateTo.OnNavigatedTo(parameter);

            CheckHistory();
            //GC.Collect();
            return true;
        }

        return false;
    }

    private void CheckHistory()
    {
        for (int i = 0; i < _navigationBackStack.Count; i++)
        {
            var item = _navigationBackStack.ElementAt(i);
            if (item.Page is not null)
            {
                //if we are 1 pages deep, then our index = 0
                //if we are 2 pages deep, our index = 1...
                //so if our max depth = 1 (meaning the page can be at MAX index : length - 2 = length - depth - 1)

                //if we have 6 pages in the backstack, we add another page, meaning we now have 7 pages. We check page 6, its max depth = 2,
                //meaning its ok! because page 6 = 5, and 5 + 2 = 7 == length of array
                //however, now we go another page, the backstack is now 8 pages long, 
                //page 6 = 5, and 5 +2 = 7 < length.
                //so we should check if index_page + max_depth < length

                //similarly: if we are at page 6, page 6 has a depth of 1, we go back to page 4, meaning 6 should be cleared of cache...
                //because: index + 2 = 3 +2 = 5, the length = still 6!
                if (item.Page.MaxDepth + i < _navigationBackStack.Count)
                {
                    _navigationBackStack[i] = (item.PageType, null, item.Parameter);
                }
            }
        }

        for (int i = 0; i < _navigationForwardStack.Count; i++)
        {
            var item = _navigationForwardStack.ElementAt(i);
            if (item.Page is not null)
            {
                //if we are 1 pages deep, then our index = 0
                //if we are 2 pages deep, our index = 1...
                //so if our max depth = 1 (meaning the page can be at MAX index : length - 2 = length - depth - 1)

                //if we have 6 pages in the backstack, we add another page, meaning we now have 7 pages. We check page 6, its max depth = 2,
                //meaning its ok! because page 6 = 5, and 5 + 2 = 7 == length of array
                //however, now we go another page, the backstack is now 8 pages long, 
                //page 6 = 5, and 5 +2 = 7 < length.
                //so we should check if index_page + max_depth < length

                //similarly: if we are at page 6, page 6 has a depth of 1, we go back to page 4, meaning 6 should be cleared of cache...
                //because: index + 2 = 3 +2 = 5, the length = still 6!
                if (item.Page.MaxDepth + i < _navigationForwardStack.Count)
                {
                    _navigationForwardStack[i] = (item.PageType, null, item.Parameter);
                }
            }
        }

        if (_navigationForwardStack.Count > max_depth)
        {
            while (_navigationForwardStack.Count > max_depth)
            {
                _navigationForwardStack.RemoveAt(0);
            }
        }
        if (_navigationBackStack.Count > max_depth)
        {
            while (_navigationBackStack.Count > max_depth)
            {
                _navigationBackStack.RemoveAt(0);
            }
        }
    }

    private const int max_depth = 10;

}