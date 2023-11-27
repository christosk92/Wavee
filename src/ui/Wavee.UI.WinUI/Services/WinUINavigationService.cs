using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Features.Navigation;
using Wavee.UI.WinUI.Contracts;

namespace Wavee.UI.WinUI.Services;


internal sealed class WinUINavigationService : ObservableObject, INavigationService
{
    private Frame? _frame;
    private readonly IServiceProvider _services;
    public WinUINavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;
        GoNextCommand = new RelayCommand(() => { _frame.GoForward(); });
        GoBackCommand = new RelayCommand(() =>
        {
            _frame.GoBack();
            if (_frame.Content is INavigeablePageNonGeneric page)
            {
                page.UpdateBindings();
            }
        });

        _frame.Navigated += (s, e) =>
        {
            GoNextCommand.NotifyCanExecuteChanged();
            GoBackCommand.NotifyCanExecuteChanged();

            this.OnPropertyChanged(nameof(CanGoNext));
            this.OnPropertyChanged(nameof(CanGoBack));

            NavigatedTo?.Invoke(this, e.Parameter);
        };
    }

    public bool CanGoNext => _frame.CanGoForward;
    public bool CanGoBack => _frame.CanGoBack;
    public RelayCommand GoNextCommand { get; set; }
    public RelayCommand GoBackCommand { get; set; }

    public void Navigate<TViewModel>(object navigationParams, TViewModel? overrideDataContext = default)
    {
        var typeName = typeof(TViewModel).FullName;
        if (!_pages.TryGetValue(typeName, out var pageType))
        {
            return;
        }

        var viewModel = overrideDataContext ?? _services.GetRequiredService<TViewModel>();


        if (_frame.Navigate(pageType, parameter: viewModel, infoOverride: navigationParams as NavigationTransitionInfo))
        {
            if (_frame.Content is INavigeablePage<TViewModel> v)
            {
                v.UpdateBindings();
            }
        }
    }

    public event EventHandler<object> NavigatedTo;

    static WinUINavigationService()
    {
        var dictionaryOutput = new Dictionary<string, Type>();
        //Use reflection to get all pages that implement INavigeablePage
        var pages = typeof(WinUINavigationService).Assembly.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Page)) && !x.IsAbstract && x.IsClass)
            .ToList();
        var interestingInterface = typeof(INavigeablePage<>);
        foreach (var page in pages)
        {
            var interfaces = page.GetInterfaces();
            if (interfaces.Any(f => f.Name == interestingInterface.Name))
            {
                var viewModel = interfaces.First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(INavigeablePage<>)).GetGenericArguments()[0];

                var viewModelFullName = viewModel.FullName;
                dictionaryOutput.Add(viewModelFullName, page);

            }
        }

        _pages = dictionaryOutput;
    }

    private static IReadOnlyDictionary<string, Type> _pages;
}