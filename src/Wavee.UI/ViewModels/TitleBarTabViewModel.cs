using System;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.WinUI;

namespace Wavee.UI.ViewModels;

/// <summary>
/// A view model for a (virtual) tab in the title bar.
/// </summary>
public sealed partial class TitleBarTabViewModel : ReactiveObject
{
    //Do not flip this bool!
    [AutoNotify] private bool _isSelected;
    [AutoNotify] private string? _title;
    [AutoNotify] private IconSource? _icon;

    //The current view model for the tab.
    [AutoNotify] private object? _viewModel;

    private TitleBarTabViewModel(
        Guid id,
        string title,
        IconElement icon,
        object? viewModel,
        bool canClose,
        bool partOfMenuNavigation)
    {
        Title = title;
        Icon = icon.ToIconSource();
        Id = id;
        CanClose = canClose;
        PartOfMenuNavigation = partOfMenuNavigation;
        _viewModel = viewModel;
    }

    public bool CanClose { get; }
    public Guid Id { get; }
    public bool PartOfMenuNavigation { get; }
    public IObservable<bool> IsSelectedObservable => this.WhenAnyValue(x => x.IsSelected);
    public IObservable<object?> ViewModelObservable => this.WhenAnyValue(x => x.ViewModel);
    public static TitleBarTabViewModel Library(LibraryViewModel library) => new(
        id: Constants.LibraryTabId,
        title: "My Library",
        icon: Icons.SegoeFluent("\uE8F1"),
        //iconfilled: Icons.SegoeFluent("\uE8F1"),
        viewModel: null,
        canClose: false,
        partOfMenuNavigation: false);

    public static TitleBarTabViewModel Home(Home.HomeViewModel viewModel) => new(
        id: Constants.HomeTabId,
        title: "Home",
        icon: Icons.SegoeFluent("\uE80F"),
        //iconfilled: Icons.SegoeFluent("\uEA8A"),
        viewModel: viewModel,
        canClose: false,
        partOfMenuNavigation: true);

    public static TitleBarTabViewModel Search(SearchViewModel search) => new(
        id: Constants.SearchTabId,
        title: "Search",
        icon: Icons.SegoeFluent("\uE721"),
        //iconfilled: Icons.SegoeFluent("\uE721"),
        search,
        canClose: false,
        partOfMenuNavigation: true);

    public static TitleBarTabViewModel SignIn(AccountViewModel account) => new TitleBarTabViewModel(
        id: Constants.SignInTabId,
        title: "Sign In",
        icon: Icons.SegoeFluent("\uE721"),
        account,
        canClose: false,
        partOfMenuNavigation: false);
}