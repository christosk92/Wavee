using System.Reactive.Disposables;
using DynamicData.Binding;
using DynamicData.Operators;
using System.Windows.Input;
using LanguageExt;
using ReactiveUI;

namespace Wavee.UI.ViewModels.Playlists.Specific;
public class PageParameterData : AbstractNotifyPropertyChanged
{
    private readonly ReactiveCommand<System.Reactive.Unit, Unit> _nextPageCommand;
    private readonly ReactiveCommand<System.Reactive.Unit, Unit> _previousPageCommand;
    private int _currentPage;
    private int _pageCount;
    private int _pageSize;
    private int _totalCount;
    public PageParameterData(int currentPage, int pageSize)
    {
        _currentPage = currentPage;
        _pageSize = pageSize;

        var canGoNext = this.WhenAnyValue(x => x.CurrentPage, x => x.PageCount, (page, count) => page < count);

        _nextPageCommand = ReactiveCommand.Create(() =>
        {
            CurrentPage = CurrentPage + 1;
            return Unit.Default;
        }, canGoNext);

        var canGoPrevious = this.WhenAnyValue(x => x.CurrentPage, x => x.PageCount, (page, count) => page > 1);
        _previousPageCommand = ReactiveCommand.Create(() =>
        {
             CurrentPage = CurrentPage - 1;
            return Unit.Default;
        }, canGoPrevious);
    }

    public ICommand NextPageCommand => _nextPageCommand;

    public ICommand PreviousPageCommand => _previousPageCommand;

    public int TotalCount
    {
        get => _totalCount;
        private set => SetAndRaise(ref _totalCount, value);
    }

    public int PageCount
    {
        get => _pageCount;
        private set => SetAndRaise(ref _pageCount, value);
    }

    public int CurrentPage
    {
        get => _currentPage;
        private set => SetAndRaise(ref _currentPage, value);
    }


    public int PageSize
    {
        get => _pageSize;
        private set => SetAndRaise(ref _pageSize, value);
    }


    public void Update(IPageResponse response)
    {
        CurrentPage = response.Page;
        PageSize = response.PageSize;
        PageCount = response.Pages;
        TotalCount = response.TotalSize;
        // _nextPageCommand.e();
        // _previousPageCommand.Refresh();
    }
}